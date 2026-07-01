using UnityEngine; // Herramientas básicas de Unity para scripts
using UnityEngine.InputSystem; // Herramientas modernas para detectar botones (teclado/mando)

public class PlayerInteraction : MonoBehaviour // Clase principal para que el jugador pueda interactuar con el entorno
{
    public enum DetectionMethod // Enumera las dos formas en que podemos detectar objetos
    {
        SphereCheckFromPlayer, // Detectar objetos en un área circular alrededor del jugador
        RaycastFromCamera      // Lanzar un "láser" invisible desde la cámara (para juegos de disparos/primera persona)
    }

    [Header("Ajustes de Detección")] // Organización en el Inspector
    [Tooltip("Método para detectar objetos interactuables.")]
    public DetectionMethod detectionMethod = DetectionMethod.SphereCheckFromPlayer; // Por defecto usa el círculo alrededor del jugador

    [Tooltip("Distancia máxima de interacción.")]
    public float interactionDistance = 3.0f; // Qué tan lejos llega el "láser" si usamos Raycast

    [Tooltip("Capa (LayerMask) en la que están los objetos interactuables.")]
    public LayerMask interactableMask; // Filtro para solo detectar cosas en la capa "Interactuable" y no paredes

    [Header("Ajustes de Esfera (SphereCheck)")] // Ajustes específicos del área circular
    [Tooltip("Radio de la esfera de detección alrededor del jugador.")]
    public float sphereRadius = 2.0f; // Tamaño del área circular

    [Header("Referencias")] // Objetos necesarios
    [Tooltip("Referencia a la cámara (requerido para RaycastDesdeCamara).")]
    public Transform cameraTransform; // La cámara del jugador

    private IInteractable currentInteractable; // Guarda el objeto interactuable que estamos mirando o tocando actualmente

    void Start() // Se ejecuta al empezar el nivel
    {
        // Si olvidamos conectar la cámara en el Inspector, el juego busca automáticamente la cámara principal ("Main Camera")
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update() // Se ejecuta todo el tiempo, muchísimas veces por segundo (cada fotograma)
    {
        // 1. Escanear el entorno buscando cosas con las que interactuar
        FindInteractable();

        // 2. Verificar si presionamos el botón de interacción
        bool interactPressed = false; // Variable temporal (bandera)

        // Comprueba si se presionó la tecla 'E' en el teclado
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            interactPressed = true; // Levantamos la bandera
        }

        // Comprueba si se presionó el botón 'Cuadrado' (PlayStation) o 'X' (Xbox) en un mando
        if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
        {
            interactPressed = true; // Levantamos la bandera
        }

        // 3. Ejecutar la acción si apretamos el botón y hay un objeto cerca
        if (interactPressed && currentInteractable != null)
        {
            currentInteractable.Interact(); // Llamamos a la función Interact del objeto
        }
    }

    private void FindInteractable() // Función encargada del escaneo
    {
        IInteractable foundInteractable = null; // Variable temporal para guardar lo que encontremos

        if (detectionMethod == DetectionMethod.RaycastFromCamera && cameraTransform != null) // Si usamos el modo "Láser"
        {
            // Creamos un rayo invisible que sale de la cámara hacia el frente
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            RaycastHit hit; // Aquí se guardará la información de lo que golpeamos

            bool hitSomething = false;
            if (interactableMask.value != 0) // Si tenemos un filtro de capa...
            {
                // Disparamos el rayo usando el filtro (ignora objetos normales)
                hitSomething = Physics.Raycast(ray, out hit, interactionDistance, interactableMask);
            }
            else // Si no hay filtro...
            {
                // Disparamos a todo
                hitSomething = Physics.Raycast(ray, out hit, interactionDistance);
            }

            if (hitSomething) // Si el rayo chocó con algo
            {
                // Buscamos si el objeto que golpeamos tiene un script de interactuar
                foundInteractable = hit.collider.GetComponent<IInteractable>();
                if (foundInteractable == null) foundInteractable = hit.collider.GetComponentInParent<IInteractable>(); // Buscamos en el "padre"
                if (foundInteractable == null) foundInteractable = hit.collider.GetComponentInChildren<IInteractable>(); // Buscamos en el "hijo"
            }
        }
        else if (detectionMethod == DetectionMethod.SphereCheckFromPlayer) // Si usamos el modo de "Círculo" (por cercanía)
        {
            Collider[] colliders = null; // Lista de todo lo que esté en nuestro círculo
            
            if (interactableMask.value != 0) // Si usamos filtro
            {
                colliders = Physics.OverlapSphere(transform.position, sphereRadius, interactableMask); // Guardamos lo detectado
            }

            // Si no detectó nada con filtro o no hay filtro, hacemos un escaneo general
            if (colliders == null || colliders.Length == 0)
            {
                colliders = Physics.OverlapSphere(transform.position, sphereRadius);
            }

            float closestDistance = Mathf.Infinity; // Variable para calcular qué está más cerca (inicia en infinito)

            foreach (Collider col in colliders) // Revisamos cada objeto encontrado en el círculo uno por uno
            {
                // Buscamos el script en el objeto
                IInteractable interactable = col.GetComponent<IInteractable>();
                if (interactable == null) interactable = col.GetComponentInParent<IInteractable>();
                if (interactable == null) interactable = col.GetComponentInChildren<IInteractable>();

                if (interactable != null) // Si efectivamente era un objeto interactuable
                {
                    // Calculamos la distancia exacta entre nosotros y ese objeto
                    float distanceToCollider = Vector3.Distance(transform.position, col.transform.position);
                    
                    if (distanceToCollider < closestDistance) // Si está más cerca que el último objeto que encontramos...
                    {
                        closestDistance = distanceToCollider; // Actualizamos el récord de cercanía
                        foundInteractable = interactable; // Lo marcamos como nuestro "objetivo actual"
                    }
                }
            }
        }

        // Si el objeto al que miramos ahora es diferente al que mirábamos hace un momento
        if (foundInteractable != currentInteractable)
        {
            currentInteractable = foundInteractable; // Actualizamos nuestro objetivo

            if (currentInteractable != null) // Si encontramos algo...
            {
                Debug.Log("[Interacción] Detectado: " + currentInteractable.GetInteractPrompt()); // Mensaje de prueba
            }
        }
    }

    // Permite que la interfaz de usuario de la pantalla pregunte "¿A qué estamos mirando?" para dibujar el botón "E"
    public IInteractable GetCurrentInteractable()
    {
        return currentInteractable; // Devuelve el objetivo actual
    }

    // Función exclusiva de Unity para dibujar una esfera amarilla en la pestaña 'Scene' para facilitar la programación
    private void OnDrawGizmosSelected()
    {
        if (detectionMethod == DetectionMethod.SphereCheckFromPlayer)
        {
            Gizmos.color = Color.yellow; // Pintamos la esfera de amarillo
            Gizmos.DrawWireSphere(transform.position, sphereRadius); // Dibujamos un marco alámbrico
        }
    }
}
