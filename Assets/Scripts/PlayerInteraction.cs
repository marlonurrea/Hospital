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
    }

    void Update()
    {
        // 1. Detectar interactuables en cada fotograma
        FindInteractable();

        // 2. Comprobar si el jugador presiona la tecla de interacción
        bool interactPressed = false;

        // Leer teclado (Tecla E por defecto)
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            interactPressed = true;
        }

        // Leer mando (Botón Oeste: X en Xbox, Cuadrado en PlayStation por defecto)
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            interactPressed = true;
        }

        // 3. Ejecutar la interacción
        if (interactPressed && currentInteractable != null)
        {
            currentInteractable.Interact();
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

            // Intentar detectar con máscara si está definida, de lo contrario sin máscara
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
                // Intentar obtener el componente IInteractable de forma jerárquica (objeto, padres o hijos)
                foundInteractable = hit.collider.GetComponent<IInteractable>();
                if (foundInteractable == null)
                {
                    foundInteractable = hit.collider.GetComponentInParent<IInteractable>();
                }
                if (foundInteractable == null)
                {
                    foundInteractable = hit.collider.GetComponentInChildren<IInteractable>();
                }
            }
        }
        else if (detectionMethod == DetectionMethod.SphereCheckFromPlayer)
        {
            // Buscar colisionadores en una esfera alrededor del jugador usando la máscara
            Collider[] colliders = null;
            if (interactableMask.value != 0)
            {
                colliders = Physics.OverlapSphere(transform.position, sphereRadius, interactableMask);
            }

            // Salvaguarda: Si la máscara es 0 o no se encontró ningún colisionador, hacer una búsqueda general
            if (colliders == null || colliders.Length == 0)
            {
                colliders = Physics.OverlapSphere(transform.position, sphereRadius);
            }

            float closestDistance = Mathf.Infinity;

            foreach (Collider col in colliders)
            {
                // Intentar obtener el componente IInteractable de forma jerárquica (objeto, padres o hijos)
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable == null)
                {
                    interactable = col.GetComponentInParent<IInteractable>();
                }
                if (interactable == null)
                {
                    interactable = col.GetComponentInChildren<IInteractable>();
                }

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
