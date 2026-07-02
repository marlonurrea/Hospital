using UnityEngine; // La caja de herramientas básica de Unity.
using System.Collections.Generic; // Nos permite crear "Listas" (mochilas donde guardamos muchos datos juntos).
using TMPro; // Herramienta para usar letras bonitas y nítidas (TextMeshPro).
using UnityEngine.UI; // Herramienta para usar barras de vida o botones.
using UnityEngine.SceneManagement; // El encargado de cambiar de mapas/niveles.

// -----------------------------------------------------------------------------
// CLASE: GameProgressData
// METÁFORA: "La Hoja de Vida / El Archivero"
// Esta clase no tiene código que haga acciones. Es literalmente un pedazo de papel 
// donde anotamos qué ha hecho el jugador para luego guardarlo en el disco duro.
// -----------------------------------------------------------------------------
[System.Serializable] // Permite que Unity "empaquete" estos datos en un archivo de texto (JSON)
public class GameProgressData 
{
    [Header("Datos Generales")]
    public string currentSceneName = "Hospital_Lobby"; // En qué piso del hospital se quedó.
    public int RE_PlayerHealth = 100; // Con cuánta vida se fue a dormir.
    public int playerMaxHealth = 100; // Cuánta vida máxima puede tener.

    [Header("Progreso del Juego")]
    public List<string> completedTasks = new List<string>(); // La lista del mercado: "Ya hablé con el Guardia", "Ya hablé con el Civil", etc.
    public List<string> keycardsObtained = new List<string>(); // Llavero para guardar tarjetas de acceso.
    public int currentObjectiveIndex = 0; // Por qué paso de la misión vamos (0, 1, 2...).

    [Header("Posición del Jugador")]
    public bool hasSavedPosition = false; // ¿Guardamos la ubicación exacta en el mapa?
    public float playerPosX; // Coordenada X (Derecha/Izquierda)
    public float playerPosY; // Coordenada Y (Arriba/Abajo)
    public float playerPosZ; // Coordenada Z (Adelante/Atrás)

    [Header("Hitos de Progreso")]
    // Como las insignias o trofeos. ¿Ya llegó a cierto porcentaje del nivel?
    public bool reached25 = false; 
    public bool reached50 = false; 
    public bool reached75 = false; 
    public bool reached100 = false; 
}

// -----------------------------------------------------------------------------
// SCRIPT PRINCIPAL: RE_GameProgress
// METÁFORA: "El Alcalde del Juego / El Cerebro"
// Este script administra la hoja de vida (GameProgressData) y toma las decisiones.
// -----------------------------------------------------------------------------
public class RE_GameProgress : MonoBehaviour 
{
    // -------------------------------------------------------------------------
    // EXPLICACIÓN DE 'SINGLETON' (Instance) PARA TU SUSTENTACIÓN:
    // Imagina que este script es el Alcalde de la ciudad. Solo puede haber un alcalde.
    // Si un ciudadano (otro script) quiere hablar con él, no tiene que ir preguntando 
    // casa por casa dónde vive. Simplemente llama a "La Oficina del Alcalde" (Instance) de forma directa.
    // Esto hace que el código sea rapidísimo porque todos saben exactamente dónde encontrar el progreso del juego.
    // -------------------------------------------------------------------------
    public static RE_GameProgress Instance { get; private set; }

    [Header("Datos de Progreso")]
    // Creamos nuestra hoja de papel en blanco usando la clase de arriba.
    public GameProgressData progressData = new GameProgressData(); 

    [Header("Ajustes de Porcentaje")]
    [Tooltip("Cantidad total de misiones principales necesarias para llegar al 100%.")]
    public int totalMainTasks = 4; // Por defecto son 4 NPCs (Guardia, Civil, Enfermero, Recepcionista).

    [Header("Herramientas de Prueba")]
    [Tooltip("Dale a Play con esto marcado para borrar la partida guardada y empezar de 0.")]
    public bool reiniciarAlIniciar = false; // Trampa para que los programadores prueben el juego desde cero.

    [Header("Referencias de UI (HUD)")]
    // Estos son los enlaces a los objetos gráficos de la pantalla (El 100% y la barra verde de progreso).
    [SerializeField] private GameObject progressTextObject; 
    [SerializeField] private GameObject progressBarObject; 

