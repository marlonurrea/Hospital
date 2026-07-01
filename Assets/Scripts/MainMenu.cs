using UnityEngine; // Herramientas básicas de Unity
using UnityEngine.SceneManagement; // Permite cargar diferentes niveles (escenas)

public class MainMenu : MonoBehaviour // Clase para controlar la pantalla principal del juego
{
    [Header("Paneles de la UI")] // Sección en el Inspector para la interfaz
    [Tooltip("Panel que contiene las instrucciones del juego (opcional).")] // Texto guía en Unity
    [SerializeField] private GameObject instructionsPanel; // Variable para arrastrar el panel de instrucciones

    [Header("Configuración de Nivel")] // Sección para los niveles
    [Tooltip("Nombre exacto de la escena del primer nivel.")] // Texto guía en Unity
    [SerializeField] private string firstLevelSceneName = "Nivel 1"; // Nombre del mapa que cargará al darle "Jugar"

    /// <summary>
    /// Inicia el juego cargando el primer nivel (utiliza transición suave si está disponible).
    /// </summary>
    public void PlayGame() // Función que se activa al presionar el botón "Jugar"
    {
        // Borrar el progreso guardado para empezar una partida totalmente nueva
        PlayerPrefs.DeleteKey("HospitalGameProgress"); // Borramos la clave principal de guardado
        PlayerPrefs.DeleteKey("HospitalGam"); // Borramos cualquier otra clave residual
        PlayerPrefs.Save(); // Confirmamos el borrado en el disco duro

        // Si el sistema de progreso del juego ya estaba cargado en memoria...
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.ResetProgress(); // Lo reseteamos internamente
        }

        // Si tenemos un sistema de transición (pantalla en negro que se aclara)
        if (LevelTransitionManager.Instance != null)
        {
            LevelTransitionManager.Instance.TransitionToScene(firstLevelSceneName); // Usamos transición
        }
        else // Si no hay transición
        {
            SceneManager.LoadScene(firstLevelSceneName); // Cargamos la escena de golpe
        }
    }

    /// <summary>
    /// Muestra u oculta el panel de instrucciones.
    /// </summary>
    public void ToggleInstructions(bool show) // Función para el botón de "Instrucciones". Recibe un true o false
    {
        if (instructionsPanel != null) // Nos aseguramos que sí conectaron un panel en el Inspector
        {
            instructionsPanel.SetActive(show); // Lo mostramos o lo ocultamos según el valor de 'show'
        }
        else
        {
            Debug.LogWarning("No se asignó el panel de instrucciones en el inspector del MainMenu."); // Aviso de error
        }
    }

    /// <summary>
    /// Cierra el juego (funciona en compilación y detiene el playmode en el editor).
    /// </summary>
    public void QuitGame() // Función para el botón "Salir"
    {
        Debug.Log("Cerrando el juego..."); // Mensaje en la consola
        Application.Quit(); // Cierra el programa (solo funciona en el juego ya exportado)

        #if UNITY_EDITOR // Si estamos dentro del motor de Unity...
        UnityEditor.EditorApplication.isPlaying = false; // Detenemos la simulación del botón Play
        #endif
    }
}
