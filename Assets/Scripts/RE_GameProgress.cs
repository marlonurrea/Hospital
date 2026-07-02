using UnityEngine; // Librería base
using System.Collections.Generic; // Permite el uso de Listas (List<T>)
using TMPro; // Textos mejorados
using UnityEngine.UI; // UI normal
using UnityEngine.SceneManagement; // Control de mapas/escenas

/// <summary>
/// Contenedor que agrupa toda la información que necesitamos guardar como archivo de guardado.
/// </summary>
[System.Serializable] // Permite que Unity convierta estos datos en texto/JSON para guardarlos en el disco duro
public class GameProgressData // Clase contenedora de datos puros, sin lógica
{
    [Header("Datos Generales")]
    public string currentSceneName = "Hospital_Lobby"; // Último mapa en el que estuvo el jugador
    public int RE_PlayerHealth = 100; // Vida actual con la que se guardó
    public int playerMaxHealth = 100; // Capacidad máxima de vida

    [Header("Progreso del Juego")]
    public List<string> completedTasks = new List<string>(); // Lista de nombres de las tareas ya hechas
    public List<string> keycardsObtained = new List<string>(); // Lista de llaves o tarjetas recogidas
    public int currentObjectiveIndex = 0; // Índice de la misión principal actual

    [Header("Posición del Jugador")]
    public bool hasSavedPosition = false; // Bandera para saber si hay punto de control
    public float playerPosX; // Eje X del mapa
    public float playerPosY; // Altura Y del mapa
    public float playerPosZ; // Profundidad Z del mapa

    [Header("Hitos de Progreso")]
    public bool reached25 = false; // ¿Llegó al 25%?
    public bool reached50 = false; // ¿Llegó al 50%?
    public bool reached75 = false; // ¿Llegó al 75%?
    public bool reached100 = false; // ¿Completó todo?
}

public class RE_GameProgress : MonoBehaviour // Clase principal que maneja los datos y la interfaz del progreso
{
    // Singleton para que cualquier archivo pueda llamar a 'RE_GameProgress.Instance' globalmente
    public static RE_GameProgress Instance { get; private set; }

    [Header("Datos de Progreso")]
    public GameProgressData progressData = new GameProgressData(); // Crea un objeto de nuestra clase contenedora de arriba

    [Header("Ajustes de Porcentaje")]
    [Tooltip("Cantidad total de misiones principales necesarias para llegar al 100%.")]
    public int totalMainTasks = 4; // Por defecto necesitamos 4 misiones principales para el 100% (incluye la recepcionista)

    [Header("Herramientas de Prueba")]
    [Tooltip("Dale a Play con esto marcado para borrar la partida guardada y empezar de 0.")]
    public bool reiniciarAlIniciar = false; // Trampa de desarrollador para limpiar las partidas de prueba

    [Header("Referencias de UI (HUD)")]
    [Tooltip("El GameObject que tiene el texto de porcentaje (ej: el objeto Porcentaje).")]
    [SerializeField] private GameObject progressTextObject; // El texto en la pantalla que dice "X%"

    [Tooltip("El GameObject que tiene la barra de progreso (ej: el objeto Barra de progreso).")]
    [SerializeField] private GameObject progressBarObject; // La barra que se llena al completar misiones

    // Eventos a los que otros scripts pueden suscribirse. Ejemplo: la música de victoria se suscribe a OnProgress100
    public static event System.Action<float> OnProgressChanged;
    public static event System.Action OnProgress25;
    public static event System.Action OnProgress50;
    public static event System.Action OnProgress75;
    public static event System.Action OnProgress100;

    [Header("Configuración de Guardado")]
    [Tooltip("Clave con la que se guardará el archivo en el sistema.")]
    [SerializeField] private string saveKey = "HospitalGameProgress"; // El nombre del "Archivo de Guardado"

    private void Awake() // Se ejecuta inmediatamente
    {
        if (Instance == null) // Si somos el primer Gestor de Progreso
        {
            Instance = this; // Nos coronamos como el principal
            DontDestroyOnLoad(gameObject); // Evitamos que nos eliminen al cambiar de nivel
            
            if (reiniciarAlIniciar) ResetProgress(); // Si el programador quiere reiniciar de 0, lo hacemos
            else LoadProgress(); // Si no, cargamos nuestra partida guardada normalmente
        }
        else // Si ya existía otro gestor cargado...
        {
            if (reiniciarAlIniciar) Instance.ResetProgress(); // Le pasamos la orden al gestor principal de reiniciarse
            Destroy(gameObject); // Nos autodestruimos para no generar conflictos
        }
    }

