using UnityEngine;
using UnityEngine.InputSystem; // Requerido para el nuevo Input System

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [Tooltip("El Transform del personaje que la cámara debe seguir.")]
    public Transform target;

    [Header("Ajustes de Distancia y Altura")]
    [Tooltip("Distancia inicial detrás del objetivo.")]
    public float distance = 5.0f;
    [Tooltip("Desplazamiento de altura respecto al pivote del objetivo.")]
    public float heightOffset = 1.5f;

    [Header("Límites de Rotación")]
    [Tooltip("Límite inferior para mirar hacia abajo (grados).")]
    public float minVerticalAngle = -30f;
    [Tooltip("Límite superior para mirar hacia arriba (grados).")]
    public float maxVerticalAngle = 60f;

    [Header("Sensibilidad")]
    [Tooltip("Sensibilidad del ratón.")]
    public float mouseSensitivity = 0.15f;
    [Tooltip("Sensibilidad del joystick derecho del mando.")]
    public float controllerSensitivity = 2.0f;

    [Header("Suavizado y Colisión")]
    [Tooltip("Tiempo de suavizado para el movimiento de la cámara.")]
    public float smoothTime = 0.12f;
    [Tooltip("Habilitar detección de colisiones para evitar que la cámara atraviese paredes.")]
    public bool enableCollision = true;
    [Tooltip("Capa (LayerMask) de los objetos con los que la cámara puede colisionar.")]
    public LayerMask collisionMask;

    private float rotationX = 0.0f; // Rotación vertical
    private float rotationY = 0.0f; // Rotación horizontal

    private Vector3 currentVelocity;
    private Vector3 destinationPosition;

    void Start()
    {
        // Bloquear y ocultar el cursor del ratón en el juego
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Inicializar las rotaciones con la rotación inicial de la cámara
        Vector3 angles = transform.eulerAngles;
        rotationY = angles.y;
        rotationX = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // --- LEER ENTRADA DE ROTACIÓN ---
        float lookX = 0f;
        float lookY = 0f;

        // Ratón
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            lookX = mouseDelta.x * mouseSensitivity;
            lookY = mouseDelta.y * mouseSensitivity;
        }

        // Mando (stick derecho)
        if (Gamepad.current != null)
        {
            Vector2 stickInput = Gamepad.current.rightStick.ReadValue();
            lookX += stickInput.x * controllerSensitivity;
            lookY += stickInput.y * controllerSensitivity;
        }

        // Acumular rotaciones
        rotationY += lookX;
        rotationX -= lookY;

        // Limitar la rotación vertical para no girar de cabeza
        rotationX = Mathf.Clamp(rotationX, minVerticalAngle, maxVerticalAngle);

        // --- CALCULAR POSICIÓN Y ROTACIÓN ---
        Quaternion rotation = Quaternion.Euler(rotationX, rotationY, 0);
        Vector3 targetPivotPosition = target.position + Vector3.up * heightOffset;
        
        float currentDistance = distance;

        // Detección de colisiones opcional (para evitar atravesar paredes)
        if (enableCollision)
        {
            Vector3 idealPosition = targetPivotPosition - (rotation * Vector3.forward * distance);
            Vector3 directionToCamera = idealPosition - targetPivotPosition;
            RaycastHit hit;

            // Lanzar un rayo desde el jugador hacia la cámara
            if (Physics.Raycast(targetPivotPosition, directionToCamera.normalized, out hit, distance, collisionMask))
            {
                // Si choca con algo que no sea el jugador, acortar la distancia
                if (hit.transform != target)
                {
                    currentDistance = Mathf.Clamp(hit.distance - 0.2f, 0.5f, distance);
                }
            }
        }

        // Calcular posición final
        destinationPosition = targetPivotPosition - (rotation * Vector3.forward * currentDistance);

        // Aplicar la rotación a la cámara
        transform.rotation = rotation;

        // Desplazar suavemente a la cámara a la posición de destino
        transform.position = Vector3.SmoothDamp(transform.position, destinationPosition, ref currentVelocity, smoothTime);
    }
}
