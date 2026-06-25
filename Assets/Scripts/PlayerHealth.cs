using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem; // Utiliza el nuevo Input System del proyecto

public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Referencias de UI")]
    [Tooltip("Slider que representa la barra de salud (opcional).")]
    [SerializeField] private Slider healthSlider;

    [Tooltip("Imagen tipo Filled que representa la barra de salud (opcional, ej: Image de barra roja).")]
    [SerializeField] private Image healthImageFill;

    [Tooltip("Texto para mostrar la salud (ej: '100 / 100') (opcional).")]
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Modo Temporizador")]
    [Tooltip("Si está activo, la vida del jugador actuará como un temporizador que disminuye con el tiempo.")]
    [SerializeField] private bool useAsTimer = true;

    [Tooltip("Duración en segundos del nivel (ej: 59 segundos).")]
    [SerializeField] private float levelDuration = 59f;

    [Header("Configuración de Muerte")]
    [Tooltip("Nombre de la escena a cargar cuando el jugador muere.")]
    [SerializeField] private string gameOverSceneName = "Fin del Juego";

    [Header("Pruebas de Desarrollo")]
    [Tooltip("Si está activo, al presionar la tecla K en el teclado recibirás 10 de daño.")]
    [SerializeField] private bool enableTestDamageKey = true;

    private float currentHealthTimer;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Forzar la búsqueda y asignación del objeto correcto 'relleno salud' para evitar que se asigne el fondo por error
        GameObject go = GameObject.Find("relleno salud");
        if (go != null)
        {
            healthImageFill = go.GetComponent<Image>();
            if (healthImageFill != null)
            {
                Debug.Log("[PlayerHealth] Asignado 'relleno salud' dinámicamente.");
            }
        }

        // Forzar la búsqueda y asignación del texto de salud
        GameObject textGo = GameObject.Find("Salud");
        if (textGo != null)
        {
            healthText = textGo.GetComponent<TextMeshProUGUI>();
            if (healthText == null)
            {
                healthText = textGo.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (healthText != null)
            {
                Debug.Log("[PlayerHealth] Asignado texto 'Salud' dinámicamente.");
            }
        }

        if (GameProgress.Instance != null)
        {
            int maxHealth = GameProgress.Instance.progressData.playerMaxHealth;
            if (maxHealth <= 0)
            {
                maxHealth = 100;
                GameProgress.Instance.progressData.playerMaxHealth = 100; // Asegurar que quede corregido en memoria
            }
            currentHealthTimer = maxHealth;
            GameProgress.Instance.progressData.playerHealth = maxHealth;
            Debug.Log($"[PlayerHealth] Start: Inicializado a {maxHealth} (timer: {currentHealthTimer})");
        }
        else
        {
            currentHealthTimer = 100f;
            Debug.Log($"[PlayerHealth] Start: GameProgress es nulo, inicializado a 100");
        }
        UpdateHealthUI();
    }

    private void Update()
    {
        if (useAsTimer)
        {
            // Detener el temporizador si el nivel ya se completó
            if (LevelComplete.Instance != null && LevelComplete.Instance.IsLevelCompleted())
            {
                return;
            }

            // Calcular cuánto daño equivale a 1 segundo de tiempo
            int maxHealth = GameProgress.Instance != null ? GameProgress.Instance.progressData.playerMaxHealth : 100;
            if (maxHealth <= 0) maxHealth = 100;

            float decreaseRate = (float)maxHealth / levelDuration;
            currentHealthTimer -= decreaseRate * Time.deltaTime;

            if (currentHealthTimer < 0)
            {
                currentHealthTimer = 0;
            }

            if (GameProgress.Instance != null)
            {
                GameProgress.Instance.progressData.playerHealth = Mathf.CeilToInt(currentHealthTimer);
            }

            UpdateHealthUI();

            if (currentHealthTimer <= 0)
            {
                Die();
            }
        }

        // Tecla de prueba para recibir daño rápidamente (reduce tiempo restante en modo temporizador)
        if (enableTestDamageKey && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("[Prueba] Tecla K presionada. Perdiendo 10 segundos/puntos de vida.");
            TakeDamage(10);
        }
    }

    /// <summary>
    /// Resta vida al jugador (o tiempo restante si el modo temporizador está activo), actualiza la UI y verifica si ha muerto.
    /// </summary>
    /// <param name="amount">Cantidad de daño/tiempo a restar.</param>
    public void TakeDamage(int amount)
    {
        if (GameProgress.Instance == null)
        {
            Debug.LogWarning("[PlayerHealth] No se encontró la instancia de GameProgress. No se puede guardar el daño.");
            return;
        }

        if (useAsTimer)
        {
            currentHealthTimer -= amount;
            if (currentHealthTimer < 0) currentHealthTimer = 0;
            GameProgress.Instance.progressData.playerHealth = Mathf.CeilToInt(currentHealthTimer);
        }
        else
        {
            GameProgress.Instance.progressData.playerHealth -= amount;
            if (GameProgress.Instance.progressData.playerHealth < 0)
            {
                GameProgress.Instance.progressData.playerHealth = 0;
            }
        }

        UpdateHealthUI();
        GameProgress.Instance.SaveProgress();

        if (GameProgress.Instance.progressData.playerHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Cura al jugador (o añade tiempo restante si el modo temporizador está activo).
    /// </summary>
    /// <param name="amount">Cantidad de curación/tiempo a añadir.</param>
    public void Heal(int amount)
    {
        if (GameProgress.Instance == null) return;

        int maxHealth = GameProgress.Instance.progressData.playerMaxHealth;
        if (maxHealth <= 0) maxHealth = 100;
        
        if (useAsTimer)
        {
            currentHealthTimer += amount;
            if (currentHealthTimer > maxHealth) currentHealthTimer = maxHealth;
            GameProgress.Instance.progressData.playerHealth = Mathf.CeilToInt(currentHealthTimer);
        }
        else
        {
            GameProgress.Instance.progressData.playerHealth += amount;
            if (GameProgress.Instance.progressData.playerHealth > maxHealth)
            {
                GameProgress.Instance.progressData.playerHealth = maxHealth;
            }
        }

        UpdateHealthUI();
        GameProgress.Instance.SaveProgress();
    }

    /// <summary>
    /// Sincroniza y actualiza la interfaz visual de salud (barra y/o texto).
    /// </summary>
    public void UpdateHealthUI()
    {
        if (GameProgress.Instance == null) return;

        int current = GameProgress.Instance.progressData.playerHealth;
        int max = GameProgress.Instance.progressData.playerMaxHealth;
        
        if (max <= 0)
        {
            max = 100; // Evitar división por cero si max es 0 o menor
        }

        float percent = (float)current / max;

        // 1. Si usas un Slider de UI
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        // 2. Si usas una Imagen con tipo de llenado "Filled" (muy común en barras personalizadas)
        if (healthImageFill != null)
        {
            healthImageFill.fillAmount = percent;
        }

        // 3. Si tienes un texto de texto para los valores numéricos o de tiempo
        if (healthText != null)
        {
            if (useAsTimer)
            {
                // Formatear el tiempo restante en formato Minutos:Segundos
                int minutes = Mathf.FloorToInt(currentHealthTimer / 60f);
                int seconds = Mathf.FloorToInt(currentHealthTimer % 60f);
                healthText.text = string.Format("Tiempo: {0:00}:{1:00}", minutes, seconds);
            }
            else
            {
                healthText.text = $"Salud: {current} / {max}";
            }
        }
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] El jugador ha muerto. Cargando pantalla de Fin del Juego.");
        
        // Restablecer la salud al máximo para cuando reinicie la partida
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.progressData.playerHealth = GameProgress.Instance.progressData.playerMaxHealth;
            GameProgress.Instance.SaveProgress();
        }

        // Cargar escena de fin de juego con transición suave si existe
        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(gameOverSceneName);
        }
        else
        {
            SceneManager.LoadScene(gameOverSceneName);
        }
    }
}
