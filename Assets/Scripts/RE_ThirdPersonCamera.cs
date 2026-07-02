using UnityEngine;
using UnityEngine.InputSystem; // Necesario para detectar ratón y joystick moderno

public class RE_ThirdPersonCamera : MonoBehaviour // Script principal para la cámara en tercera persona que sigue al jugador
{
    [Header("Objetivo")]
    [Tooltip("El objeto que la cámara debe seguir (el jugador).")]
    public Transform target; // El jugador al que seguiremos
    
    [Tooltip("Desplazamiento para no apuntar a los pies. Usualmente apunta a los hombros/cabeza.")]
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0); // Lo usamos para que la cámara apunte a la cabeza y no a los pies

    [Header("Configuración de Cámara")]
    [Tooltip("Distancia base de la cámara al jugador.")]
    public float distance = 5.0f; // Qué tan lejos flota la cámara por detrás
    
    [Tooltip("Distancia mínima al chocar con una pared.")]
    public float minDistance = 1.0f; // Lo más cerca que puede llegar a estar (para que no se meta dentro de su propia cabeza)
    
    [Header("Sensibilidad")]
    public float mouseSensitivity = 1.5f; // Velocidad de giro con el ratón
    public float controllerSensitivity = 150.0f; // Velocidad de giro con un joystick

    [Header("Límites de Inclinación (Eje Y)")]
    public float yMinLimit = -20f; // Qué tanto puede mirar hacia abajo
    public float yMaxLimit = 60f; // Qué tanto puede mirar hacia arriba (hacia el cielo)

    [Header("Colisiones (Evitar atravesar paredes)")]
    [Tooltip("Capas que bloquearán la visión de la cámara (ej. Default, Paredes).")]
    public LayerMask collisionMask; // Qué objetos pueden bloquear a la cámara (paredes, techos)
    
    public float collisionCushion = 0.2f; // Grosor de la cámara para que no atraviese esquinas afiladas

    private float currentX = 0.0f; // Rotación horizontal actual
    private float currentY = 0.0f; // Rotación vertical actual
    private float currentDistance; // Distancia real (que se encoge al chocar)

    void Start() // Se ejecuta al inicio
    {
        // Ocultar y bloquear el ratón al centro de la pantalla
        Cursor.lockState = CursorLockMode.Locked; // Evita que se salga del juego
        Cursor.visible = false; // Lo vuelve invisible

        currentDistance = distance; // Inicializamos a la distancia predeterminada

        // Extraemos hacia donde está mirando el personaje actualmente al empezar
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;
    }

    void LateUpdate() // Se ejecuta CADA FOTOGRAMA DESPUÉS de que Update haya terminado (ideal para cámaras)
    {
        if (target == null) return; // Si no hay jugador asignado, no hacemos nada

        // Si el ratón no está bloqueado (por ejemplo, si estamos hablando con un NPC), la cámara se congela
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float mouseX = 0f; // Input temporal horizontal
        float mouseY = 0f; // Input temporal vertical

        // 1. Escaneo del ratón
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue(); // Leemos el cambio de posición del ratón físico
            mouseX += delta.x * mouseSensitivity * 0.1f;
            mouseY -= delta.y * mouseSensitivity * 0.1f;
        }

        // 2. Escaneo del mando (Gamepad - Palanca derecha)
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.rightStick.ReadValue();
            mouseX += stick.x * controllerSensitivity * Time.deltaTime; // Se multiplica por deltaTime para mantener consistencia
            mouseY -= stick.y * controllerSensitivity * Time.deltaTime;
        }

        currentX += mouseX; // Sumamos la rotación horizontal
        currentY += mouseY; // Sumamos la rotación vertical

        // Restringimos la mirada vertical para que la cámara no dé la voltereta por debajo de los pies o por encima de la cabeza
        currentY = Mathf.Clamp(currentY, yMinLimit, yMaxLimit);

        // Convertimos esos números a una Rotación de Unity (Quaternion)
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Determinamos el punto central exacto (Los hombros o cabeza del jugador)
        Vector3 targetPosition = target.position + targetOffset;

        // Calculamos la dirección en línea recta hacia atrás de dónde debe estar la cámara
        Vector3 direction = rotation * new Vector3(0, 0, -distance);
        
        // --- Sistema Anti-Atravesar Paredes (Collision Handling) ---
        float desiredDistance = distance; // Guardamos dónde "nos gustaría" estar
        RaycastHit hit; // Variable para almacenar información del choque
        
        // Disparamos una esfera (SphereCast) invisible del tamaño de "collisionCushion" desde la cabeza del jugador hacia la cámara
        // Si choca contra algo etiquetado en collisionMask (ej: una pared)...
        if (Physics.SphereCast(targetPosition, collisionCushion, direction.normalized, out hit, distance, collisionMask))
        {
            // Acortamos la distancia hasta el punto de choque (para que la cámara quede delante de la pared)
            desiredDistance = Mathf.Clamp(hit.distance, minDistance, distance);
        }

        // Suavizamos (Lerp) el cambio de distancia para que el acercamiento al chocar no dé mareos
        currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * 10f);

        // Calculamos las coordenadas 3D matemáticas finales en el mundo
        Vector3 position = targetPosition + rotation * new Vector3(0, 0, -currentDistance);

        // Aplicamos la posición y la rotación al objeto de la cámara
        transform.position = position;
        transform.rotation = rotation;
    }
}
