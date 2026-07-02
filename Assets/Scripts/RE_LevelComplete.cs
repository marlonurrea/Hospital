using UnityEngine; // Librería principal de Unity para matemáticas e interacción de mundos 3D
using UnityEngine.UI; // Herramientas de Interfaz de Usuario viejas (Botones antiguos)
using UnityEngine.SceneManagement; // El encargado de empaquetar un mapa y pasar al siguiente (Gestor de Escenas)
using System.Collections.Generic; // Librería de "Mochilas" (Listas para guardar cosas múltiples)
using TMPro; // Herramienta para Textos 4K nítidos (Text Mesh Pro)

// -----------------------------------------------------------------------------
// SCRIPT: RE_LevelComplete
// METÁFORA: "El Notario / El Juez del Nivel"
// Este script es el que dicta sentencia cuando terminas. Apaga el movimiento, 
// te quita el control del cuerpo, saca la calculadora para ver cómo te fue (estadísticas)
// y te abre la puerta al siguiente nivel.
// -----------------------------------------------------------------------------
public class RE_LevelComplete : MonoBehaviour 
{
    // Patrón Singleton
    // METÁFORA: "La Sala Principal del Juzgado". Solo hay una en cada nivel, 
    // y si alguien gana el juego, llama directamente a esta sala sin tener que buscarla.
    public static RE_LevelComplete Instance { get; private set; }

    [Header("Referencias de UI (Pantalla de Éxito)")] 
    [Tooltip("El GameObject del panel que contiene la interfaz de nivel completado.")]
    [SerializeField] private GameObject levelCompletePanel; // La pantalla oscura de victoria que tapa el juego.

    [Tooltip("Texto para mostrar el tiempo que le tomó al jugador completar el nivel (TMP).")]
    [SerializeField] private TextMeshProUGUI timeText; // La hoja de papel donde imprimiremos cuánto tiempo jugaste.

    [Tooltip("Texto para mostrar el número de tareas completadas de forma amigable (TMP).")]
    [SerializeField] private TextMeshProUGUI tasksText; // La hoja de papel donde imprimiremos tus misiones hechas.

    [Tooltip("Texto para mostrar la salud restante del jugador al terminar el nivel (TMP).")]
    [SerializeField] private TextMeshProUGUI healthText; // La hoja de papel donde imprimiremos con qué vida terminaste.

    [Header("Botones de Navegación")] 
    [SerializeField] private Button nextLevelButton; // El botón físico en tu pantalla de "Siguiente".
    [SerializeField] private Button restartButton; // El botón físico en tu pantalla de "Reiniciar".
    [SerializeField] private Button mainMenuButton; // El botón físico en tu pantalla de "Salir al Menú".

    [Header("Ajustes de Escenas")] 
    [Tooltip("Nombre de la escena correspondiente al siguiente nivel del juego.")]
    [SerializeField] private string nextSceneName = "Hospital_Level2"; // La etiqueta/nombre del siguiente cuarto al que saltaremos.

