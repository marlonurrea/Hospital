using UnityEngine;
using UnityEngine.SceneManagement; // Importado para posible uso futuro (ej: volver al menú)

// Controla el sistema de pausa del juego con la tecla P
public class MenuPausa : MonoBehaviour
{
    public GameObject panelPause; // Panel de la UI (la pantalla oscura con botones) que se muestra al pausar
    public bool pause = false;    // Estado actual: true = pausado, false = jugando

    // Update se ejecuta una vez por frame — aquí detectamos las teclas en tiempo real
    void Update()
    {
        // --- FUNCIÓN IMPORTANTE: Input.GetKeyDown ---
        // A diferencia de GetKey (que se dispara mientas mantienes la tecla hundida),
        // GetKeyDown solo se dispara en el fotograma EXACTO en que presionas la tecla hacia abajo.
        // Esto evita que el juego se pause y despause 60 veces por segundo si dejas el dedo en la tecla.
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Solo pausamos si el juego NO estaba pausado ya
            if (!pause)
            {
                panelPause.SetActive(true); // Muestra el panel gráfico de pausa en pantalla
                pause = true;               // Marcamos en la variable que el juego está pausado

                // --- FUNCIÓN IMPORTANTE: Time.timeScale ---
                // 'timeScale' es la velocidad a la que pasa el tiempo en el juego.
                // 1f es la velocidad normal. 0.5f sería cámara lenta (Matrix).
                // Al ponerlo en 0f, el tiempo se congela por completo. Físicas, animaciones y temporizadores 
                // basados en Time.deltaTime (como nuestro reloj de juego) se detendrán.
                Time.timeScale = 0f; 

                // Libera el cursor del mouse ('None') para que puedas moverlo por la pantalla
                // y hace que la flechita blanca sea visible ('visible = true') para poder hacer clic en los botones
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
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