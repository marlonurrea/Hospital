using UnityEngine; // Motor físico y herramientas de Unity
using UnityEngine.InputSystem; // Sistema moderno para detectar controles de mando o teclado

[RequireComponent(typeof(CharacterController))] // Obliga a Unity a ponerle un controlador físico de cápsula a este objeto
public class PlayerMovement : MonoBehaviour // Clase principal para controlar el movimiento del jugador
{
    [Header("Movimiento")] // Título en el Inspector
    [Tooltip("Velocidad de movimiento al caminar.")]
    public float walkSpeed = 6.0f; // Velocidad base
    
    [Tooltip("Velocidad de movimiento al correr (manteniendo Shift).")]
    public float runSpeed = 10.0f; // Velocidad acelerada

    [Header("Salto y Gravedad")]
    [Tooltip("Fuerza o altura del salto.")]
    public float jumpHeight = 1.5f; // Altura a la que llega al saltar
    
    [Tooltip("Fuerza de la gravedad aplicada al personaje.")]
    public float gravity = -9.81f; // Fuerza que tira al jugador hacia abajo (similar a la gravedad real de la Tierra)

    [Header("Habilidades")]
    [Tooltip("Permite al jugador correr.")]
    [SerializeField] private bool canSprint = false; // Interruptor para habilitar o deshabilitar que el jugador pueda correr

    [Tooltip("Permite al jugador saltar.")]
    [SerializeField] private bool canJump = false; // Interruptor para habilitar el salto

    [Header("Cámara y Orientación")]
    [Tooltip("Referencia a la cámara principal. Si se deja vacío, se asignará Camera.main automáticamente.")]
    public Transform cameraTransform; // Se necesita saber dónde está la cámara para moverse según hacia dónde miremos
    
    [Tooltip("Velocidad con la que el personaje rota hacia la dirección de movimiento.")]
    public float rotationSpeed = 10.0f; // Qué tan rápido se voltea el modelo 3D al cambiar de dirección

    private CharacterController controller; // Componente físico que mueve al jugador y choca con paredes
    private Vector3 velocity; // Fuerza vertical acumulada (para caer o saltar)
    private bool isGrounded; // Bandera para saber si estamos tocando el piso

    void Start() // Se ejecuta al principio
    {
        // Forzar a false para desactivar correr y saltar por completo (según el diseño actual de este juego)
        canSprint = false;
        canJump = false;

        // Buscamos nuestro propio componente controlador físico
        controller = GetComponent<CharacterController>();

        // Si olvidaron conectar la cámara en el inspector, la buscamos por nuestra cuenta
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Update() // Se ejecuta en cada fotograma
    {
        // Revisamos si las suelas de los zapatos del controlador tocan suelo
        isGrounded = controller.isGrounded;

        // Si tocamos suelo y nuestra velocidad nos estaba tirando hacia abajo...
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Le aplicamos una pequeñita fuerza constante hacia abajo para mantenerlo pegado a las rampas/piso
        }

        // Variables para almacenar lo que pulse el usuario
        float horizontalInput = 0f; // Movimiento Izquierda-Derecha (-1 a 1)
        float verticalInput = 0f; // Movimiento Adelante-Atrás (-1 a 1)
        bool isSprinting = false; // ¿Está corriendo?
        bool jumpPressed = false; // ¿Apretó saltar?

        // 1. Escaneo del Teclado
        if (Keyboard.current != null)
        {
            // Movimiento horizontal (A/D o Flechas)
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f; // Derecha
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f; // Izquierda

            // Movimiento vertical (W/S o Flechas)
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput += 1f; // Adelante
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput -= 1f; // Atrás

            // Si tiene permiso de correr, revisa si mantiene apretada la tecla Shift
            if (canSprint) isSprinting = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            
            // Si tiene permiso de saltar, revisa si presionó la Barra Espaciadora justo ahora
            if (canJump) jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        // 2. Escaneo del Mando (Gamepad)
        if (Gamepad.current != null)
        {
            // Lee qué tanto inclinó la palanca izquierda (joystick)
            Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
            
            // Ignoramos movimientos pequeñitos (zona muerta o "deadzone") para evitar que el jugador se mueva solo si el mando está viejo
            if (stickInput.sqrMagnitude > 0.02f)
            {
                horizontalInput += stickInput.x;
                verticalInput += stickInput.y;
            }

            // Limitamos los valores entre -1 y 1 por si alguien aprieta teclado y mando a la vez
            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
            verticalInput = Mathf.Clamp(verticalInput, -1f, 1f);

            // Correr pulsando la palanca hacia adentro (L3)
            if (canSprint && Gamepad.current.leftStickButton.isPressed) isSprinting = true;
            // Saltar apretando el botón de abajo (A en Xbox / Cruz en PS)
            if (canJump && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;
        }

        // Si caminas en diagonal usando teclado, irías más rápido (1 + 1 = 1.4). Esto "normaliza" la velocidad para que siempre sea máximo 1.
        float inputLength = Mathf.Sqrt(horizontalInput * horizontalInput + verticalInput * verticalInput);
        if (inputLength > 1f)
        {
            horizontalInput /= inputLength;
            verticalInput /= inputLength;
        }

        // Traducimos las teclas (W/A/S/D) a la dirección en la que está mirando la cámara
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null)
        {
            // Tomamos los ejes de la cámara y los aplanamos (quitamos la altura 'y')
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Combinamos las teclas que presionamos con la dirección en la que miramos
            moveDirection = camRight * horizontalInput + camForward * verticalInput;
        }
        else // Por si no hay cámara, se mueve basándose en el propio cuerpo del jugador
        {
            moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        }

        // Rotar visualmente el cuerpo del personaje de forma suave hacia donde camina
        if (moveDirection.magnitude > 0.1f)
        {
            // Calcula hacia qué grado debe rotar
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            // Slerp es una interpolación esférica que rota al personaje gradualmente
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Decide a qué velocidad iremos dependiendo de si apretamos Shift
        float speed = isSprinting ? runSpeed : walkSpeed;

        // SALTO: Aplica la fórmula física real para alcanzar exactamente la altura deseada (v = raízCuadrada(h * -2 * g))
        if (jumpPressed && isGrounded && canJump)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // GRAVEDAD: Va empujando al jugador hacia abajo cada fotograma
        velocity.y += gravity * Time.deltaTime;

        // Juntamos el movimiento de caminar (horizontal) y el de caer/saltar (vertical) en un solo vector final
        Vector3 movement = moveDirection * speed + Vector3.up * velocity.y;

        // Le ordenamos al controlador físico que efectúe el movimiento y se encargue de no atravesar paredes
        controller.Move(movement * Time.deltaTime);
    }
}
