using UnityEngine;
using UnityEngine.InputSystem; // Requerido para el nuevo Input System de Unity

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Velocidad de movimiento al caminar.")]
    public float walkSpeed = 6.0f;
    
    [Tooltip("Velocidad de movimiento al correr (manteniendo Shift).")]
    public float runSpeed = 10.0f;

    [Header("Salto y Gravedad")]
    [Tooltip("Fuerza o altura del salto.")]
    public float jumpHeight = 1.5f;
    
    [Tooltip("Fuerza de la gravedad aplicada al personaje.")]
    public float gravity = -9.81f;

    [Header("Habilidades")]
    [Tooltip("Permite al jugador correr.")]
    [SerializeField] private bool canSprint = false;

    [Tooltip("Permite al jugador saltar.")]
    [SerializeField] private bool canJump = false;

    [Header("Cámara y Orientación")]
    [Tooltip("Referencia a la cámara principal. Si se deja vacío, se asignará Camera.main automáticamente.")]
    public Transform cameraTransform;
    [Tooltip("Velocidad con la que el personaje rota hacia la dirección de movimiento.")]
    public float rotationSpeed = 10.0f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        // Forzar a false para desactivar correr y saltar por completo (según requerimiento de diseño)
        canSprint = false;
        canJump = false;

        // Obtener la referencia al CharacterController
        controller = GetComponent<CharacterController>();

        // Si no se asignó cámara, buscar la cámara principal
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    void Update()
    {
        // Obtener el estado de si está tocando el suelo directamente del controlador
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            // Pequeña fuerza hacia abajo constante para mantener al personaje pegado al suelo
            velocity.y = -2f;
        }

        // Variables de entrada para el nuevo Input System
        float horizontalInput = 0f;
        float verticalInput = 0f;
        bool isSprinting = false;
        bool jumpPressed = false;

        // 1. Leer teclado (si está disponible)
        if (Keyboard.current != null)
        {
            // Movimiento horizontal (A/D o Flechas)
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;

            // Movimiento vertical (W/S o Flechas)
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput -= 1f;

            // Correr (Shift Izquierdo o Derecho)
            if (canSprint)
            {
                isSprinting = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
            }

            // Saltar (Barra Espaciadora)
            if (canJump)
            {
                jumpPressed = Keyboard.current.spaceKey.wasPressedThisFrame;
            }
        }

        // 2. Leer mando/gamepad (si está disponible)
        if (Gamepad.current != null)
        {
            Vector2 stickInput = Gamepad.current.leftStick.ReadValue();
            // Aplicar zona muerta (deadzone) para evitar drift
            if (stickInput.sqrMagnitude > 0.02f)
            {
                horizontalInput += stickInput.x;
                verticalInput += stickInput.y;
            }

            // Limitar la entrada combinada de teclado + mando
            horizontalInput = Mathf.Clamp(horizontalInput, -1f, 1f);
            verticalInput = Mathf.Clamp(verticalInput, -1f, 1f);

            // Correr con el stick izquierdo (L3 / Left Stick Button)
            if (canSprint && Gamepad.current.leftStickButton.isPressed) isSprinting = true;

            // Saltar con el botón sur (A en Xbox / Cruz en PlayStation)
            if (canJump && Gamepad.current.buttonSouth.wasPressedThisFrame) jumpPressed = true;
        }

        // Normalizar entrada direccional combinada para evitar que el movimiento diagonal sea más rápido
        float inputLength = Mathf.Sqrt(horizontalInput * horizontalInput + verticalInput * verticalInput);
        if (inputLength > 1f)
        {
            horizontalInput /= inputLength;
            verticalInput /= inputLength;
        }

        // Calcular la dirección del movimiento en relación con la orientación de la cámara
        Vector3 moveDirection = Vector3.zero;
        if (cameraTransform != null)
        {
            // Obtener el vector adelante y derecha de la cámara proyectados sobre el plano horizontal
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // Dirección final de movimiento en el plano XZ
            moveDirection = camRight * horizontalInput + camForward * verticalInput;
        }
        else
        {
            // Fallback si no hay cámara asignada
            moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;
        }

        // Rotar al personaje hacia la dirección de movimiento de forma suave
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Determinar la velocidad actual (Correr o caminar)
        float speed = isSprinting ? runSpeed : walkSpeed;

        // Controlar el salto (si está habilitado y en el suelo)
        if (jumpPressed && isGrounded && canJump)
        {
            // Fórmula física para calcular la velocidad necesaria para alcanzar la altura deseada: v = sqrt(h * -2 * g)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Aplicar la gravedad acumulada a la velocidad vertical
        velocity.y += gravity * Time.deltaTime;

        // Combinar movimiento horizontal y velocidad vertical en un único vector de velocidad
        Vector3 movement = moveDirection * speed + Vector3.up * velocity.y;

        // Mover al personaje usando una única llamada a controller.Move para evitar vibración de colisiones (stutter/jitter)
        controller.Move(movement * Time.deltaTime);
    }
}


