using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("El objeto que la cámara debe seguir (el jugador).")]
    public Transform target;
    [Tooltip("Desplazamiento para no apuntar a los pies. Usualmente apunta a los hombros/cabeza.")]
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Configuración de Cámara")]
    [Tooltip("Distancia base de la cámara al jugador.")]
    public float distance = 5.0f;
    [Tooltip("Distancia mínima al chocar con una pared.")]
    public float minDistance = 1.0f;
    
    [Header("Sensibilidad")]
    public float mouseSensitivity = 1.5f;
    public float controllerSensitivity = 150.0f;

    [Header("Límites de Inclinación (Eje Y)")]
    public float yMinLimit = -20f;
    public float yMaxLimit = 60f;

    [Header("Colisiones (Evitar atravesar paredes)")]
    [Tooltip("Capas que bloquearán la visión de la cámara (ej. Default, Paredes).")]
    public LayerMask collisionMask;
    public float collisionCushion = 0.2f;

    private float currentX = 0.0f;
    private float currentY = 0.0f;
    private float currentDistance;

    void Start()
    {
        // Ocultar y bloquear el cursor para el control de la cámara
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentDistance = distance;

        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Si el cursor no está bloqueado (por ejemplo, estamos en el menú de diálogo del NPC), no mover la cámara
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = 0f;
        float mouseY = 0f;

        // 1. Leer movimiento del ratón
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            mouseX += delta.x * mouseSensitivity * 0.1f;
            mouseY -= delta.y * mouseSensitivity * 0.1f;
        }

        // 2. Leer movimiento del mando (Stick Derecho)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            mouseX += stick.x * controllerSensitivity * Time.deltaTime;
            mouseY -= stick.y * controllerSensitivity * Time.deltaTime;
        }

        currentX += mouseX;
        currentY += mouseY;

        // Limitar la rotación vertical para que no dé la vuelta completa
        currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);

        // Calcular la rotación deseada
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Posición del objetivo con su offset (hombros/cabeza)
        Vector3 targetPosition = target.position + targetOffset;

        // Dirección desde el objetivo hacia la cámara
        Vector3 direction = rotation * new Vector3(0, 0, -distance);
        
        // --- Manejo de Colisiones de la Cámara ---
        float desiredDistance = distance;
        RaycastHit hit;
        
        // Lanzamos una esfera desde el target hacia la cámara para detectar paredes
        if (Physics.SphereCast(targetPosition, collisionCushion, direction.normalized, out hit, distance, collisionMask))
        {
            desiredDistance = Mathf.Clamp(hit.distance, minDistance, distance);
        }

        // Suavizamos el cambio de distancia para que no salte de golpe al rozar paredes
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * 10f);

        // Calcular la posición final
        Vector3 position = targetPosition + rotation * new Vector3(0, 0, -currentDistance);

        // Aplicar la transformación a la cámara
        transform.position = position;
        transform.rotation = rotation;
    }
}
