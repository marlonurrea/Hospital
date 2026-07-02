using UnityEngine;

public class RE_SimpleInteractable : MonoBehaviour, IInteractable // Objeto de prueba súper básico con el que se puede interactuar
{
    [Header("Configuración de Interacción")]
    [Tooltip("El mensaje que aparecerá en pantalla cuando el jugador se acerque.")]
    public string promptText = "Presiona E para interactuar"; // Texto flotante que pide apretar la tecla E

    [Tooltip("El mensaje que se mostrará en consola cuando el jugador presione el botón de interactuar.")]
    public string interactionMessage = "¡Interacción realizada con éxito!"; // Texto de éxito para verificar que funcionó

    [Tooltip("¿Desactivar el objeto después de interactuar? (Ej: Cofre abierto o consumible)")]
    public bool disableAfterInteract = false; // ¿Debe desaparecer al tocarlo? (como recoger una moneda)

    // Función requerida por IInteractable que devuelve el texto flotante
    public string GetInteractPrompt()
    {
        return promptText; // Le dice a la interfaz qué texto mostrar
    }

    // Función requerida por IInteractable que se activa cuando el jugador pulsa la tecla E
    public void Interact()
    {
        // Esta es la lógica que se ejecutará al pulsar el botón
        Debug.Log(interactionMessage); // Imprime el mensaje de éxito en la consola invisible para los programadores

        // Si marcamos la casilla de que debe desaparecer
        if (disableAfterInteract)
        {
            gameObject.SetActive(false); // Apaga visual y físicamente el objeto
        }
    }
}
