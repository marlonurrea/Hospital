using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("Nombre de la escena del primer nivel para reiniciar.")]
    [SerializeField] private string levelSceneName = "Nivel 1";

    [Tooltip("Nombre de la escena del menú principal.")]
    [SerializeField] private string mainMenuSceneName = "MenuPrincipal";

    private void Start()
    {
        // Asegurar que el cursor esté libre y visible para poder hacer clic en los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Reinicia el nivel cargando la escena de juego.
    /// </summary>
    public void RestartLevel()
    {
        // Limpiar el progreso del nivel actual y restablecer la salud al máximo al reiniciar
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.ResetLevelProgressOnly();
            
            int maxHealth = GameProgress.Instance.progressData.playerMaxHealth;
            if (maxHealth <= 0) maxHealth = 100; // Evitar que empiece con 0 de vida
            
            GameProgress.Instance.progressData.playerHealth = maxHealth;
            GameProgress.Instance.SaveProgress();
        }

        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(levelSceneName);
        }
        else
        {
            SceneManager.LoadScene(levelSceneName);
        }
    }

    /// <summary>
    /// Carga la escena del menú principal.
    /// </summary>
    public void LoadMainMenu()
    {
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
    /// Cierra la aplicación de juego.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Cerrando el juego...");
        Application.Quit();

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