    // -------------------------------------------------------------------------
    // EXPLICACIÓN DE DELEGADOS Y EVENTOS (Action):
    // Imagina que esto es una "alarma de incendios". El Alcalde (este script) jala la alarma.
    // No sabe quién va a escucharla (puede ser el script de música, de logros, etc.), 
    // pero él cumple con gritar: "¡LLEGAMOS AL 100%!" y los demás actúan solos.
    // -------------------------------------------------------------------------
    public static event System.Action<float> OnProgressChanged;
    public static event System.Action OnProgress25;
    public static event System.Action OnProgress50;
    public static event System.Action OnProgress75;
    public static event System.Action OnProgress100;

    [Header("Configuración de Guardado")]
    [Tooltip("Clave con la que se guardará el archivo en el sistema.")]
    [SerializeField] private string saveKey = "HospitalGameProgress"; // El nombre de la carpeta virtual donde se guardará.

    private void Awake() // Awake se ejecuta antes de que el juego siquiera respire (antes de Start).
    {
        if (Instance == null) // Si todavía no hay ningún "Alcalde"...
        {
            Instance = this; // ¡Yo seré el Alcalde!
            
            // "DontDestroyOnLoad": Imagina que al pasar al nivel 2, Unity destruye el nivel 1 como si fuera un edificio viejo.
            // Esta función le pone un campo de fuerza a nuestro Alcalde para que sobreviva a la demolición y pase al Nivel 2.
            DontDestroyOnLoad(gameObject); 
            
            if (reiniciarAlIniciar) ResetProgress(); // Si el programador activó la trampa, limpiamos la partida.
            else LoadProgress(); // Si no, cargamos la partida desde el disco duro.
        }
        else // Si ya existía otro Alcalde (porque venimos de otro nivel y ya había uno)...
        {
            if (reiniciarAlIniciar) Instance.ResetProgress(); 
            Destroy(gameObject); // Nos destruimos a nosotros mismos porque no puede haber 2 Alcaldes.
        }
    }

    private void OnEnable() // Cuando este script despierta...
    {
        SceneManager.sceneLoaded += OnSceneLoaded; // Le decimos a Unity: "Avísame cada vez que termines de cargar un nivel nuevo".
    }

    private void OnDisable() // Cuando apagamos el script...
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Dejamos de pedirle avisos a Unity.
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // Esta función se llama sola cuando entramos a un nuevo nivel
    {
        DetectUIComponents(); // Volvemos a buscar dónde están los textos en la pantalla.
        ActualizarUI(GetProgressPercentage()); // Refrescamos los números.
    }

    private void Start() // Primer latido del juego
    {
        DetectUIComponents(); 
        ActualizarUI(GetProgressPercentage()); 
    }

    /// <summary>
    /// Función detective: Si olvidaste arrastrar los textos al Inspector, esta función los busca por todo el mapa.
    /// </summary>
    private void DetectUIComponents()
    {
        // Si no tenemos el objeto de texto conectado...
        if (progressTextObject == null)
        {
            progressTextObject = GameObject.Find("Porcentaje"); // Lo buscamos por su nombre en la placa de la puerta.
            if (progressTextObject == null) // Si sigue sin aparecer...
            {
                // Revisamos ABSOLUTAMENTE TODOS los objetos ocultos del mapa
                foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) 
                {
                    // Si encontramos uno que se llame "porcentaje" o "progreso", nos lo quedamos.
                    if (go.hideFlags == HideFlags.None && (go.name.ToLower().Contains("porcentaje") || go.name.ToLower().Contains("progreso")))
                    {
                        progressTextObject = go;
                        break;
                    }
                }
            }
        }

        // Hacemos el mismo trabajo de detective, pero buscando la barra visual.
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
    /// Guarda la partida. 
    /// METÁFORA: Mete nuestra hoja de datos (GameProgressData) en una fotocopiadora que la convierte en texto (JSON),
    /// y luego guarda ese texto en una gaveta especial del computador llamada "PlayerPrefs".
    /// </summary>
    public void SaveProgress() 
    {
        try // 'Try' significa: "Intenta hacer esto, pero si explota (ej: disco lleno), no me cierres el juego".
        {
            string json = JsonUtility.ToJson(progressData, true); // Convierte los datos a un texto universal JSON.
            PlayerPrefs.SetString(saveKey, json); // Mete el texto a la gaveta de Windows/Mac.
            PlayerPrefs.Save(); // Cierra con llave para asegurar el guardado.
            Debug.Log("[RE_GameProgress] Progreso guardado con éxito.");
        }
        catch (System.Exception e) // Si falla...
        {
            Debug.LogError($"[RE_GameProgress] Error al guardar el progreso: {e.Message}"); // Reportar error a los programadores
        }
    }

