using UnityEngine; // Herramientas físicas de Unity
using UnityEngine.InputSystem; // Sistema de ratón moderno

// -----------------------------------------------------------------------------
// SCRIPT: RE_ThirdPersonCamera
// METÁFORA: "El Camarógrafo Flotante"
// Este script es como un camarógrafo invisible que flota detrás del jugador, 
// girando sobre un eje y persiguiéndolo por el hospital.
// -----------------------------------------------------------------------------
public class RE_ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("El objeto que la cámara va a seguir (generalmente el jugador).")]
    public Transform target; // El actor al que persigue nuestro camarógrafo.

    [Header("Posición de Cámara")]
    [Tooltip("La distancia a la que la cámara estará del jugador.")]
    public float distance = 5.0f; // Qué tan lejos está flotando (5 metros de distancia)
    
    [Tooltip("Altura base hacia donde mira la cámara.")]
    public float height = 1.5f; // A qué altura está el lente (1.5m es altura de los hombros/cabeza)
    
    [Tooltip("El desplazamiento lateral de la cámara.")]
    public Vector3 offset = new Vector3(0, 0, 0); // Ajustes precisos si la queremos ligeramente hacia la derecha (estilo Resident Evil 4)

    [Header("Sensibilidad")]
    [Tooltip("Velocidad de rotación usando el ratón.")]
    public float mouseSensitivity = 2.0f; // Qué tan rápido voltea la cámara cuando mueves el ratón

    [Header("Límites de Rotación (Vertical)")]
    // Evita que el camarógrafo se voltee de cabeza rompiéndose el cuello.
    public float yMinLimit = -20f; // Límite para mirar hacia abajo
    public float yMaxLimit = 80f; // Límite para mirar hacia arriba al techo

    [Header("Colisiones (Raycast)")]
    [Tooltip("Capas que bloquearán la cámara, acercándola al jugador.")]
    public LayerMask collisionLayers; // Paredes que el camarógrafo no puede traspasar.
    
    [Tooltip("Radio de la esfera de la cámara para evitar que atraviese paredes muy finas.")]
    public float cameraRadius = 0.3f; // El tamaño "gordo" del lente de la cámara para que no perfore paredes de papel.

    private float currentX = 0.0f; // Memoria de los grados rotados a la derecha/izquierda
    private float currentY = 0.0f; // Memoria de los grados rotados arriba/abajo
    private float currentDistance; // La distancia real a la que está ahora mismo (se encoge si choca contra pared)

    void Start()
    {
        // 1. Atrapamos el cursor del ratón en el centro de la pantalla y lo volvemos invisible.
        // Como en Counter Strike o juegos de disparo, para que no te salgas de la ventana jugando.
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false; 

        currentDistance = distance; // Empezamos a los 5 metros requeridos.

        // Leemos hacia dónde estaba mirando la cámara cuando le dimos a Play, para no dar giros bruscos.
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y; // Grados en el eje Y (giro horizontal como un trompo)
        currentY = angles.x; // Grados en el eje X (cabeceo arriba/abajo)
    }

    // LateUpdate ocurre siempre que termina Update.
    // METÁFORA: Si el jugador camina en Update, la cámara se mueve en LateUpdate.
    // Así garantizamos que el camarógrafo siga al actor DESPUÉS de que dio el paso, y la imagen no vibre.
    void LateUpdate() 
    {
        if (target == null) return; // Si no hay jugador a quien seguir, apagamos la cámara.

        // Si el ratón NO está bloqueado (porque estamos en un menú o hablando con un NPC)...
        if (Cursor.lockState != CursorLockMode.Locked) return; // Congelamos al camarógrafo, no le permitimos girar.

        float mouseX = 0f; // Cuánto movimos el ratón horizontalmente
        float mouseY = 0f; // Cuánto movimos el ratón verticalmente

        // ESCANEO DEL RATÓN (Controles de PC exclusivamente)
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue(); // Leemos la velocidad pura del ratón sobre el escritorio.
            mouseX += delta.x * mouseSensitivity * 0.1f; // Lo multiplicamos por la sensibilidad
            mouseY -= delta.y * mouseSensitivity * 0.1f; 
        }

        currentX += mouseX; // Acumulamos el giro horizontal total.
        currentY += mouseY; // Acumulamos el giro vertical total.

        // Abrazadera matemática (Clamp): Evita que miremos más arriba del techo o más abajo de los pies.
        currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);

        // Traducimos los grados de giro (Euler) a matemáticas de rotación 3D pura (Quaterniones).
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // ¿Desde dónde mira el camarógrafo? Desde los pies del jugador más la altura de su cabeza (target.position + Vector3.up * height)
        Vector3 targetPosition = target.position + Vector3.up * height + rotation * offset;

        // FÍSICA DE COLISIONES: ¡Que la cámara no traspase paredes!
        // Tiramos un láser (Raycast) desde la cabeza del jugador hacia la cámara.
        Vector3 directionToCamera = (transform.position - targetPosition).normalized;
        RaycastHit hit;

        // Si el láser, siendo ancho (SphereCast), choca con una pared (collisionLayers)...
        if (Physics.SphereCast(targetPosition, cameraRadius, directionToCamera, out hit, distance, collisionLayers))
        {
            // Achicamos la distancia actual hasta el punto exacto donde chocó, para que la cámara se acerque al jugador y no atraviese la pared.
            currentDistance = Mathf.Lerp(currentDistance, hit.distance, Time.deltaTime * 10f);
        }
        else
        {
            // Si el láser pasa limpio, estiramos la cámara a su distancia máxima suavemente.
            currentDistance = Mathf.Lerp(currentDistance, distance, Time.deltaTime * 10f);
        }

        // Aplicamos finalmente toda la matemática: Colocamos la cámara flotando detrás de la cabeza en el ángulo calculado.
        Vector3 finalPosition = targetPosition - (rotation * Vector3.forward * currentDistance);
        
        // Ejecutamos visualmente el movimiento y rotación.
        transform.position = finalPosition;
        transform.rotation = rotation;
    }
}
