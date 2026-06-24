using UnityEngine;
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
    }
}
