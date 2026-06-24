using System.Collections;
using UnityEngine;
using TMPro; // Requiere TextMesh Pro en el proyecto

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Referencias de UI")]
    [Tooltip("Panel principal que contiene los textos de diálogo.")]
    public GameObject dialoguePanel;

    [Tooltip("Componente de texto para mostrar el nombre del NPC.")]
    public TMP_Text nameText;

    [Tooltip("Componente de texto para mostrar la línea de diálogo.")]
    public TMP_Text dialogueText;

    [Header("Ajustes del Texto")]
    [Tooltip("Tiempo de espera en segundos entre cada letra al escribir.")]
    public float typingSpeed = 0.03f;

    private bool isTyping = false;
    private string currentFullLine = "";
    private Coroutine typingCoroutine;

    private void Awake()
    {
        // Implementación del patrón Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Ocultar el panel de diálogo al iniciar el juego
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    /// <summary>
    /// Retorna si actualmente se está escribiendo una línea de texto de forma progresiva.
    /// </summary>
    public bool IsTyping()
    {
        return isTyping;
    }

    /// <summary>
    /// Activa la UI de diálogo y coloca el nombre del NPC correspondiente.
    /// </summary>
    public void OpenDialogue(string npcName)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            
            // Asegurar de activar y habilitar el Canvas padre por si estaba desactivado
            Canvas parentCanvas = dialoguePanel.GetComponentInParent<Canvas>(true);
            if (parentCanvas != null)
            {
                parentCanvas.gameObject.SetActive(true);
                parentCanvas.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning("<b>[DialogueManager]</b> La referencia 'Dialogue Panel' no está asignada en el Inspector.");
        }

        if (nameText != null)
        {
            nameText.text = npcName;
        }
        else
        {
            Debug.LogWarning("<b>[DialogueManager]</b> La referencia 'Name Text' no está asignada en el Inspector.");
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }
        else
        {
            Debug.LogWarning("<b>[DialogueManager]</b> La referencia 'Dialogue Text' no está asignada en el Inspector.");
        }
    }

    /// <summary>
    /// Muestra una línea de diálogo con un efecto de escritura letra a letra.
    /// </summary>
    public void DisplayLine(string line)
    {
        currentFullLine = line;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(line));
    }

    /// <summary>
    /// Completa la línea de diálogo de golpe si se está escribiendo progresivamente.
    /// </summary>
    public void TryCompleteLine()
    {
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }

            if (dialogueText != null)
            {
                dialogueText.text = currentFullLine;
            }

            isTyping = false;
        }
    }

    /// <summary>
    /// Oculta el panel de diálogo y limpia el estado actual.
    /// </summary>
    public void CloseDialogue()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        isTyping = false;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    // Corrutina para simular el efecto de máquina de escribir
    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            foreach (char letter in text.ToCharArray())
            {
                dialogueText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }
        isTyping = false;
    }
}
