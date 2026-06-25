using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Gestiona la lógica que ocurre al completar el nivel, mostrando estadísticas de la partida,
/// deteniendo el movimiento del jugador, y permitiendo navegar a otros niveles o al menú.
/// </summary>
public class LevelComplete : MonoBehaviour
{
    // Instancia única (Singleton) accesible desde otros componentes
    public static LevelComplete Instance { get; private set; }

    [Header("Referencias de UI (Pantalla de Éxito)")]
    [Tooltip("El GameObject del panel que contiene la interfaz de nivel completado.")]
    [SerializeField] private GameObject levelCompletePanel;

    [Tooltip("Texto para mostrar el tiempo que le tomó al jugador completar el nivel (TMP).")]
    [SerializeField] private TextMeshProUGUI timeText;

    [Tooltip("Texto para mostrar el número de tareas completadas de forma amigable (TMP).")]
    [SerializeField] private TextMeshProUGUI tasksText;

    [Tooltip("Texto para mostrar la salud restante del jugador al terminar el nivel (TMP).")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Botones de Navegación")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Ajustes de Escenas")]
    [Tooltip("Nombre de la escena correspondiente al siguiente nivel del juego.")]
    [SerializeField] private string nextSceneName = "Hospital_Level2";

    [Tooltip("Nombre de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal";

    [Header("Efectos y Audio")]
    [Tooltip("Sonido de victoria que se reproduce al completar el nivel.")]
    [SerializeField] private AudioClip completeSFX;

    [Tooltip("Reproductor de audio. Si se deja vacío, se buscará o creará uno en el GameObject.")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Sistema de partículas (ej. confeti, fuegos artificiales) a activar.")]
    [SerializeField] private ParticleSystem completeParticles;

    [Header("Comportamiento Automático")]
    [Tooltip("¿Completar el nivel de manera automática cuando el progreso del juego llega al 100%?")]
    [SerializeField] private bool autoCompleteOn100Percent = true;

    private float levelStartTime;
    private bool isLevelCompleted = false;

    private void Awake()
    {
        // Inicializar el Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // Registrar el tiempo en el que inicia el nivel
        levelStartTime = Time.time;

        // Ocultar el panel de nivel completado al iniciar
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(false);
        }

        // Registrar los eventos de los botones del panel
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(LoadNextLevel);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(LoadMainMenu);
    }

    private void OnEnable()
    {
        // Suscribirse al evento de progreso al 100% de GameProgress
        if (autoCompleteOn100Percent)
        {
            GameProgress.OnProgress100 += TriggerLevelComplete;
        }
    }

    private void OnDisable()
    {
        // Cancelar suscripción para evitar pérdidas de memoria
        if (autoCompleteOn100Percent)
        {
            GameProgress.OnProgress100 -= TriggerLevelComplete;
        }
    }

    /// <summary>
    /// Devuelve si el nivel ya ha sido completado.
    /// </summary>
    public bool IsLevelCompleted()
    {
        return isLevelCompleted;
    }

    /// <summary>
    /// Dispara los eventos de nivel completado: muestra el panel de UI, calcula las
    /// estadísticas, bloquea el movimiento del jugador, y desbloquea el cursor del mouse.
    /// </summary>
    public void TriggerLevelComplete()
    {
        // Evitar que se llame múltiples veces
        if (isLevelCompleted) return;
        isLevelCompleted = true;

        Debug.Log("nivel completado");

        // 1. Mostrar Panel de Victoria
        if (levelCompletePanel != null)
        {
            levelCompletePanel.SetActive(true);
        }

        // 2. Desbloquear y hacer visible el cursor para poder pulsar los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Desactivar el control de movimiento del jugador
        PlayerMovement playerMovement = FindFirstObjectByType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        // 4. Calcular y rellenar las estadísticas de UI
        CalcularYMostrarEstadisticas();

        // 5. Activar efectos visuales (partículas/confeti)
        if (completeParticles != null)
        {
            completeParticles.Play();
        }

        // 6. Reproducir audio de victoria
        if (completeSFX != null)
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            audioSource.PlayOneShot(completeSFX);
        }
    }

    /// <summary>
    /// Calcula el tiempo transcurrido en el nivel, lee las tareas y salud desde GameProgress
    /// y actualiza los campos de texto de la UI.
    /// </summary>
    private void CalcularYMostrarEstadisticas()
    {
        // Calcular tiempo de juego formateado en minutos y segundos
        float totalTime = Time.time - levelStartTime;
        int minutes = Mathf.FloorToInt(totalTime / 60F);
        int seconds = Mathf.FloorToInt(totalTime % 60F);

        if (timeText != null)
        {
            timeText.text = string.Format("Tiempo: {0:00}:{1:00}", minutes, seconds);
        }

        // Tareas del nivel (leídas de GameProgress)
        if (GameProgress.Instance != null && tasksText != null)
        {
            int completed = GameProgress.Instance.progressData.completedTasks.Count;
            int total = GameProgress.Instance.totalMainTasks;
            tasksText.text = string.Format("Tareas: {0} / {1}", completed, total);
        }
        else if (tasksText != null)
        {
            tasksText.text = "Tareas: Completadas";
        }

        // Salud restante (leída de GameProgress)
        if (GameProgress.Instance != null && healthText != null)
        {
            int currentHealth = GameProgress.Instance.progressData.playerHealth;
            int maxHealth = GameProgress.Instance.progressData.playerMaxHealth;
            healthText.text = string.Format("Salud: {0} / {1}", currentHealth, maxHealth);
        }
        else if (healthText != null)
        {
            healthText.text = "Sobrevivido";
        }
    }

    /// <summary>
    /// Carga la escena del siguiente nivel de forma suave y reinicia las tareas del nivel.
    /// </summary>
    public void LoadNextLevel()
    {
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.ResetLevelProgressOnly();
        }

        Debug.Log("[LevelComplete] Cargando escena del siguiente nivel: " + nextSceneName);
        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    /// <summary>
    /// Reinicia la escena actual de forma suave y limpia el progreso del nivel.
    /// </summary>
    public void RestartLevel()
    {
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.ResetLevelProgressOnly();
            
            int maxHealth = GameProgress.Instance.progressData.playerMaxHealth;
            if (maxHealth <= 0) maxHealth = 100;
            
            GameProgress.Instance.progressData.playerHealth = maxHealth;
            GameProgress.Instance.SaveProgress();
        }

        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log("[LevelComplete] Reiniciando nivel: " + currentScene);
        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(currentScene);
        }
        else
        {
            SceneManager.LoadScene(currentScene);
        }
    }

    /// <summary>
    /// Carga la escena del menú principal de forma suave.
    /// </summary>
    public void LoadMainMenu()
    {
        Debug.Log("[LevelComplete] Cargando menú principal: " + mainMenuSceneName);
        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    private void OnDestroy()
    {
        // Limpiar los listeners al destruir el objeto para evitar referencias nulas
        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(LoadNextLevel);

        if (restartButton != null)
            restartButton.onClick.RemoveListener(RestartLevel);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(LoadMainMenu);
    }
}
