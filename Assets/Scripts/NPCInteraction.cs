using UnityEngine;
<<<<<<< Updated upstream
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuración de NPC")]
    [Tooltip("El mensaje que aparecerá al acercarse al NPC.")]
    public string promptText = "Presiona E para hablar con el Guardia";

    [Header("Referencias de UI")]
    [Tooltip("El objeto/Canvas que contiene los diálogos del NPC.")]
    public GameObject npcDialogosCanvas;

    [Tooltip("El botón para continuar/salir del diálogo.")]
    public Button botonContinuar;

    private void Start()
    {
        // Asegurarse de que el diálogo esté cerrado al inicio
        if (npcDialogosCanvas != null)
        {
            npcDialogosCanvas.SetActive(false);
        }

        // Configurar el listener del botón para que cierre el diálogo al presionarlo
        if (botonContinuar != null)
        {
            botonContinuar.onClick.AddListener(CerrarDialogo);
        }
        else
        {
            Debug.LogWarning("NPCInteraction: No se ha asignado el botón de continuar.");
        }
    }

    // Método de la interfaz IInteractable
    public string GetInteractPrompt()
    {
        return promptText;
    }

    // Método de la interfaz IInteractable
    public void Interact()
    {
        AbrirDialogo();
    }

    private void AbrirDialogo()
    {
        if (npcDialogosCanvas != null)
        {
            npcDialogosCanvas.SetActive(true);
            
            // Opcional: Aquí podrías añadir lógica para bloquear el movimiento del jugador,
            // mostrar el cursor, o cambiar al mapa de inputs de la interfaz (UI).
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Debug.LogWarning("NPCInteraction: No se ha asignado el Canvas de Npcdialogos.");
        }
    }

    public void CerrarDialogo()
    {
        if (npcDialogosCanvas != null)
        {
            npcDialogosCanvas.SetActive(false);
            
            // Opcional: Aquí podrías restaurar el estado del cursor y devolver el control al jugador.
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDestroy()
    {
        // Es buena práctica remover los listeners para evitar memory leaks si el objeto se destruye
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveListener(CerrarDialogo);
        }
=======
using UnityEngine.Events;
using UnityEngine.InputSystem; // Requerido para el nuevo Input System (de KeyboardTester)

public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuración del NPC")]
    [Tooltip("Nombre del personaje no jugable.")]
    public string npcName = "NPC";

    [Header("Diálogos")]
    [Tooltip("Líneas de diálogo de este NPC.")]
    [TextArea(3, 5)]
    public string[] dialogueLines = new string[] { "Hola, ¿en qué puedo ayudarte?", "Ten cuidado por aquí." };

    [Tooltip("¿El diálogo debe comenzar de nuevo al terminar o quedarse en la última línea?")]
    public bool loopDialogue = true;

    [Header("Eventos de Interacción")]
    public UnityEvent onDialogueStart;
    
    [Tooltip("Evento que envía la línea de texto actual (útil para vincular con textos de UI).")]
    public UnityEvent<string> onDialogueLineChanged;
    
    public UnityEvent onDialogueEnd;

    private int currentLineIndex = -1;
    private bool isInteracting = false;
    private int dialogueStartFrame = -1; // Para evitar que se autocomplete en el mismo fotograma de inicio

    void Update()
    {
        // Si estamos interactuando y presionamos la tecla E, avanzamos el diálogo
        // Se verifica Time.frameCount > dialogueStartFrame para evitar avanzar en el mismo frame que se inició la interacción
        if (isInteracting && Time.frameCount > dialogueStartFrame)
        {
            bool advancePressed = false;

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                advancePressed = true;
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                advancePressed = true;
            }

            if (advancePressed)
            {
                Debug.Log($"[KeyboardTester integrado] Tecla E presionada durante interacción con {npcName}.");
                Interact();
            }
        }
    }

    public string GetInteractPrompt()
    {
        // Retorna el prompt contextual con el nombre del NPC
        return $"Hablar con {npcName}";
    }

    public void Interact()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"[{npcName}] No tiene líneas de diálogo configuradas.");
            return;
        }

        // Si el DialogueManager está escribiendo texto actualmente, completarlo de golpe
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsTyping())
        {
            DialogueManager.Instance.TryCompleteLine();
            return;
        }

        // Si es la primera interacción o ya terminó y vuelve a iniciar
        if (!isInteracting || currentLineIndex == -1)
        {
            StartDialogue();
        }
        else
        {
            AdvanceDialogue();
        }
    }

    private void StartDialogue()
    {
        isInteracting = true;
        currentLineIndex = 0;
        dialogueStartFrame = Time.frameCount; // Registrar el frame de inicio
        
        Debug.Log($"[{npcName}]: {dialogueLines[currentLineIndex]}");
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OpenDialogue(npcName);
            DialogueManager.Instance.DisplayLine(dialogueLines[currentLineIndex]);
        }

        onDialogueStart?.Invoke();
        onDialogueLineChanged?.Invoke(dialogueLines[currentLineIndex]);
    }

    private void AdvanceDialogue()
    {
        currentLineIndex++;

        // Verificar si hemos llegado al final de las líneas de diálogo
        if (currentLineIndex >= dialogueLines.Length)
        {
            EndDialogue();
        }
        else
        {
            Debug.Log($"[{npcName}]: {dialogueLines[currentLineIndex]}");
            
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.DisplayLine(dialogueLines[currentLineIndex]);
            }

            onDialogueLineChanged?.Invoke(dialogueLines[currentLineIndex]);
        }
    }

    private void EndDialogue()
    {
        Debug.Log($"Fin del diálogo con [{npcName}].");
        
        isInteracting = false;
        
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CloseDialogue();
        }

        if (loopDialogue)
        {
            currentLineIndex = -1; // Se resetea para que la próxima interacción inicie el diálogo
        }
        else
        {
            currentLineIndex = dialogueLines.Length - 1; // Se queda en la última línea
        }

        onDialogueEnd?.Invoke();
    }

    // Método de conveniencia para reiniciar el estado de diálogo desde fuera (ej: si el jugador se aleja)
    public void ResetDialogue()
    {
        isInteracting = false;
        currentLineIndex = -1;
>>>>>>> Stashed changes
    }
}