    private void OnEnable() // Se ejecuta al activar este script
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Avisamos que queremos saber cuando se carga un mapa nuevo
    }

    private void OnDisable() // Al desactivarse...
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Cancelamos el aviso
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // Función que corre cada vez que entramos a un nivel
    {
        DetectUIComponents(); // Busca los textos y barras en la nueva pantalla porque los anteriores se destruyeron
        ActualizarUI(GetProgressPercentage()); // Refresca los números para que sean exactos
    }

    private void Start() // Se ejecuta en el primer fotograma
    {
        DetectUIComponents(); // Detectamos componentes visuales por si acaso
        ActualizarUI(GetProgressPercentage()); // Refrescamos
    }

    /// <summary>
    /// Busca y asigna los componentes visuales aunque no los hayamos conectado en el Inspector manualmente
    /// </summary>
    private void DetectUIComponents()
    {
        // 1. Si el texto se rompió o está vacío...
        if (progressTextObject == null)
        {
            progressTextObject = GameObject.Find("Porcentaje"); // Lo buscamos por su nombre exacto
            if (progressTextObject == null) // Si sigue sin estar
            {
                foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) // Revisamos ABSOLUTAMENTE TODOS los objetos ocultos
                {
                    if (go.hideFlags == HideFlags.None && (go.name.ToLower().Contains("porcentaje") || go.name.ToLower().Contains("progreso")))
                    {
                        progressTextObject = go; // Si algún objeto dice "porcentaje" en su nombre, lo usamos
                        break;
                    }
                }
            }
        }

        // Hacemos el mismo escaneo intenso, pero buscando la barra visual
        if (progressBarObject == null)
        {
            progressBarObject = GameObject.Find("Barra de progreso");
            if (progressBarObject == null)
            {
                foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                {
                    if (go.hideFlags == HideFlags.None && (go.name.ToLower().Contains("barra de progreso") || go.name.ToLower().Contains("progressbar") || go.name.ToLower().Contains("progress bar")))
                    {
                        progressBarObject = go;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Transforma nuestra clase de datos (GameProgressData) en texto y lo guarda en el disco duro (PlayerPrefs).
    /// </summary>
    public void SaveProgress() // Función de guardado de partida
    {
        try // Intentamos guardar (usamos try por si el disco está lleno o sin permisos)
        {
            string json = JsonUtility.ToJson(progressData, true); // Convierte los datos a un texto JSON
            PlayerPrefs.SetString(saveKey, json); // Guarda el texto largo en el sistema bajo nuestro nombre de archivo
            PlayerPrefs.Save(); // Obliga al disco a escribir
            Debug.Log("[RE_GameProgress] Progreso guardado con éxito.");
        }
        catch (System.Exception e) // Si falla...
        {
            Debug.LogError($"[RE_GameProgress] Error al guardar el progreso: {e.Message}"); // Reportar error
        }
    }

    /// <summary>
    /// Lee el texto de PlayerPrefs y lo convierte de vuelta a nuestra clase GameProgressData.
    /// </summary>
    public void LoadProgress() // Función de cargar partida
    {
        if (PlayerPrefs.HasKey(saveKey)) // Si el archivo sí existe...
        {
            try
            {
                string json = PlayerPrefs.GetString(saveKey); // Extraemos todo el texto
                JsonUtility.FromJsonOverwrite(json, progressData); // Lo inyectamos en nuestra clase actual
                Debug.Log("[RE_GameProgress] Progreso cargado con éxito.");
            }
            catch (System.Exception e) // Si el archivo está corrupto
            {
                Debug.LogError($"[RE_GameProgress] Partida corrupta. Iniciando por defecto: {e.Message}");
                progressData = new GameProgressData(); // Empezamos de cero si se corrompió
            }
        }
        else // Si es la primera vez que juegan
        {
            Debug.Log("[RE_GameProgress] No se encontró partida. Iniciando nueva.");
            progressData = new GameProgressData(); // Crea contenedor vacío
        }
        ActualizarUI(GetProgressPercentage()); // Mostramos en pantalla lo que hayamos cargado
    }

    /// <summary>
    /// Borra definitivamente la partida guardada (Hard Reset)
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(saveKey); // Elimina el archivo del registro
        progressData = new GameProgressData(); // Limpia los datos de la memoria RAM
        ActualizarUI(0f); // Pone la pantalla en 0%
    }

    /// <summary>
    /// Solo borra las misiones del nivel (Soft Reset) útil si pierdes y te reinician el mapa, pero conservas la vida máxima, etc.
    /// </summary>
    public void ResetLevelProgressOnly()
    {
        progressData.completedTasks.Clear(); // Vaciamos la lista de tareas
        progressData.reached25 = false; // Reseteamos hitos
        progressData.reached50 = false;
        progressData.reached75 = false;
        progressData.reached100 = false;

        ActualizarUI(0f);
        SaveProgress(); // Confirmamos el borrado
    }

    #region Métodos de Utilidad / Atajos (Lógica de Misiones)

    /// <summary>
    /// Se llama a esta función (ej: desde un NPC) para marcar una misión como hecha.
    /// </summary>
    public void CompleteTask(string taskId)
    {
        if (!progressData.completedTasks.Contains(taskId)) // Si no estaba hecha previamente
        {
            progressData.completedTasks.Add(taskId); // La añadimos a la lista de completadas
            
            float currentPercent = GetProgressPercentage(); // Recalculamos el porcentaje total
            
            OnProgressChanged?.Invoke(currentPercent); // Avisamos a todos los scripts visuales para que se actualicen
            CheckProgressMilestones(currentPercent); // Revisamos si con esta misión activamos un Hito Especial
            ActualizarUI(currentPercent); // Refrescamos pantalla

            SaveProgress(); // Guardamos automáticamente la partida por precaución
        }
    }

    /// <summary>
    /// Calcula cuántas tareas tenemos divididas entre las totales para sacar el porcentaje base 100.
    /// </summary>
    public float GetProgressPercentage()
    {
        if (totalMainTasks <= 0) return 0f; // Evitar división por cero
        float percent = ((float)progressData.completedTasks.Count / totalMainTasks) * 100f; // Regla de tres simple
        return Mathf.Clamp(percent, 0f, 100f); // Asegurar que nunca se pase del 100%
    }

    /// <summary>
    /// Revisa si el jugador ha cruzado marcas clave del porcentaje (25%, 50%, 75%, 100%)
    /// y avisa a los eventos especiales (por si queremos que suene música, den un logro, etc).
    /// </summary>
    private void CheckProgressMilestones(float percentage)
    {
        if (percentage >= 25f && !progressData.reached25)
        {
            progressData.reached25 = true; // Confirmamos que pasamos el hito
            OnProgress25?.Invoke(); // Disparamos la alerta de "llegamos al 25"
        }
        if (percentage >= 50f && !progressData.reached50)
        {
            progressData.reached50 = true;
            OnProgress50?.Invoke();
        }
        if (percentage >= 75f && !progressData.reached75)
        {
            progressData.reached75 = true;
            OnProgress75?.Invoke();
        }
        if (percentage >= 100f && !progressData.reached100)
        {
            progressData.reached100 = true; // Confirmamos que ya hicimos todo
            OnProgress100?.Invoke(); // Dispara la orden que finaliza el nivel (escuchada por RE_LevelComplete.cs)
        }
    }

    /// <summary>
    /// Comprueba si una tarea específica está en la lista.
    /// </summary>
    public bool IsTaskCompleted(string taskId) { return progressData.completedTasks.Contains(taskId); }

    // Misiones específicas hardcodeadas buscando la palabra clave
    public bool IsGuardiaCompleted() { foreach (string task in progressData.completedTasks) if (task.ToLower().Contains("guardia")) return true; return false; }
    public bool IsCivilCompleted() { foreach (string task in progressData.completedTasks) if (task.ToLower().Contains("civil")) return true; return false; }
    public bool IsEnfermeroCompleted() { foreach (string task in progressData.completedTasks) if (task.ToLower().Contains("enfermero")) return true; return false; }

    /// <summary>
    /// Actualiza los textos y las barras de progreso de la interfaz.
    /// </summary>
    public void ActualizarUI(float porcentaje)
    {
        string textoFormateado = $"Progreso {porcentaje:0}%"; // Crea el texto, omitiendo decimales

        // Actualizar el número escrito
        if (progressTextObject != null)
        {
            // Intentar buscar los TextMeshPro o los Text Antiguos y asignarles el texto
            TextMeshProUGUI textTMP = progressTextObject.GetComponent<TextMeshProUGUI>();
            if (textTMP == null) textTMP = progressTextObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textTMP != null) textTMP.text = textoFormateado;
            else
            {
                Text textLegacy = progressTextObject.GetComponent<Text>();
                if (textLegacy == null) textLegacy = progressTextObject.GetComponentInChildren<Text>();
                if (textLegacy != null) textLegacy.text = textoFormateado;
            }
        }

        // Actualizar el tamaño visual de la barra
        if (progressBarObject != null)
        {
            // Intentar actualizar un Slider
            Slider slider = progressBarObject.GetComponent<Slider>();
            if (slider == null) slider = progressBarObject.GetComponentInChildren<Slider>();
            
            if (slider != null)
            {
                slider.value = (porcentaje / 100f) * slider.maxValue; // Lo ajusta al máximo permitido
            }
            else
            {
                // Si no hay slider, intentamos rellenar una Imagen estilo 'Filled'
                Image image = progressBarObject.GetComponent<Image>();
                if (image == null) image = progressBarObject.GetComponentInChildren<Image>();
                if (image != null) image.fillAmount = porcentaje / 100f; // Asigna un valor de 0 a 1
            }
        }
    }
    #endregion
}