    [Tooltip("Nombre de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal"; // Etiqueta del menú de inicio.

    [Header("Efectos y Audio")] 
    [Tooltip("Sonido de victoria que se reproduce al completar el nivel.")]
    [SerializeField] private AudioClip completeSFX; // El archivo .mp3 o .wav del ruido de victoria (Fanfarria).

    [Tooltip("Reproductor de audio. Si se deja vacío, se buscará o creará uno en el GameObject.")]
    [SerializeField] private AudioSource audioSource; // El parlante (bocina) físico 3D que reproducirá el archivo de arriba.

    [Tooltip("Sistema de partículas (ej. confeti, fuegos artificiales) a activar.")]
    [SerializeField] private ParticleSystem completeParticles; // El cañón de confeti.

    [Header("Comportamiento Automático")] 
    [Tooltip("¿Completar el nivel de manera automática cuando el progreso del juego llega al 100%?")]
    [SerializeField] private bool autoCompleteOn100Percent = true; // Interruptor: Falso = El jugador debe pisar una meta física final. Verdadero = El nivel se corta de golpe apenas haces 100%.

    private float levelStartTime; // Reloj de pulsera que anota a qué horas (en segundos) empezó todo.
    private bool isLevelCompleted = false; // Candado: Para que la victoria ocurra 1 SOLA VEZ y no se repita en bucle si hablamos dos veces con el NPC final.

    private void Awake() 
    {
        // Regla del Juez Único. Si no hay juez, yo soy el juez. Si hay otro, me suicido.
        if (Instance == null) Instance = this; 
        else { Destroy(gameObject); return; }
    }

    private void Start() 
    {
        // Time.time son los segundos exactos desde que abriste el programa (el juego en Windows).
        levelStartTime = Time.time; 

        // Ocultamos la pantalla de victoria apenas nacemos, para que no te estorbe la vista al jugar.
        if (levelCompletePanel != null) levelCompletePanel.SetActive(false);

        // A cada botón le asignamos una "misión" (función).
        // METÁFORA: Le estamos atando un hilo invisible al botón que, al ser jalado, activa nuestro código.
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(LoadNextLevel);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(LoadMainMenu);
    }

    private void OnEnable() 
    {
        // Si el juez decide trabajar de forma automática...
        if (autoCompleteOn100Percent) 
        {
            // Se suscribe a la alarma del "Alcalde" (GameProgress). 
            // "Oye, Alcalde, cuando llegues a 100%, pégame un grito a mi función TriggerLevelComplete".
            RE_GameProgress.OnProgress100 += TriggerLevelComplete; 
        }
    }

    private void OnDisable() 
    {
        // Cuando cerremos la escena, desuscribimos la alarma para que no hayan fugas de memoria (errores invisibles).
        if (autoCompleteOn100Percent) RE_GameProgress.OnProgress100 -= TriggerLevelComplete; 
    }

    /// <summary>
    /// Un puente para que otras personas pregunten si el nivel ya acabó.
    /// </summary>
    public bool IsLevelCompleted() 
    {
        return isLevelCompleted; 
    }

    /// <summary>
    /// La sentencia final. Todo se congela y sale la pantalla de victoria.
    /// </summary>
    public void TriggerLevelComplete() 
    {
        // Si la sentencia ya fue dictada antes, nos retiramos. (Evita el bug del bucle).
        if (isLevelCompleted) return; 
        isLevelCompleted = true; // Echamos llave al candado.

        Debug.Log("nivel completado"); // Anotamos en la consola invisible para revisar después.

        // Mostramos el telón gigante que tapa el juego (Pantalla final).
        if (levelCompletePanel != null) levelCompletePanel.SetActive(true);

        // Desbloqueamos el cursor para que dejes de controlar la cámara y puedas hacer clic con el puntero en "Siguiente".
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true; 

        // Buscamos al jugador ("el títere") por todo el mapa y le arrancamos las piernas (apagamos su movimiento).
        RE_PlayerMovement player = FindFirstObjectByType<RE_PlayerMovement>(); 
        if (player != null) player.enabled = false; 

        CalcularYMostrarEstadisticas(); // Llamamos al contador para procesar los números.

        // Si tenemos un cañón de confeti, lo disparamos.
        if (completeParticles != null) completeParticles.Play();

        // Si tenemos un disco con música de victoria...
        if (completeSFX != null) 
        {
            // Buscamos si tenemos un parlante (AudioSource).
            if (audioSource == null) 
            {
                audioSource = GetComponent<AudioSource>(); 
                // Si no hay parlante en ningún lado, construimos un parlante en vivo.
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>(); 
            }
            // Reproducimos el disco una sola vez, sin cortarlo (PlayOneShot).
            audioSource.PlayOneShot(completeSFX); 
        }
    }

    /// <summary>
    /// El contador saca su calculadora.
    /// </summary>
    private void CalcularYMostrarEstadisticas() 
    {
        // Matemáticas: Tiempo Actual (las 3pm) menos el Tiempo Inicial (las 2pm) = Tardaste 1 hora.
        float totalTime = Time.time - levelStartTime; 
        
        // FloorToInt = Redondear hacia abajo. (Si son 115 segundos, 115 / 60 = 1.91. Redondeado es 1 minuto).
        int minutes = Mathf.FloorToInt(totalTime / 60F); 
        // Modulo (%). Nos da el residuo de la división. 115 / 60 sobra 55. (Entonces son 55 segundos).
        int seconds = Mathf.FloorToInt(totalTime % 60F); 

        // Rellenamos el texto con un formato limpio. ":00" fuerza a que el número 5 se escriba "05".
        if (timeText != null) timeText.text = string.Format("Tiempo: {0:00}:{1:00}", minutes, seconds); 

        // Leemos la lista de tareas completadas directo desde la oficina del Alcalde.
        if (RE_GameProgress.Instance != null && tasksText != null) 
        {
            int completed = RE_GameProgress.Instance.progressData.completedTasks.Count; 
            int total = RE_GameProgress.Instance.totalMainTasks; 
            tasksText.text = string.Format("Tareas: {0} / {1}", completed, total); 
        }

        // El usuario solicitó borrar el número de la salud, así que solo imprimimos la palabra "Salud".
        if (healthText != null) healthText.text = "Salud"; 
    }

    /// <summary>
    /// El botón de siguiente nivel jala este hilo.
    /// </summary>
    public void LoadNextLevel() 
    {
        // Borramos el progreso LOCAL de las misiones para empezar en el Nivel 2 con cero misiones hechas.
        if (RE_GameProgress.Instance != null) RE_GameProgress.Instance.ResetLevelProgressOnly();
        
        // Si tenemos un sistema suave de transición a oscuras (Fade To Black), lo usamos.
        if (RE_LevelTransitionManager.Instance != null) RE_LevelTransitionManager.Instance.TransitionToScene(nextSceneName); 
        // Si no, cargamos bruscamente la escena que dice nuestro letrero (nextSceneName).
        else SceneManager.LoadScene(nextSceneName); 
    }

    /// <summary>
    /// El botón de reiniciar nivel jala este hilo.
    /// </summary>
    public void RestartLevel() 
    {
        // Si reiniciamos, es como si hubiéramos muerto. Borramos todo rastro de lo que hicimos en este mapa.
        if (RE_GameProgress.Instance != null) 
        {
            RE_GameProgress.Instance.ResetLevelProgressOnly(); 
            
            // Reconstituimos nuestra vida al máximo antes de reiniciar.
            int maxHealth = RE_GameProgress.Instance.progressData.playerMaxHealth; 
            if (maxHealth <= 0) maxHealth = 100; 
            
            RE_GameProgress.Instance.progressData.RE_PlayerHealth = maxHealth; 
            RE_GameProgress.Instance.SaveProgress(); // Sellamos este guardado.
        }

        // Le preguntamos a Unity cómo se llama la escena exacta donde estamos parados AHORA mismo, para recargarla.
        string currentScene = SceneManager.GetActiveScene().name; 
        
        if (RE_LevelTransitionManager.Instance != null) RE_LevelTransitionManager.Instance.TransitionToScene(currentScene);
        else SceneManager.LoadScene(currentScene);
    }

    /// <summary>
    /// El botón de Menú Principal jala este hilo.
    /// </summary>
    public void LoadMainMenu() 
    {
        if (RE_LevelTransitionManager.Instance != null) RE_LevelTransitionManager.Instance.TransitionToScene(mainMenuSceneName);
        else SceneManager.LoadScene(mainMenuSceneName);
    }

    private void OnDestroy() 
    {
        // Cuando cambiamos de escena, este Juez (Script) se destruye.
        // Cortamos los hilos invisibles de los botones para evitar que jalen algo que ya no existe (Memory Leaks).
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(LoadNextLevel);
        if (restartButton != null) restartButton.onClick.RemoveListener(RestartLevel);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(LoadMainMenu);
    }
}
