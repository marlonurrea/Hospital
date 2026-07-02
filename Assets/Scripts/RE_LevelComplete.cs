using UnityEngine; // Librería principal de Unity para scripts
using UnityEngine.UI; // Librería para manipular botones, imágenes y otros elementos de la interfaz de usuario antigua
using UnityEngine.SceneManagement; // Librería necesaria para poder cambiar, reiniciar o cargar niveles
using System.Collections.Generic; // Librería para usar Listas (aunque aquí no se usa mucho, viene por defecto)
using TMPro; // Librería para usar textos mejorados (TextMeshPro)

/// <summary>
/// Gestiona la lógica que ocurre al completar el nivel, mostrando estadísticas de la partida,
/// deteniendo el movimiento del jugador, y permitiendo navegar a otros niveles o al menú.
/// </summary>
public class RE_LevelComplete : MonoBehaviour // Clase para controlar qué pasa cuando terminamos un nivel
{
    // Instancia única (Singleton) que permite que otros scripts llamen a este sin necesidad de buscarlo
    public static RE_LevelComplete Instance { get; private set; }

    [Header("Referencias de UI (Pantalla de Éxito)")] // Título para organizar el Inspector de Unity
    [Tooltip("El GameObject del panel que contiene la interfaz de nivel completado.")] // Explicación de la variable
    [SerializeField] private GameObject levelCompletePanel; // Panel visual que dice "Nivel Completado"

    [Tooltip("Texto para mostrar el tiempo que le tomó al jugador completar el nivel (TMP).")] // Explicación de la variable
    [SerializeField] private TextMeshProUGUI timeText; // Texto en pantalla donde mostraremos el tiempo jugado

    [Tooltip("Texto para mostrar el número de tareas completadas de forma amigable (TMP).")] // Explicación de la variable
    [SerializeField] private TextMeshProUGUI tasksText; // Texto en pantalla para mostrar cuántas tareas hizo

    [Tooltip("Texto para mostrar la salud restante del jugador al terminar el nivel (TMP).")] // Explicación de la variable
    [SerializeField] private TextMeshProUGUI healthText; // Texto en pantalla para mostrar la vida sobrante

    [Header("Botones de Navegación")] // Sección para conectar los botones en el Inspector
    [SerializeField] private Button nextLevelButton; // Botón para ir al siguiente nivel
    [SerializeField] private Button restartButton; // Botón para volver a jugar este nivel
    [SerializeField] private Button mainMenuButton; // Botón para regresar al menú principal

    [Header("Ajustes de Escenas")] // Sección para nombrar las escenas en el Inspector
    [Tooltip("Nombre de la escena correspondiente al siguiente nivel del juego.")] // Explicación
    [SerializeField] private string nextSceneName = "Hospital_Level2"; // Nombre exacto del siguiente nivel

    [Tooltip("Nombre de la escena del menú principal.")] // Explicación
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal"; // Nombre exacto del menú

    [Header("Efectos y Audio")] // Sección para los efectos de victoria
    [Tooltip("Sonido de victoria que se reproduce al completar el nivel.")] // Explicación
    [SerializeField] private AudioClip completeSFX; // Archivo de audio con la música de victoria

    [Tooltip("Reproductor de audio. Si se deja vacío, se buscará o creará uno en el GameObject.")] // Explicación
    [SerializeField] private AudioSource audioSource; // Componente que reproduce el sonido en el mundo

    [Tooltip("Sistema de partículas (ej. confeti, fuegos artificiales) a activar.")] // Explicación
    [SerializeField] private ParticleSystem completeParticles; // Efecto visual de celebración

    [Header("Comportamiento Automático")] // Sección de reglas automáticas
    [Tooltip("¿Completar el nivel de manera automática cuando el progreso del juego llega al 100%?")] // Explicación
    [SerializeField] private bool autoCompleteOn100Percent = true; // Si es verdadero, el nivel acaba solo al llegar a 100%

    private float levelStartTime; // Variable interna para guardar a qué hora empezamos a jugar
    private bool isLevelCompleted = false; // Variable interna para asegurar que la victoria solo ocurra una vez

    private void Awake() // Se ejecuta justo cuando el objeto "nace" o se crea en el juego
    {
        // Inicializar el Singleton para que sea único
        if (Instance == null) // Si aún no existe otro igual
        {
            Instance = this; // Nos asignamos como el principal
        }
        else // Si ya existía uno en la escena
        {
            Destroy(gameObject); // Nos destruimos para evitar duplicados
            return; // Salimos de la función
        }
    }