    /// <summary>
    /// Carga la partida. Hace el proceso contrario al guardado.
    /// </summary>
    public void LoadProgress() 
    {
        if (PlayerPrefs.HasKey(saveKey)) // Si la gaveta (archivo de guardado) existe...
        {
            try
            {
                string json = PlayerPrefs.GetString(saveKey); // Saca el papel arrugado con texto de la gaveta.
                JsonUtility.FromJsonOverwrite(json, progressData); // Plancha el papel y lo vuelve a convertir en nuestra clase ordenada.
                Debug.Log("[RE_GameProgress] Progreso cargado con éxito.");
            }
            catch (System.Exception e) // Si el archivo está corrupto (alguien lo hackeó o se apagó la luz guardando)
            {
                Debug.LogError($"[RE_GameProgress] Partida corrupta. Iniciando por defecto: {e.Message}");
                progressData = new GameProgressData(); // Le entregamos una hoja de vida en blanco nueva.
            }
        }
        else // Si es la primera vez que la persona juega
        {
            Debug.Log("[RE_GameProgress] No se encontró partida. Iniciando nueva.");
            progressData = new GameProgressData(); // Crea contenedor vacío y nuevo.
        }
        ActualizarUI(GetProgressPercentage()); // Mostramos en la pantalla del jugador lo que cargamos.
    }

