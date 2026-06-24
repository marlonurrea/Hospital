using UnityEngine;
using UnityEngine.InputSystem; // Requerido para el nuevo Input System

public class ObjectInteractTest : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    [Tooltip("Distancia máxima a la que debe estar el jugador para interactuar.")]
    public float interactionDistance = 3.0f;

    [Tooltip("Mensaje personalizado que se imprimirá en la consola.")]
    public string customMessage = "¡Has interactuado con este objeto correctamente!";

    private Transform playerTransform;

    void Start()
    {
        // Buscar automáticamente al jugador en la escena usando el Tag "Player"
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("ObjectInteractTest: No se encontró ningún GameObject con la etiqueta (Tag) 'Player'. Asigna la etiqueta 'Player' a tu personaje.");
        }
    }

    void Update()
    {
        if (playerTransform == null)
        {
            // Intentar buscar de nuevo si no se encontró en Start (por si el jugador se genera después)
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            return;
        }

        // Calcular la distancia entre el objeto y el jugador
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Si el jugador está lo suficientemente cerca
        if (distance <= interactionDistance)
        {
            // Si el jugador presiona la tecla E en el teclado
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Imprimir el mensaje en consola incluyendo el nombre del objeto
                Debug.Log($"[{gameObject.name}] interactuado: {customMessage}");
            }
        }
    }

    // Dibujar el radio de interacción en el Editor de Unity para verlo visualmente
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
