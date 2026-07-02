using UnityEngine;
using UnityEngine.InputSystem;

// -----------------------------------------------------------------------------
// SCRIPT: RE_PlayerInteraction
// METÁFORA: "El Radar y la Mano del Jugador"
// Versión ultra simplificada: Solo detecta NPCs usando una burbuja alrededor del jugador.
// -----------------------------------------------------------------------------
public class RE_PlayerInteraction : MonoBehaviour 
{
    [Header("Ajustes de Detección")]
    [Tooltip("El tamaño de la burbuja invisible alrededor del jugador para detectar NPCs.")]
    public float sphereRadius = 2.0f; // Qué tan cerca debes estar del NPC para poder hablarle.

    [Tooltip("El filtro para ignorar paredes y solo detectar objetos interactuables.")]
    public LayerMask interactableMask; // Las "Gafas" que solo ven cosas con las que se puede hablar.

    private IInteractable currentInteractable; // Nuestra memoria: "¿A quién estamos mirando ahora mismo?"

    void Update() // Se ejecuta todo el tiempo
    {
        // ---------------------------------------------------------
        // PASO 1: ENCENDER EL RADAR (Buscar NPCs cerca)
        // ---------------------------------------------------------
        FindInteractable();

        // ---------------------------------------------------------
        // PASO 2: APRETAR LA TECLA PARA HABLAR
        // ---------------------------------------------------------
        // Si apretamos la tecla 'E' Y ADEMÁS tenemos a alguien en frente...
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (currentInteractable != null)
            {
                // Le damos la orden a ese NPC de que hable (Llama al script RE_NPCInteraction).
                currentInteractable.Interact(); 
            }
        }
    }

    private void FindInteractable()
    {
        IInteractable foundInteractable = null; // Empezamos asumiendo que no hay nadie cerca.

        // 1. Dibujamos una burbuja mágica invisible alrededor del jugador.
        // Todo lo que toque la burbuja y coincida con la capa "interactableMask" entra en la lista.
        Collider[] colliders = Physics.OverlapSphere(transform.position, sphereRadius, interactableMask); 
        
        // Si el filtro falló, probamos atrapar TODO lo que esté en la burbuja.
        if (colliders == null || colliders.Length == 0) 
        {
            colliders = Physics.OverlapSphere(transform.position, sphereRadius);
        }

        // 2. Buscar al NPC MÁS CERCANO dentro de la burbuja.
        float closestDistance = Mathf.Infinity; // Empezamos a medir desde el infinito.

        foreach (Collider col in colliders) // Revisamos los objetos tocados uno por uno
        {
            // Le buscamos en los bolsillos el "Carnet de Interactuable" (La interfaz IInteractable).
            IInteractable interactable = col.GetComponent<IInteractable>(); 
            if (interactable == null) interactable = col.GetComponentInParent<IInteractable>();
            if (interactable == null) interactable = col.GetComponentInChildren<IInteractable>();

            if (interactable != null) // Si sí tiene el carnet (¡Es un NPC válido!)...
            {
                // Medimos con una cinta métrica a cuántos metros exactos está de nosotros.
                float distanceToCollider = Vector3.Distance(transform.position, col.transform.position);
                
                if (distanceToCollider < closestDistance) // Si está más cerca que el último que revisamos...
                {
                    closestDistance = distanceToCollider; // Actualizamos nuestro récord de cercanía.
                    foundInteractable = interactable; // Lo coronamos como el "NPC Actual" con el que vamos a hablar.
                }
            }
        }

        // 3. Actualizamos nuestra memoria
        // Si cambiamos de objetivo (ej. nos alejamos del Guardia y nos acercamos al Civil).
        if (foundInteractable != currentInteractable)
        {
            currentInteractable = foundInteractable; 
        }
    }

    // Un puente para que el creador del juego pueda dibujar el letrero de "Presiona E" en la pantalla.
    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable; 
    }

    // Dibuja la burbuja amarilla en la ventana de Scene de Unity para que tú la puedas ver al programar.
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; 
        Gizmos.DrawWireSphere(transform.position, sphereRadius); 
    }
}
