using UnityEngine;
using UnityEngine.SceneManagement; // Importado para posible uso futuro (ej: volver al menú)
using UnityEngine.InputSystem; // <-- AÑADIDO PARA DETECTAR TECLAS EN TU PROYECTO

// Controla el sistema de pausa del juego con la tecla P
public class PauseMenu : MonoBehaviour
{
    public GameObject panelPause; // Panel de la UI (la pantalla oscura con botones) que se muestra al pausar
    public bool pause = false;    // Estado actual: true = pausado, false = jugando

    // Update se ejecuta una vez por frame — aquí detectamos las teclas en tiempo real
    void Update()
    {
        // Detectar si presionan la tecla P usando el Nuevo Input System
        bool pPressed = false;
        
        // Forma antigua (por si el proyecto tiene ambos activados)
        if (Input.GetKeyDown(KeyCode.P)) pPressed = true;
        
        // Forma nueva (la que usan tus demás scripts)
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame) pPressed = true;

        if (pPressed)
        {
            // Verificamos que no hayas olvidado arrastrar el panel en Unity
            if (panelPause == null)
            {
                Debug.LogError("¡Falta asignar el Panel Pause en el Inspector de Unity!");
                return;
            }

            if (!pause) // Si NO estaba pausado, lo pausamos
            {
                panelPause.SetActive(true); // Muestra el panel gráfico de pausa en pantalla
                pause = true;               // Marcamos en la variable que el juego está pausado

                // --- FUNCIÓN IMPORTANTE: Time.timeScale ---
                Time.timeScale = 0f; 

                // Libera el cursor del mouse ('None') para que puedas moverlo por la pantalla
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else // Si YA estaba pausado, lo despausamos
            {
                Resume(); // Llamamos a la función de reanudar
            }
        }
    }

    // Esta función es 'public' para poder asignarla al botón "Reanudar" del menú desde el Inspector
    public void Resume()
    {
        panelPause.SetActive(false); // Oculta el panel gráfico de pausa
        pause = false;               // Marcamos que ya no estamos en pausa

        // Devolvemos el tiempo a su velocidad normal (1.0 = 100%)
        Time.timeScale = 1f; 

        // Vuelve a bloquear el cursor en el centro ('Locked') y lo oculta ('visible = false')
        // para que puedas volver a jugar estilo First Person Shooter.
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}