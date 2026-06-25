using UnityEngine;
using UnityEngine.UI;

public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuración de NPC")]
    [Tooltip("El mensaje que aparecerá al acercarse al NPC.")]
    public string promptText = "Presiona E para hablar con el Guardia";

    [Tooltip("ID único para registrar esta conversación en el progreso (si queda vacío se usará el nombre del objeto).")]
    public string npcId;

    [Header("Referencias de UI")]
    [Tooltip("El objeto/Canvas que contiene los diálogos del NPC.")]
    public GameObject npcDialogosCanvas;

    [Tooltip("El botón para continuar/salir del diálogo.")]
    public Button botonContinuar;

    private void Start()
    {
        if (string.IsNullOrEmpty(npcId))
        {
            npcId = gameObject.name;
        }

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
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 1. Pausar la cuenta atrás de la barra de salud (temporizador)
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.SetPaused(true);
            }

            // 2. Desactivar el script de movimiento para dejar al jugador estático
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = false;
            }
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
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // 1. Reanudar la cuenta atrás de la barra de salud
            if (PlayerHealth.Instance != null)
            {
                PlayerHealth.Instance.SetPaused(false);
            }

            // 2. Reactivar el script de movimiento para que el jugador se mueva
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = true;
            }

            if (GameProgress.Instance != null)
            {
                GameProgress.Instance.CompleteTask(npcId);
            }
        }
    }

    private void OnDestroy()
    {
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveListener(CerrarDialogo);
        }
    }
}
