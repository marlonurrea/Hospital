using UnityEngine;

public class ObjectInteractTest : MonoBehaviour, IInteractable
{
    [Header("Configuración de Interacción")]
    [Tooltip("Mensaje que aparecerá en el prompt de la UI del jugador.")]
    public string promptText = "Interactuar con objeto de prueba";

    [Tooltip("Mensaje personalizado que se imprimirá en la consola.")]
    public string customMessage = "¡Has interactuado con este objeto correctamente!";

    public string GetInteractPrompt()
    {
        return promptText;
    }

    public void Interact()
    {
        // Se ejecuta a través de PlayerInteraction cuando el jugador presiona la tecla de interacción (E)
        Debug.Log($"[{gameObject.name}] interactuado: {customMessage}");
    }
}
