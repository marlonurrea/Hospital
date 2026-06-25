using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel que contiene las instrucciones del juego (opcional).")]
    [SerializeField] private GameObject instructionsPanel;

    [Header("Configuración de Nivel")]
    [Tooltip("Nombre exacto de la escena del primer nivel.")]
    [SerializeField] private string firstLevelSceneName = "Nivel 1";

    /// <summary>
    /// Inicia el juego cargando el primer nivel (utiliza transición suave si está disponible).
    /// </summary>
    public void PlayGame()
    {
        // Borrar el progreso de PlayerPrefs para empezar una partida nueva limpia
        PlayerPrefs.DeleteKey("HospitalGameProgress");
        PlayerPrefs.DeleteKey("HospitalGam");
        PlayerPrefs.Save();

        // Restablecer el progreso en memoria si ya existiera una instancia
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.ResetProgress();
        }

        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(firstLevelSceneName);
        }
        else
        {
            SceneManager.LoadScene(firstLevelSceneName);
        }
    }

    /// <summary>
    /// Muestra u oculta el panel de instrucciones.
    /// </summary>
    public void ToggleInstructions(bool show)
    {
        if (instructionsPanel != null)
        {
            instructionsPanel.SetActive(show);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el panel de instrucciones en el inspector del MainMenu.");
        }
    }

    /// <summary>
    /// Cierra el juego (funciona en compilación y detiene el playmode en el editor).
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
