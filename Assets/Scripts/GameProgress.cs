using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Contenedor serializable para todos los datos del progreso del juego.
/// </summary>
[System.Serializable]
public class GameProgressData
{
    [Header("Datos Generales")]
    public string currentSceneName = "Hospital_Lobby";
    public int playerHealth = 100;
    public int playerMaxHealth = 100;

    [Header("Progreso del Juego")]
    public List<string> completedTasks = new List<string>();
    public List<string> keycardsObtained = new List<string>();
    public int currentObjectiveIndex = 0;

    [Header("Posición del Jugador")]
    public bool hasSavedPosition = false;
    public float playerPosX;
    public float playerPosY;
    public float playerPosZ;

    [Header("Hitos de Progreso")]
    public bool reached25 = false;
    public bool reached50 = false;
    public bool reached75 = false;
    public bool reached100 = false;
}

public class GameProgress : MonoBehaviour
{
    // Instancia única (Singleton) accesible desde cualquier script
    public static GameProgress Instance { get; private set; }

    [Header("Datos de Progreso")]
    public GameProgressData progressData = new GameProgressData();

    [Header("Ajustes de Porcentaje")]
    [Tooltip("Cantidad total de misiones principales necesarias para llegar al 100%.")]
    public int totalMainTasks = 4;

    [Header("Herramientas de Prueba")]
    [Tooltip("Marca esta casilla y dale a Play para borrar la partida guardada y empezar en 0%. luego desmárcala.")]
    public bool reiniciarAlIniciar = false;

    [Header("Referencias de UI (HUD)")]
    [Tooltip("El GameObject que tiene el texto de porcentaje (ej: el objeto Porcentaje).")]
    [SerializeField] private GameObject progressTextObject;

    [Tooltip("El GameObject que tiene la barra de progreso (ej: el objeto Barra de progreso).")]
    [SerializeField] private GameObject progressBarObject;

    // Delegados/Eventos para suscribirse desde otros scripts (UI, lógica de juego, etc.)
    public static event System.Action<float> OnProgressChanged;
    public static event System.Action OnProgress25;
    public static event System.Action OnProgress50;
    public static event System.Action OnProgress75;
    public static event System.Action OnProgress100;

    [Header("Configuración de Guardado")]
    [Tooltip("Clave con la que se guardará el JSON en los PlayerPrefs.")]
    [SerializeField] private string saveKey = "HospitalGameProgress";

