using UnityEngine;
using UnityEngine.InputSystem; // Requerido para el nuevo Input System

public class PlayerInteraction : MonoBehaviour
{
    public enum DetectionMethod
    {
        SphereCheckFromPlayer, // Detectar por proximidad (esfera alrededor del jugador)
        RaycastFromCamera      // Lanzar un rayo desde el centro de la cámara hacia adelante
    }

    [Header("Ajustes de Detección")]
    [Tooltip("Método para detectar objetos interactuables.")]
    public DetectionMethod detectionMethod = DetectionMethod.SphereCheckFromPlayer;

    [Tooltip("Distancia máxima de interacción.")]
    public float interactionDistance = 3.0f;

    [Tooltip("Capa (LayerMask) en la que están los objetos interactuables.")]
    public LayerMask interactableMask;

    [Header("Ajustes de Esfera (SphereCheck)")]
    [Tooltip("Radio de la esfera de detección alrededor del jugador.")]
    public float sphereRadius = 2.0f;

    [Header("Referencias")]
    [Tooltip("Referencia a la cámara (requerido para RaycastDesdeCamara).")]
    public Transform cameraTransform;

    private IInteractable currentInteractable;

    void Start()
    {
        // Si no se asigna cámara, buscar la cámara principal
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Advertencia si la máscara de capas no está configurada
        if (interactableMask.value == 0)
        {
            Debug.LogWarning("<b>[PlayerInteraction]</b> La máscara de capas (Interactable Mask) está configurada como 'Nothing'. Asegúrate de seleccionar la capa de tus objetos interactuables en el Inspector.");
        }
    }

    void Update()
    {
        // 1. Detectar interactuables en cada fotograma
        FindInteractable();

        // 2. Comprobar si el jugador presiona la tecla de interacción
        bool interactPressed = false;

        // Leer teclado (Tecla E) con soporte para nuevo Input System y fallback al sistema clásico (Old Input System)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            interactPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            interactPressed = true;
        }

        // Leer mando (Botón Oeste: X en Xbox, Cuadrado en PlayStation por defecto)
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            interactPressed = true;
        }

        // 3. Ejecutar la interacción
        if (interactPressed)
        {
            if (currentInteractable != null)
            {
                Debug.Log($"<b>[PlayerInteraction]</b> Interactuando con: {currentInteractable.GetInteractPrompt()}");
                currentInteractable.Interact();
            }
            else
            {
                Debug.Log("<b>[PlayerInteraction]</b> Se presionó interactuar, pero no hay ningún objeto interactuable cerca.");

                // Diagnóstico detallado para ayudar al usuario a ver qué está pasando en la consola de Unity
                MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                string diagnosticMsg = "<b>[PlayerInteraction - Diagnóstico de Interacción]</b>\n";
                int count = 0;
                foreach (MonoBehaviour mono in allScripts)
                {
                    if (mono is IInteractable)
                    {
                        count++;
                        float dist = Vector3.Distance(transform.position, mono.transform.position);
                        Collider col = mono.GetComponentInChildren<Collider>();
                        if (col == null) col = mono.GetComponentInParent<Collider>();
                        
                        diagnosticMsg += $"- <b>{mono.gameObject.name}</b>:\n" +
                                         $"  • Distancia al jugador: {dist:F2}m (Rango requerido: Esfera = {sphereRadius}m, Raycast = {interactionDistance}m)\n" +
                                         $"  • ¿Tiene Collider?: {(col != null ? "SÍ" : "NO")}\n" +
                                         $"  • Capa (Layer): {LayerMask.LayerToName(mono.gameObject.layer)}\n";
                    }
                }
                if (count == 0)
                {
                    diagnosticMsg += "¡ATENCIÓN! No se encontró ningún script en la escena que implemente IInteractable (como NPCInteraction o ObjectInteractTest).";
                }
                Debug.LogWarning(diagnosticMsg);
            }
        }
    }

    private void FindInteractable()
    {
        IInteractable foundInteractable = null;

        if (detectionMethod == DetectionMethod.RaycastFromCamera && cameraTransform != null)
        {
            // Lanzar rayo desde el centro de la cámara hacia adelante
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit;

            bool hitSomething = false;
            if (interactableMask.value != 0)
            {
                hitSomething = Physics.Raycast(ray, out hit, interactionDistance, interactableMask);
            }
            else
            {
                hitSomething = Physics.Raycast(ray, out hit, interactionDistance);
            }

            if (hitSomething)
            {
                // Intentar obtener el componente que implementa IInteractable (se busca también en los padres)
                foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
            }
        }
        else if (detectionMethod == DetectionMethod.SphereCheckFromPlayer)
        {
            // Buscar colisionadores en una esfera alrededor del jugador
            Collider[] colliders;
            if (interactableMask.value != 0)
            {
                colliders = Physics.OverlapSphere(transform.position, sphereRadius, interactableMask);
            }
            else
            {
                colliders = Physics.OverlapSphere(transform.position, sphereRadius);
            }
            
            float closestDistance = Mathf.Infinity;

            foreach (Collider col in colliders)
            {
                // Evitar colisionar consigo mismo si el script está en el jugador
                if (col.gameObject == gameObject || col.transform.IsChildOf(transform))
                {
                    continue;
                }

                IInteractable interactable = col.GetComponentInParent<IInteractable>();
                if (interactable != null)
                {
                    // Elegir el objeto interactuable más cercano
                    float distanceToCollider = Vector3.Distance(transform.position, col.transform.position);
                    if (distanceToCollider < closestDistance)
                    {
                        closestDistance = distanceToCollider;
                        foundInteractable = interactable;
                    }
                }
            }
        }

        // Actualizar el interactuable actual si ha cambiado
        if (foundInteractable != currentInteractable)
        {
            currentInteractable = foundInteractable;

            if (currentInteractable != null)
            {
                // Muestra un mensaje en consola con el prompt
                Debug.Log("Interactuable detectado: " + currentInteractable.GetInteractPrompt());
            }
            else
            {
                Debug.Log("No hay interactuables cerca.");
            }
        }
    }

    // Método para que otros scripts (como la interfaz de usuario) puedan leer el interactuable actual
    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable;
    }

    // Dibujar la esfera de detección en el editor de Unity
    private void OnDrawGizmosSelected()
    {
        if (detectionMethod == DetectionMethod.SphereCheckFromPlayer)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, sphereRadius);
        }
    }
}
