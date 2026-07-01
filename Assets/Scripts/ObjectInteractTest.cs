using UnityEngine; // Funciones básicas de Unity
using UnityEngine.InputSystem; // Sistema moderno para detectar controles y teclado

public class ObjectInteractTest : MonoBehaviour // Clase de prueba para verificar que la interacción funciona
{
    [Header("Configuración de Interacción")]
    [Tooltip("Distancia máxima a la que debe estar el jugador para interactuar.")]
    public float interactionDistance = 3.0f; // Qué tan cerca debe estar el jugador

    [Tooltip("Mensaje personalizado que se imprimirá en la consola.")]
    public string customMessage = "¡Has interactuado con este objeto correctamente!"; // Mensaje de éxito

    private Transform playerTransform; // Variable para recordar dónde está el jugador

    void Start() // Se ejecuta al principio
    {
        // Busca automáticamente al jugador por su etiqueta ("Player")
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform; // Si lo encuentra, guarda su ubicación
        else Debug.LogWarning("[ObjectInteractTest] No se encontró a nadie con el Tag 'Player'."); // Aviso de error
    }

    void Update() // Se ejecuta cada fotograma
    {
        if (playerTransform == null) // Si no encontró al jugador al principio...
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player"); // Sigue buscándolo
            if (player != null) playerTransform = player.transform;
            return; // Espera hasta encontrarlo
        }

        // Calcula la distancia matemática exacta entre este objeto y el jugador
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= interactionDistance) // Si estamos lo suficientemente cerca...
        {
            // Y si presionamos la tecla E...
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                // Mostramos el mensaje de éxito en la consola de programador
                Debug.Log($"[{gameObject.name}] interactuado: {customMessage}");
            }
        }
    }

    // Dibuja un círculo verde en el editor de Unity para visualizar el rango de alcance
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
