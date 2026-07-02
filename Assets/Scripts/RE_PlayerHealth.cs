using UnityEngine; // Librería estándar
using UnityEngine.UI; // UI básica
using TMPro; // Interfaz moderna para textos
using UnityEngine.SceneManagement; // Permite cargar otra escena si morimos
using UnityEngine.InputSystem; // Sistema para detectar teclados modernos o mandos

public class RE_PlayerHealth : MonoBehaviour // Clase principal que maneja la vida o el tiempo del jugador
{
    // Singleton para acceder a esta clase desde otros scripts muy fácilmente
    public static RE_PlayerHealth Instance { get; private set; }

    [Header("Referencias de UI")] // Organización en el Inspector
    [Tooltip("Slider que representa la barra de salud (opcional).")]
    [SerializeField] private Slider healthSlider; // Barra clásica (Slider) para vida

    [Tooltip("Imagen tipo Filled que representa la barra de salud (opcional).")]
    [SerializeField] private Image healthImageFill; // Barra moderna circular o lineal que se vacía

    [Tooltip("Texto para mostrar la salud (ej: '100 / 100') (opcional).")]
    [SerializeField] private TextMeshProUGUI healthText; // Texto en pantalla con números

    [Header("Modo Temporizador")]
    [Tooltip("Si está activo, la vida del jugador actuará como un temporizador que disminuye con el tiempo.")]
    [SerializeField] private bool useAsTimer = true; // Casilla para convertir la barra de vida en reloj contrarreloj

    [Tooltip("Duración en segundos del nivel.")]
    [SerializeField] private float levelDuration = 59f; // Segundos que durará el nivel

    [Header("Configuración de Muerte")]
    [Tooltip("Nombre de la escena a cargar cuando el jugador muere.")]
    [SerializeField] private string gameOverSceneName = "Fin del Juego"; // Escena de "Has Perdido"

    [Header("Pruebas de Desarrollo")]
    [Tooltip("Si está activo, al presionar la tecla K recibirás daño.")]
    [SerializeField] private bool enableTestDamageKey = true; // Botón de trampa para pruebas de los programadores

    private float currentHealthTimer; // Reloj interno para llevar la cuenta si está en modo contrarreloj
    private bool isPaused = false; // Detiene el daño por tiempo si hablamos con un NPC

    /// <summary>
    /// Pausa o reanuda la cuenta atrás de la vida.
    /// </summary>
    public void SetPaused(bool paused) // Función que otras clases pueden llamar (Ej: al abrir un menú)
    {
        isPaused = paused; // Actualizamos el estado interno
        Debug.Log($"[RE_PlayerHealth] Temporizador pausado: {paused}");
    }

    private void Awake() // Se ejecuta antes que nada
    {
        // Configuramos la variable única para el sistema (Singleton)
        if (Instance == null) Instance = this;
        else Destroy(gameObject); // Evitamos duplicados
    }

    private void Start() // Se ejecuta al inicio del nivel
    {
        // Buscar inteligentemente la imagen de "relleno salud" en la interfaz si olvidamos arrastrarla al Inspector
        GameObject go = GameObject.Find("relleno salud");
        if (go != null) healthImageFill = go.GetComponent<Image>();

        // Buscar el texto "Salud" de la misma forma para conectarlo solo
        GameObject textGo = GameObject.Find("Salud");
        if (textGo != null)
        {
            healthText = textGo.GetComponent<TextMeshProUGUI>();
            if (healthText == null) healthText = textGo.GetComponentInChildren<TextMeshProUGUI>();
        }

        // Leer datos del guardado (para saber con cuánta vida entramos al nivel)
        if (RE_GameProgress.Instance != null)
        {
            int maxHealth = RE_GameProgress.Instance.progressData.playerMaxHealth; // Leemos salud máxima
            if (maxHealth <= 0) // Prevención de errores (evita empezar con vida máxima cero)
            {
                maxHealth = 100;
                RE_GameProgress.Instance.progressData.playerMaxHealth = 100;
            }
            
            currentHealthTimer = maxHealth; // Inicializamos reloj interno
            RE_GameProgress.Instance.progressData.RE_PlayerHealth = maxHealth; // Sanamos por completo al empezar el nivel
        }
        else // Si no hay sistema de guardado (juego no conectado)
        {
            currentHealthTimer = 100f; // Asignamos vida de respaldo
        }
        
        UpdateHealthUI(); // Actualizamos la pantalla con la nueva salud
    }

    private void Update() // Se ejecuta todo el tiempo
    {
        if (useAsTimer) // Si estamos en modo Contrarreloj (La vida baja sola)
        {
            // Pausar daño si es necesario o si ya ganamos el nivel
            if (isPaused || (RE_LevelComplete.Instance != null && RE_LevelComplete.Instance.IsLevelCompleted())) return;

            // Calcular cuánto equivale 1 segundo de vida en porcentaje
            int maxHealth = RE_GameProgress.Instance != null ? RE_GameProgress.Instance.progressData.playerMaxHealth : 100;
            if (maxHealth <= 0) maxHealth = 100; // Evitar división por cero

            float decreaseRate = (float)maxHealth / levelDuration; // Cuánta vida quitamos por segundo de la vida real
            currentHealthTimer -= decreaseRate * Time.deltaTime; // Restamos vida basados en fotogramas

            if (currentHealthTimer < 0) currentHealthTimer = 0; // Evitamos vidas negativas (-5)

            // Guardamos la nueva vida en el progreso general
            if (RE_GameProgress.Instance != null) RE_GameProgress.Instance.progressData.RE_PlayerHealth = Mathf.CeilToInt(currentHealthTimer);

            UpdateHealthUI(); // Actualizamos gráficas

            // Si nos quedamos sin vida, ejecutamos la función de muerte
            if (currentHealthTimer <= 0) Die();
        }

        // Truco para desarrolladores: Quitarse vida pulsando K para probar la muerte más rápido
        if (enableTestDamageKey && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            TakeDamage(10); // Hacemos 10 de daño artificial
        }
    }