    private void Start() // Se ejecuta en el primer fotograma después de crearse
    {
        levelStartTime = Time.time; // Guardamos el tiempo exacto en que empezó el nivel

        // Ocultar el panel de nivel completado al iniciar para que no tape la pantalla mientras jugamos
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        // Conectar código a los botones para que hagan algo al darles clic
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(LoadNextLevel); // Cuando le den clic, llama a LoadNextLevel

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel); // Cuando le den clic, llama a RestartLevel

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu); // Cuando le den clic, llama a LoadMainMenu
    }

    private void OnEnable() // Se ejecuta cada vez que el script se enciende o activa
    {
        if (autoCompleteOn100Percent) // Si queremos que termine automático al llegar a 100%
        {
            RE_GameProgress.OnProgress100 += TriggerLevelComplete; // Nos suscribimos a la alerta de que se llegó a 100%
        }
    }

    private void OnDisable() // Se ejecuta cuando el script se apaga o se destruye el objeto
    {
        if (autoCompleteOn100Percent) // Si estábamos suscritos a la alerta
        {
            RE_GameProgress.OnProgress100 -= TriggerLevelComplete; // Nos quitamos de la alerta para evitar errores
        }
    }

    /// <summary>
    /// Devuelve si el nivel ya ha sido completado.
    /// </summary>
    public bool IsLevelCompleted() // Función para que otros scripts pregunten si ya ganamos
    {
        return isLevelCompleted; // Retorna verdadero o falso
    }

    /// <summary>
    /// Dispara los eventos de nivel completado: muestra el panel de UI, calcula las estadísticas, bloquea el movimiento.
    /// </summary>
    public void TriggerLevelComplete() // Función principal que desata la victoria
    {
        if (isLevelCompleted) return; // Si ya habíamos ganado antes, no hacemos nada y salimos
        isLevelCompleted = true; // Confirmamos que ya ganamos para no repetir el proceso

        Debug.Log("nivel completado"); // Mensaje de prueba en la consola para programadores

        // Mostrar Panel de Victoria
        if (levelCompletePanel != null) // Si asignamos el panel en el inspector
        {
            levelCompletePanel.SetActive(true); // Lo hacemos visible
        }

        // Desbloquear el ratón para poder dar clic a los botones
        Cursor.lockState = CursorLockMode.None; // Liberamos el ratón del centro de la pantalla
        Cursor.visible = true; // Lo hacemos visible

        // Desactivar el control de movimiento del jugador para que no siga caminando
        RE_PlayerMovement RE_PlayerMovement = FindFirstObjectByType<RE_PlayerMovement>(); // Buscamos al jugador en el mapa
        if (RE_PlayerMovement != null) // Si lo encontramos
        {
            RE_PlayerMovement.enabled = false; // Le apagamos el script de caminar
        }

        CalcularYMostrarEstadisticas(); // Llamamos a otra función para rellenar los números de tiempo y tareas

        // Activar efectos visuales (partículas/confeti)
        if (completeParticles != null) // Si pusimos un efecto
        {
            completeParticles.Play(); // Reproducimos la explosión de confeti o destellos
        }

        // Reproducir audio de victoria
        if (completeSFX != null) // Si asignamos un sonido
        {
            if (audioSource == null) // Si olvidamos poner un reproductor de audio
            {
                audioSource = GetComponent<AudioSource>(); // Buscamos uno en el mismo objeto
                if (audioSource == null) // Si de plano no hay
                {
                    audioSource = gameObject.AddComponent<AudioSource>(); // Le creamos uno nuevo en este instante
                }
            }
            audioSource.PlayOneShot(completeSFX); // Hacemos sonar la victoria una vez
        }
    }

    /// <summary>
    /// Calcula el tiempo transcurrido en el nivel y actualiza los campos de texto.
    /// </summary>
    private void CalcularYMostrarEstadisticas() // Función que calcula tiempos, vida y tareas y las escribe en pantalla
    {
        float totalTime = Time.time - levelStartTime; // Restamos el tiempo actual con el tiempo en que empezamos para saber cuánto tardó
        int minutes = Mathf.FloorToInt(totalTime / 60F); // Convertimos los segundos totales en minutos
        int seconds = Mathf.FloorToInt(totalTime % 60F); // Obtenemos el residuo para saber los segundos sobrantes

        if (timeText != null) // Si el texto de tiempo existe
        {
            timeText.text = string.Format("Tiempo: {0:00}:{1:00}", minutes, seconds); // Le ponemos el formato de reloj digital
        }

        // Mostrar Tareas completadas
        if (RE_GameProgress.Instance != null && tasksText != null) // Si hay sistema de progreso y texto
        {
            int completed = RE_GameProgress.Instance.progressData.completedTasks.Count; // Contamos cuántas tareas hizo
            int total = RE_GameProgress.Instance.totalMainTasks; // Vemos cuántas eran en total
            tasksText.text = string.Format("Tareas: {0} / {1}", completed, total); // Escribimos "X / Y" tareas
        }
        else if (tasksText != null) // Si falló algo pero el texto existe
        {
            tasksText.text = "Tareas: Completadas"; // Mensaje por defecto
        }

        // Mostrar Salud restante
        if (RE_GameProgress.Instance != null && healthText != null) // Si hay sistema de progreso y texto
        {
            healthText.text = "Salud"; // El usuario solicitó que solo diga "Salud"
        }
        else if (healthText != null) // Fallback
        {
            healthText.text = "Sobrevivido"; // Mensaje por defecto
        }
    }

    /// <summary>
    /// Carga la escena del siguiente nivel de forma suave y reinicia las tareas del nivel.
    /// </summary>
    public void LoadNextLevel() // Función asignada al botón "Siguiente"
    {
        if (RE_GameProgress.Instance != null) // Borramos el progreso de nivel porque pasaremos a uno nuevo
        {
            RE_GameProgress.Instance.ResetLevelProgressOnly();
        }

        Debug.Log("[RE_LevelComplete] Cargando escena del siguiente nivel: " + nextSceneName); // Aviso por consola
        
        if (RE_LevelTransitionManager.Instance != null) // Si hay animador de pantalla (fundido a negro)
        {
            RE_LevelTransitionManager.Instance.TransitionToScene(nextSceneName); // Transición suave
        }
        else // Si no
        {
            SceneManager.LoadScene(nextSceneName); // Carga directa
        }
    }

    /// <summary>
    /// Reinicia la escena actual de forma suave y limpia el progreso del nivel.
    /// </summary>
    public void RestartLevel() // Función asignada al botón "Reiniciar"
    {
        if (RE_GameProgress.Instance != null) // Reiniciamos todo lo del nivel fallido
        {
            RE_GameProgress.Instance.ResetLevelProgressOnly(); // Borramos progreso de nivel
            
            int maxHealth = RE_GameProgress.Instance.progressData.playerMaxHealth; // Buscamos vida máxima
            if (maxHealth <= 0) maxHealth = 100; // Por si hay error, que sea 100
            
            RE_GameProgress.Instance.progressData.RE_PlayerHealth = maxHealth; // Restauramos vida
            RE_GameProgress.Instance.SaveProgress(); // Guardamos cambio
        }

        string currentScene = SceneManager.GetActiveScene().name; // Obtenemos el nombre del mapa en el que estamos
        Debug.Log("[RE_LevelComplete] Reiniciando nivel: " + currentScene); // Aviso en consola
        
        if (RE_LevelTransitionManager.Instance != null)
        {
            RE_LevelTransitionManager.Instance.TransitionToScene(currentScene); // Transición suave al mismo mapa
        }
        else
        {
            SceneManager.LoadScene(currentScene); // Carga brusca al mismo mapa
        }
    }

    /// <summary>
    /// Carga la escena del menú principal de forma suave.
    /// </summary>
    public void LoadMainMenu() // Función para el botón "Menú Principal"
    {
        Debug.Log("[RE_LevelComplete] Cargando menú principal: " + mainMenuSceneName);
        if (RE_LevelTransitionManager.Instance != null)
        {
            RE_LevelTransitionManager.Instance.TransitionToScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnDestroy() // Se ejecuta cuando el objeto es destruido o cambiamos de mapa
    {
        // Quitar la suscripción de los botones para evitar errores de memoria o llamadas accidentales a objetos muertos
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(LoadNextLevel);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(LoadMainMenu);
    }
}
