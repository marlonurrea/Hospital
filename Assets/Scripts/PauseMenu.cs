using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona la lógica del menú de pausa, permitiendo pausar el juego,
/// reanudarlo, reiniciar el nivel o volver al menú principal.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("El GameObject del panel del menú de pausa que se activará/desactivará.")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal";

    private bool isPaused = false;

    private void Start()
    {
        // Asegurar que al iniciar el panel de pausa esté desactivado
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Detectar si se presiona la tecla Escape o el botón Start del mando
        bool pausePressed = false;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            pausePressed = true;
        }

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
        {
            pausePressed = true;
        }

        if (pausePressed)
        {
            // Evitar pausar si la partida ya terminó (pantalla de victoria o derrota activa)
            bool levelCompleted = LevelComplete.Instance != null && LevelComplete.Instance.IsLevelCompleted();
            bool isGameOver = FindFirstObjectByType<GameOverMenu>() != null;

            if (!levelCompleted && !isGameOver)
            {
                if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }
    }

    /// <summary>
    /// Reanuda el estado normal del juego.
    /// </summary>
    public void Resume()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        Time.timeScale = 1f;
        isPaused = false;

        // Comprobar si hay algún diálogo de NPC activo antes de bloquear el cursor y reactivar movimiento
        bool npcDialogActive = false;
        NPCInteraction[] npcs = FindObjectsByType<NPCInteraction>(FindObjectsSortMode.None);
        foreach (var npc in npcs)
        {
            if (npc.npcDialogosCanvas != null && npc.npcDialogosCanvas.activeSelf)
            {
                npcDialogActive = true;
                break;
            }
        }

        if (npcDialogActive)
        {
            // Si el diálogo está activo, mantenemos el cursor visible e interactuable
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // Si no hay diálogo activo, bloqueamos el cursor y reactivamos movimiento y temporizador
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.SetPaused(false);
            }

            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = true;
            }
        }
    }

    /// <summary>
    /// Pausa el tiempo del juego y muestra la interfaz de pausa.
    /// </summary>
    public void Pause()
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        Time.timeScale = 0f;
        isPaused = true;

        // Liberar el cursor para interactuar con los botones de la UI de pausa
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Pausar el temporizador de salud/vida del jugador
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.SetPaused(true);
        }

        // Desactivar el movimiento del jugador
        PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
        if (pm != null)
        {
            pm.enabled = false;
        }
    }

    /// <summary>
    /// Reinicia la escena del nivel actual restableciendo los datos del progreso correspondientes.
    /// </summary>
    public void RestartLevel()
    {
        // Asegurarse de reanudar el flujo del tiempo antes de cambiar de escena
        Time.timeScale = 1f;

        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.ResetLevelProgressOnly();
            
            int maxHealth = GameProgress.Instance.progressData.playerMaxHealth;
            if (maxHealth <= 0) maxHealth = 100;
            
            GameProgress.Instance.progressData.playerHealth = maxHealth;
            GameProgress.Instance.SaveProgress();
        }

        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"[PauseMenu] Reiniciando nivel actual: {currentScene}");
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
    /// Carga la escena del menú principal.
    /// </summary>
    public void LoadMainMenu()
    {
        // Asegurarse de reanudar el flujo del tiempo antes de cambiar de escena
        Time.timeScale = 1f;

        Debug.Log($"[PauseMenu] Cargando el menú principal: {mainMenuSceneName}");
        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    /// <summary>
    /// Cierra el ejecutable del juego.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[PauseMenu] Cerrando el juego...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