    /// <summary>
    /// Resta vida (o tiempo) y actualiza la UI.
    /// </summary>
    public void TakeDamage(int amount) // Función pública para que zonas como el fuego nos puedan hacer daño
    {
        if (RE_GameProgress.Instance == null) return; // Si no hay donde guardar, ignoramos

        if (useAsTimer) // Si estamos en modo tiempo, restamos segundos
        {
            currentHealthTimer -= amount;
            if (currentHealthTimer < 0) currentHealthTimer = 0;
            RE_GameProgress.Instance.progressData.RE_PlayerHealth = Mathf.CeilToInt(currentHealthTimer);
        }
        else // Modo daño clásico (por golpes)
        {
            RE_GameProgress.Instance.progressData.RE_PlayerHealth -= amount;
            if (RE_GameProgress.Instance.progressData.RE_PlayerHealth < 0) RE_GameProgress.Instance.progressData.RE_PlayerHealth = 0;
        }

        UpdateHealthUI(); // Refrescar pantalla
        RE_GameProgress.Instance.SaveProgress(); // Guardar avance

        // Morimos si la vida es 0
        if (RE_GameProgress.Instance.progressData.RE_PlayerHealth <= 0) Die();
    }

    /// <summary>
    /// Cura al jugador (o añade tiempo si es temporizador).
    /// </summary>
    public void Heal(int amount) // Función para curarnos (botiquines, pociones)
    {
        if (RE_GameProgress.Instance == null) return;

        int maxHealth = RE_GameProgress.Instance.progressData.playerMaxHealth; // Límite máximo
        if (maxHealth <= 0) maxHealth = 100;
        
        if (useAsTimer) // Mismas reglas que TakeDamage, pero sumando
        {
            currentHealthTimer += amount;
            if (currentHealthTimer > maxHealth) currentHealthTimer = maxHealth;
            RE_GameProgress.Instance.progressData.RE_PlayerHealth = Mathf.CeilToInt(currentHealthTimer);
        }
        else
        {
            RE_GameProgress.Instance.progressData.RE_PlayerHealth += amount;
            if (RE_GameProgress.Instance.progressData.RE_PlayerHealth > maxHealth) RE_GameProgress.Instance.progressData.RE_PlayerHealth = maxHealth;
        }

        UpdateHealthUI();
        RE_GameProgress.Instance.SaveProgress();
    }

    /// <summary>
    /// Sincroniza y actualiza la interfaz visual de salud (barra y/o texto).
    /// </summary>
    public void UpdateHealthUI() // Función interna que sincroniza las matemáticas con las barras de la pantalla
    {
        if (RE_GameProgress.Instance == null) return;

        int current = RE_GameProgress.Instance.progressData.RE_PlayerHealth; // Vida actual numérica
        int max = RE_GameProgress.Instance.progressData.playerMaxHealth; // Vida máxima numérica
        if (max <= 0) max = 100; // Evitar que el juego reviente al dividir entre cero

        float percent = (float)current / max; // Cálculo del porcentaje (0.0 a 1.0) para las barras

        // Actualizar barra clásica (Slider)
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }

        // Actualizar barra moderna circular o rellena
        if (healthImageFill != null)
        {
            healthImageFill.fillAmount = percent;
        }

        // Actualizar los textos con números
        if (healthText != null)
        {
            if (useAsTimer) // Mostrar formato de reloj en minutos y segundos
            {
                int minutes = Mathf.FloorToInt(currentHealthTimer / 60f); // Sacamos los minutos
                int seconds = Mathf.FloorToInt(currentHealthTimer % 60f); // Sacamos los segundos sobrantes
                healthText.text = string.Format("Tiempo: {0:00}:{1:00}", minutes, seconds); // Formato digital
            }
            else // Daño por golpes, el usuario solicitó que solo diga "Salud" sin números
            {
                healthText.text = "Salud";
            }
        }
    }

    private void Die() // Función de muerte
    {
        Debug.Log("[RE_PlayerHealth] El jugador ha muerto."); // Aviso de sistema
        
        // Restauramos la salud al tope en los archivos de guardado para la siguiente partida
        if (RE_GameProgress.Instance != null)
        {
            RE_GameProgress.Instance.progressData.RE_PlayerHealth = RE_GameProgress.Instance.progressData.playerMaxHealth;
            RE_GameProgress.Instance.SaveProgress();
        }

        // Manda al jugador a la escena de GameOver
        if (RE_LevelTransitionManager.Instance != null) RE_LevelTransitionManager.Instance.TransitionToScene(gameOverSceneName);
        else SceneManager.LoadScene(gameOverSceneName);
    }
}
