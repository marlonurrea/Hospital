using UnityEngine;

public class SimpleInteractable : MonoBehaviour, IInteractable
{
    [Header("Configuración de Interacción")]
    [Tooltip("El mensaje que aparecerá en pantalla cuando el jugador se acerque.")]
    public string promptText = "Presiona E para interactuar";

    [Tooltip("El mensaje que se mostrará en consola cuando el jugador presione el botón de interactuar.")]
    public string interactionMessage = "¡Interacción realizada con éxito!";

    [Tooltip("¿Desactivar el objeto después de interactuar? (Ej: Cofre abierto o consumible)")]
    public bool disableAfterInteract = false;

    public string GetInteractPrompt()
    {
        return promptText;
    }

    public void Interact()
    {
        // Esta es la lógica que se ejecutará cuando el jugador interactúe con este objeto
        Debug.Log(interactionMessage);

        if (disableAfterInteract)
        {
            gameObject.SetActive(false);
        }
    }
}
