using UnityEngine; // Funciones básicas de Unity
using UnityEngine.SceneManagement; // Necesario para cambiar entre escenas (niveles)

public class GameOverMenu : MonoBehaviour // Clase principal para el menú de derrota
{
    [Header("Configuración de Escenas")] // Categoría en el Inspector
    [Tooltip("Nombre de la escena del primer nivel para reiniciar.")] // Explicación de la variable
    [SerializeField] private string levelSceneName = "Nivel 1"; // Nombre del nivel al que volveremos a jugar

    [Tooltip("Nombre de la escena del menú principal.")] // Explicación de la variable
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal"; // Nombre de la escena del menú principal

    private void Start() // Se ejecuta al abrirse esta pantalla (el menú de Game Over)
    {
        // Aseguramos que el jugador pueda usar el ratón para hacer clic en los botones
        Cursor.lockState = CursorLockMode.None; // Desbloqueamos el cursor (para que no esté atrapado en el centro)
        Cursor.visible = true; // Hacemos que el puntero sea visible en pantalla
    }

    /// <summary>
    /// Reinicia el nivel cargando la escena de juego.
    /// </summary>
    public void RestartLevel() // Método que se asigna al botón de "Reintentar" o "Reiniciar"
    {
        // Si existe un sistema de guardado de progreso
        if (RE_GameProgress.Instance != null)
        {
            RE_GameProgress.Instance.ResetLevelProgressOnly(); // Borramos el progreso de este nivel fallido
            
            int maxHealth = RE_GameProgress.Instance.progressData.playerMaxHealth; // Obtenemos la salud máxima
            if (maxHealth <= 0) maxHealth = 100; // Evitamos un error donde la salud sea 0 al iniciar
            
            RE_GameProgress.Instance.progressData.RE_PlayerHealth = maxHealth; // Restauramos la vida al máximo
            RE_GameProgress.Instance.SaveProgress(); // Guardamos estos cambios de reinicio
        }

        // Si tenemos un sistema de transiciones de pantalla (fade in/out)
        if (RE_LevelTransitionManager.Instance != null)
        {
            RE_LevelTransitionManager.Instance.TransitionToScene(levelSceneName); // Usamos transición suave
        }
        else // Si no hay transiciones suaves...
        {
            SceneManager.LoadScene(levelSceneName); // Cargamos la escena directamente de forma brusca
        }
    }

    /// <summary>
    /// Carga la escena del menú principal.
    /// </summary>
    public void LoadMainMenu() // Método que se asigna al botón "Salir al menú"
    {
        if (RE_LevelTransitionManager.Instance != null) // Si hay transiciones suaves
        {
            RE_LevelTransitionManager.Instance.TransitionToScene(mainMenuSceneName); // Transición al menú principal
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName); // Carga directa del menú principal
        }
    }

    /// <summary>
    /// Cierra la aplicación de juego.
    /// </summary>
    public void QuitGame() // Método que se asigna al botón "Salir del juego"
    {
        Debug.Log("Cerrando el juego..."); // Imprimimos en consola que nos vamos
        Application.Quit(); // Cierra el juego (solo funciona cuando ya está exportado/compilado)

        #if UNITY_EDITOR // Esto solo se compila si estamos dentro del editor de Unity
        UnityEditor.EditorApplication.isPlaying = false; // Detiene la simulación en el editor
        #endif
    }
}