    /// <summary>
    /// Borra definitivamente la partida de la memoria del computador (Hard Reset)
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(saveKey); // Quema el archivo del computador.
        progressData = new GameProgressData(); // Limpia los datos de la memoria RAM del juego.
        ActualizarUI(0f); // Pone la pantalla en 0%
    }

    /// <summary>
    /// Solo borra las misiones hechas en ESTE nivel (Soft Reset).
    /// Si pierdes y mueres, no pierdes tus mejoras máximas, solo el progreso del mapa actual.
    /// </summary>
    public void ResetLevelProgressOnly()
    {
        progressData.completedTasks.Clear(); // Borramos con borrador mágico la lista de tareas.
        progressData.reached25 = false; // Le quitamos las insignias.
        progressData.reached50 = false;
        progressData.reached75 = false;
        progressData.reached100 = false;

        ActualizarUI(0f);
        SaveProgress(); // Guardamos este borrado.
    }

    #region Métodos de Utilidad / Atajos (Lógica de Misiones)

    /// <summary>
    /// Esta función es como ponerle un "Chulo" (Check) a una misión de tu lista.
    /// </summary>
    public void CompleteTask(string taskId)
    {
        if (!progressData.completedTasks.Contains(taskId)) // Si la tarea no estaba ya marcada con chulo...
        {
            progressData.completedTasks.Add(taskId); // La añadimos a la lista de "Cosas ya hechas".
            
            float currentPercent = GetProgressPercentage(); // Sacamos matemáticas (ej: llevamos 2 de 4 misiones = 50%).
            
            OnProgressChanged?.Invoke(currentPercent); // Gritamos la alarma de que cambió el progreso para que otras cosas actúen.
            CheckProgressMilestones(currentPercent); // Revisamos si cruzamos la meta del 25%, 50%, etc.
            ActualizarUI(currentPercent); // Dibujamos el nuevo número en la pantalla.

            SaveProgress(); // Guardamos automáticamente para evitar perder la misión si se va la luz.
        }
    }

    /// <summary>
    /// Matemáticas básicas: Regla de tres simple para sacar el porcentaje base 100.
    /// </summary>
    public float GetProgressPercentage()
    {
        if (totalMainTasks <= 0) return 0f; // Evitar que el universo explote al dividir por cero.
        float percent = ((float)progressData.completedTasks.Count / totalMainTasks) * 100f; // Tareas Hechas divididas entre Tareas Totales.
        return Mathf.Clamp(percent, 0f, 100f); // Asegurar que el porcentaje NUNCA se pase de 100 ni baje de 0.
    }

    /// <summary>
    /// Vigila si el jugador cruzó metas importantes. (Como los cuartos de hora de un reloj).
    /// </summary>
    private void CheckProgressMilestones(float percentage)
    {
        // Si llegamos a 25% y todavía no habíamos reclamado la medalla...
        if (percentage >= 25f && !progressData.reached25)
        {
            progressData.reached25 = true; // Reclamamos la medalla (para que no nos la vuelvan a dar).
            OnProgress25?.Invoke(); // Disparamos fuegos artificiales de 25%.
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
            progressData.reached100 = true; 
            OnProgress100?.Invoke(); // Esta orden específica avisa al nivel que ya ganamos (LevelComplete escucha este grito).
        }
    }

    /// <summary>
    /// Responde a la pregunta: ¿Ya hice esta tarea?
    /// </summary>
    public bool IsTaskCompleted(string taskId) { return progressData.completedTasks.Contains(taskId); }

    // Estas 3 funciones son atajos específicos para nuestra historia del hospital.
    // Buscan la palabra clave en nuestra libreta de misiones para saber con quién ya hablamos.
    public bool IsGuardiaCompleted() { foreach (string task in progressData.completedTasks) if (task.ToLower().Contains("guardia")) return true; return false; }
    public bool IsCivilCompleted() { foreach (string task in progressData.completedTasks) if (task.ToLower().Contains("civil")) return true; return false; }
    public bool IsEnfermeroCompleted() { foreach (string task in progressData.completedTasks) if (task.ToLower().Contains("enfermero")) return true; return false; }

    /// <summary>
    /// Modifica los píxeles de la pantalla del jugador (UI) para mostrar los números reales.
    /// </summary>
    public void ActualizarUI(float porcentaje)
    {
        // 1. Preparamos el texto final. El ":0" asegura que no haya decimales molestos. Ej: "Progreso 50%" en vez de "50.134%"
        string textoFormateado = $"Progreso {porcentaje:0}%"; 

        // 2. DIBUJAMOS EL TEXTO ESCRITO EN LA PANTALLA
        // Si el diseñador conectó un objeto de texto en el Inspector...
        if (progressTextObject != null)
        {
            // Intentamos buscar si es un texto moderno de alta resolución (TextMeshPro)
            TextMeshProUGUI textTMP = progressTextObject.GetComponent<TextMeshProUGUI>();
            
            // Si no lo encontramos en la superficie, lo buscamos en los hijos (adentro del objeto)
            if (textTMP == null) textTMP = progressTextObject.GetComponentInChildren<TextMeshProUGUI>();
            
            // Si sí era un texto moderno, le inyectamos la frase que preparamos en el paso 1
            if (textTMP != null) textTMP.text = textoFormateado;
            else
            {
                // Si no era moderno, intentamos buscar si es un texto feo/antiguo de Unity (Legacy Text)
                Text textLegacy = progressTextObject.GetComponent<Text>();
                
                // Buscamos en los hijos por si acaso
                if (textLegacy == null) textLegacy = progressTextObject.GetComponentInChildren<Text>();
                
                // Si sí lo encontramos, le inyectamos el texto
                if (textLegacy != null) textLegacy.text = textoFormateado;
            }
        }

        // 3. DIBUJAMOS LA BARRA DE PROGRESO (GRÁFICA VISUAL)
        // Si el diseñador conectó un objeto visual de barra en el Inspector...
        if (progressBarObject != null)
        {
            // Las barras gráficas pueden ser estilo "Slider" (como la barra de volumen de Windows)...
            Slider slider = progressBarObject.GetComponent<Slider>();
            
            // Buscamos en los hijos por si acaso
            if (slider == null) slider = progressBarObject.GetComponentInChildren<Slider>();
            
            // Si sí era un Slider...
            if (slider != null)
            {
                // Matemáticas: Convertimos nuestro porcentaje (0-100) en una fracción (0.0 - 1.0) 
                // y lo multiplicamos por el valor máximo que permita el Slider.
                slider.value = (porcentaje / 100f) * slider.maxValue; 
            }
            else
            {
                // ...O pueden ser estilo "Filled Image" (Un círculo que se va rellenando poco a poco)
                Image image = progressBarObject.GetComponent<Image>();
                
                // Buscamos en los hijos
                if (image == null) image = progressBarObject.GetComponentInChildren<Image>();
                
                // Si sí era un Filled Image...
                if (image != null) 
                {
                    // Asignamos un valor crudo entre 0.0 y 1.0 (Ej: 0.5 llena la barra a la mitad).
                    image.fillAmount = porcentaje / 100f; 
                }
            }
        }
    }
    #endregion
}
