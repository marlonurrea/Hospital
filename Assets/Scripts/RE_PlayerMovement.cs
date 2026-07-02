using UnityEngine; // Caja de herramientas físicas de Unity
using UnityEngine.InputSystem; // Sistema para detectar el Teclado

// -----------------------------------------------------------------------------
// SCRIPT: RE_PlayerMovement
// METÁFORA: "Las Piernas del Jugador"
// Versión ultra simplificada: Solo caminar en la dirección de la cámara.
// -----------------------------------------------------------------------------
[RequireComponent(typeof(CharacterController))] // Obliga a Unity a ponerle un cilindro físico invisible al jugador para que no atraviese paredes.
public class RE_PlayerMovement : MonoBehaviour 
{
    [Header("Movimiento Básico")]
    [Tooltip("Velocidad de movimiento al caminar.")]
    public float walkSpeed = 6.0f; // Qué tan rápido camina.
    
    [Header("Cámara y Orientación")]
    [Tooltip("Referencia a la cámara principal.")]
    public Transform cameraTransform; // Los ojos del jugador. Necesitamos saber para dónde mira.
    
    [Tooltip("Velocidad con la que el personaje rota su cuerpo al girar.")]
    public float rotationSpeed = 10.0f; // Qué tan suave voltea la espalda al cambiar de dirección.

    private CharacterController controller; // El cilindro físico invisible que moveremos.

    void Start() 
    {
        // Al nacer, nos conectamos a nuestro propio cilindro invisible.
        controller = GetComponent<CharacterController>(); 

        // Si se nos olvidó conectar la cámara en el inspector, la busca automáticamente.
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update() // Se ejecuta todo el tiempo, como los reflejos del cerebro
    {
        // ---------------------------------------------------------
        // PASO 1: PREGUNTAR QUÉ TECLAS ESTÁN PRESIONADAS (W, A, S, D)
        // ---------------------------------------------------------
        float horizontalInput = 0f; // Eje X: Derecha (1) o Izquierda (-1)
        float verticalInput = 0f; // Eje Z: Adelante (1) o Atrás (-1)

        if (Keyboard.current != null)
        {
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput = 1f; 
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput = -1f;

            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput = -1f;
        }

        // ---------------------------------------------------------
        // PASO 2: TRADUCIR LAS TECLAS A LA DIRECCIÓN DE LA CÁMARA
        // ---------------------------------------------------------
        Vector3 moveDirection = Vector3.zero; // Inicializamos una flecha sin dirección.
        
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward; // El frente de la cámara
            Vector3 camRight = cameraTransform.right; // La derecha de la cámara
            
            camForward.y = 0f; // Ignoramos la altura. Si miramos al cielo, no queremos volar.
            camRight.y = 0f; 
            camForward.Normalize(); // Aplanamos la matemática a una medida exacta de 1.
            camRight.Normalize();

            // Mezclamos la dirección de la cámara con las teclas que presionamos.
            moveDirection = camRight * horizontalInput + camForward * verticalInput;
            
            // "Normalize" evita que caminar en diagonal (W + D) sea más rápido matemáticamente que caminar recto.
            if (moveDirection.magnitude > 1f) moveDirection.Normalize(); 
        }

        // ---------------------------------------------------------
        // PASO 3: GIRAR EL CUERPO DEL MUÑECO 3D (Modelo)
        // ---------------------------------------------------------
        if (moveDirection.magnitude > 0.1f) // Si nos estamos moviendo...
        {
            // Calcula matemáticamente el ángulo hacia el que debemos voltear.
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection); 
            // "Slerp" hace que el muñeco gire poco a poco de forma suave, en vez de darse la vuelta bruscamente de golpe.
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // ---------------------------------------------------------
        // PASO 4: CAMINAR FÍSICAMENTE (Mover el cilindro)
        // ---------------------------------------------------------
        // Creamos una gravedad constante y simple hacia abajo (-9.81) para que el jugador nunca flote en el aire al bajar escaleras.
        Vector3 gravityForce = Vector3.down * 9.81f;
        
        // Juntamos el empuje hacia adelante (caminar) con el empuje hacia abajo (gravedad).
        Vector3 finalMovement = (moveDirection * walkSpeed) + gravityForce;

        // Le damos la orden final al cilindro invisible: "¡Muévete y cuidado con las paredes!"
        // Multiplicamos por Time.deltaTime para que la velocidad sea idéntica en cualquier PC (rápida o lenta).
        controller.Move(finalMovement * Time.deltaTime);
    }
}