    private void Awake()
    {
        // Implementación del patrón Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (reiniciarAlIniciar)
            {
                ResetProgress();
            }
            else
            {
                LoadProgress(); // Cargar progreso guardado automáticamente
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        DetectUIComponents();
        ActualizarUI(GetProgressPercentage());
    }

    /// <summary>
    /// Intenta buscar y asignar automáticamente los componentes de UI en los hijos si no están asignados o están incorrectos.
    /// </summary>
    private void DetectUIComponents()
    {
        // Buscar el texto si no está asignado o no tiene un componente de texto válido
        if (progressTextObject == null || 
            (progressTextObject.GetComponent<TextMeshProUGUI>() == null && 
             progressTextObject.GetComponentInChildren<TextMeshProUGUI>() == null &&
             progressTextObject.GetComponent<Text>() == null && 
             progressTextObject.GetComponentInChildren<Text>() == null))
        {
            TextMeshProUGUI tmp = GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) 
            {
                progressTextObject = tmp.gameObject;
            }
            else
            {
                Text txt = GetComponentInChildren<Text>();
                if (txt != null) progressTextObject = txt.gameObject;
            }
        }

        // Buscar la barra de progreso (Slider o Image tipo Filled) si no está asignada o no es válida
        if (progressBarObject == null ||
            (progressBarObject.GetComponent<Slider>() == null &&
             progressBarObject.GetComponentInChildren<Slider>() == null &&
             progressBarObject.GetComponent<Image>() == null &&
             progressBarObject.GetComponentInChildren<Image>() == null))
        {
            Slider slider = GetComponentInChildren<Slider>();
            if (slider != null) 
            {
                progressBarObject = slider.gameObject;
            }
            else
            {
                // Buscar imágenes de tipo Filled en los hijos (excluyendo el objeto de texto)
                Image[] images = GetComponentsInChildren<Image>();
                foreach (Image img in images)
                {
                    if (img.type == Image.Type.Filled && img.gameObject != progressTextObject)
                    {
                        progressBarObject = img.gameObject;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Guarda el estado actual de progressData a formato JSON en PlayerPrefs.
    /// </summary>
    public void SaveProgress()
    {
        try
        {
            string json = JsonUtility.ToJson(progressData, true);
            PlayerPrefs.SetString(saveKey, json);
            PlayerPrefs.Save();
            Debug.Log("[GameProgress] Progreso guardado con éxito.");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[GameProgress] Error al guardar el progreso: {e.Message}");
        }
    }

    /// <summary>
    /// Carga el estado del progreso desde PlayerPrefs. Si no existe, inicia datos por defecto.
    /// </summary>
    public void LoadProgress()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            try
            {
                string json = PlayerPrefs.GetString(saveKey);
                JsonUtility.FromJsonOverwrite(json, progressData);
                Debug.Log("[GameProgress] Progreso cargado con éxito.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameProgress] Error al cargar el progreso (se usaron datos por defecto): {e.Message}");
                progressData = new GameProgressData();
            }
        }
        else
        {
            Debug.Log("[GameProgress] No se encontró partida guardada. Iniciando partida nueva.");
            progressData = new GameProgressData();
        }

        ActualizarUI(GetProgressPercentage());
    }

    /// <summary>
    /// Borra el progreso guardado en PlayerPrefs y restablece los datos en memoria.
    /// </summary>
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(saveKey);
        progressData = new GameProgressData();
        ActualizarUI(0f);
        Debug.Log("[GameProgress] Progreso restablecido y eliminado de la memoria persistente.");
    }

    #region Métodos de Utilidad / Atajos

    /// <summary>
    /// Marca una tarea como completada si no lo estaba ya.
    /// </summary>
    public void CompleteTask(string taskId)
    {
        if (!progressData.completedTasks.Contains(taskId))
        {
            progressData.completedTasks.Add(taskId);
            Debug.Log($"[GameProgress] Tarea completada: {taskId}");
            
            float currentPercent = GetProgressPercentage();
            Debug.Log($"[GameProgress] Progreso actual del juego: {currentPercent}%");

            // Disparar eventos
            OnProgressChanged?.Invoke(currentPercent);
            CheckProgressMilestones(currentPercent);
            ActualizarUI(currentPercent);

            SaveProgress(); // Guardar automáticamente al cambiar estado importante
        }
    }

    /// <summary>
    /// Calcula y devuelve el porcentaje actual de progreso.
    /// </summary>
    public float GetProgressPercentage()
    {
        if (totalMainTasks <= 0) return 0f;
        float percent = ((float)progressData.completedTasks.Count / totalMainTasks) * 100f;
        return Mathf.Clamp(percent, 0f, 100f);
    }

    /// <summary>
    /// Verifica si se alcanzan los hitos de 25%, 50%, 75% o 100% y dispara sus eventos respectivos.
    /// </summary>
    private void CheckProgressMilestones(float percentage)
    {
        if (percentage >= 25f && !progressData.reached25)
        {
            progressData.reached25 = true;
            OnProgress25?.Invoke();
            Debug.Log("[GameProgress] ¡Hito del 25% alcanzado!");
        }
        if (percentage >= 50f && !progressData.reached50)
        {
            progressData.reached50 = true;
            OnProgress50?.Invoke();
            Debug.Log("[GameProgress] ¡Hito del 50% alcanzado!");
        }
        if (percentage >= 75f && !progressData.reached75)
        {
            progressData.reached75 = true;
            OnProgress75?.Invoke();
            Debug.Log("[GameProgress] ¡Hito del 75% alcanzado!");
        }
        if (percentage >= 100f && !progressData.reached100)
        {
            progressData.reached100 = true;
            OnProgress100?.Invoke();
            Debug.Log("[GameProgress] ¡Felicidades! ¡Progreso al 100% completado!");
        }
    }

    /// <summary>
    /// Comprueba si una tarea ha sido completada.
    /// </summary>
    public bool IsTaskCompleted(string taskId)
    {
        return progressData.completedTasks.Contains(taskId);
    }

    /// <summary>
    /// Añade una tarjeta de acceso al inventario del jugador.
    /// </summary>
    public void AddKeycard(string keycardId)
    {
        if (!progressData.keycardsObtained.Contains(keycardId))
        {
            progressData.keycardsObtained.Add(keycardId);
            Debug.Log($"[GameProgress] Llave/Tarjeta obtenida: {keycardId}");
            SaveProgress();
        }
    }

    /// <summary>
    /// Comprueba si el jugador posee una tarjeta de acceso específica.
    /// </summary>
    public bool HasKeycard(string keycardId)
    {
        return progressData.keycardsObtained.Contains(keycardId);
    }

    /// <summary>
    /// Guarda las coordenadas actuales del jugador.
    /// </summary>
    public void SavePlayerPosition(Vector3 position)
    {
        progressData.playerPosX = position.x;
        progressData.playerPosY = position.y;
        progressData.playerPosZ = position.z;
        progressData.hasSavedPosition = true;
        Debug.Log($"[GameProgress] Posición del jugador guardada: {position}");
        SaveProgress();
    }

    /// <summary>
    /// Obtiene la posición guardada del jugador. Devuelve Vector3.zero si no hay posición guardada.
    /// </summary>
    public Vector3 GetSavedPlayerPosition()
    {
        if (progressData.hasSavedPosition)
        {
            return new Vector3(progressData.playerPosX, progressData.playerPosY, progressData.playerPosZ);
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Actualiza los textos y las barras de progreso de la interfaz buscando componentes en los GameObjects asignados.
    /// </summary>
    public void ActualizarUI(float porcentaje)
    {
        string textoFormateado = $"Progreso {porcentaje:0}%";

        // 1. Actualizar Texto (TextMeshPro o Legacy Text)
        if (progressTextObject != null)
        {
            TextMeshProUGUI textTMP = progressTextObject.GetComponent<TextMeshProUGUI>();
            if (textTMP == null) textTMP = progressTextObject.GetComponentInChildren<TextMeshProUGUI>();

            if (textTMP != null)
            {
                textTMP.text = textoFormateado;
            }
            else
            {
                Text textLegacy = progressTextObject.GetComponent<Text>();
                if (textLegacy == null) textLegacy = progressTextObject.GetComponentInChildren<Text>();

                if (textLegacy != null)
                {
                    textLegacy.text = textoFormateado;
                }
            }
        }

        // 2. Actualizar Barra de Progreso (Slider o Imagen con Relleno)
        if (progressBarObject != null)
        {
            Slider slider = progressBarObject.GetComponent<Slider>();
            if (slider == null) slider = progressBarObject.GetComponentInChildren<Slider>();

            if (slider != null)
            {
                if (slider.maxValue > 1f)
                {
                    slider.value = (porcentaje / 100f) * slider.maxValue;
                }
                else
                {
                    slider.value = porcentaje / 100f;
                }
            }
            else
            {
                Image image = progressBarObject.GetComponent<Image>();
                if (image == null) image = progressBarObject.GetComponentInChildren<Image>();

                if (image != null)
                {
                    image.fillAmount = porcentaje / 100f;
                }
            }
        }
    }

    #endregion
}
