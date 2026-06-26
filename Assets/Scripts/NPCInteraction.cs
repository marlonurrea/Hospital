using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCInteraction : MonoBehaviour, IInteractable
{
    [Header("Configuración de NPC")]
    [Tooltip("El mensaje que aparecerá al acercarse al NPC.")]
    public string promptText = "Presiona E para hablar con el Guardia";

    [Tooltip("ID único para registrar esta conversación en el progreso (si queda vacío se usará el nombre del objeto).")]
    public string npcId;

    [Header("Contenido del Diálogo")]
    [Tooltip("El nombre del NPC que se mostrará en la interfaz. Si se deja vacío, se buscará en el canvas o se usará el nombre del objeto.")]
    public string npcName;

    [Tooltip("El texto del diálogo que dirá el NPC. Si se deja vacío, se buscará en el canvas.")]
    [TextArea(3, 10)]
    public string npcDialogue;

    [Header("Configuración de Facturación (Recepción/Fin de Nivel)")]
    [Tooltip("Si está activo, este NPC actuará como el punto de facturación que completa el nivel.")]
    public bool esNPCFacturacion = false;

    [Tooltip("Mensaje que muestra si intentas facturar pero te faltan tareas por completar.")]
    [TextArea(3, 5)]
    public string mensajeTareasPendientes = "Aún no puedes facturar tu cita. Primero debes hablar con el Guardia, el Enfermero y el Civil.";

    [Header("Referencias de UI")]
    [Tooltip("El objeto/Canvas que contiene los diálogos del NPC.")]
    public GameObject npcDialogosCanvas;

    [Tooltip("El botón para continuar/salir del diálogo.")]
    public Button botonContinuar;

    private bool puedeFacturar = false;

    private void Start()
    {
        if (string.IsNullOrEmpty(npcId))
        {
            npcId = gameObject.name;
        }

        // Autodetección para Recepcionista
        if (gameObject.name.ToLower().Contains("recepcionista") || npcId.ToLower().Contains("recepcionista"))
        {
            esNPCFacturacion = true;
        }

        // Asegurarse de que el diálogo esté cerrado al inicio
        if (npcDialogosCanvas != null)
        {
            npcDialogosCanvas.SetActive(false);

            // Si el botón no está asignado o pertenece a otro Canvas (error común en el Inspector),
            // intentar buscar el botón correcto dentro de los hijos de nuestro propio canvas.
            if (botonContinuar == null || !botonContinuar.transform.IsChildOf(npcDialogosCanvas.transform))
            {
                Button foundBtn = npcDialogosCanvas.GetComponentInChildren<Button>(true);
                if (foundBtn != null)
                {
                    botonContinuar = foundBtn;
                    Debug.Log($"[NPCInteraction] Asignado botón continuar de '{gameObject.name}' dinámicamente desde sus hijos.");
                }
            }

            // Salvaguarda: si los textos están vacíos en el Inspector, cargamos los que ya están en el Canvas
            if (string.IsNullOrEmpty(npcName))
            {
                npcName = GetExistingNameFromCanvas();
                if (string.IsNullOrEmpty(npcName))
                {
                    npcName = gameObject.name.Replace("Npc", "").Replace("npc", "").Trim();
                }
            }

            if (string.IsNullOrEmpty(npcDialogue))
            {
                npcDialogue = GetExistingDialogueFromCanvas();
            }
        }

        // Configurar el listener del botón para que cierre el diálogo al presionarlo
        if (botonContinuar != null)
        {
            botonContinuar.onClick.AddListener(CerrarDialogo);
        }
        else
        {
            Debug.LogWarning($"NPCInteraction: No se ha asignado el botón de continuar para '{gameObject.name}'.");
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
            // Lógica de facturación
            if (esNPCFacturacion)
            {
                int tareasCompletadas = 0;
                if (GameProgress.Instance != null)
                {
                    tareasCompletadas = GameProgress.Instance.progressData.completedTasks.Count;
                    // Si ya completamos esta tarea de facturación antes, restamos 1 para validar las otras
                    if (GameProgress.Instance.IsTaskCompleted(npcId))
                    {
                        tareasCompletadas--;
                    }
                }

                // Necesitamos haber completado las otras 3 misiones (totalMainTasks - 1)
                int tareasRequeridas = (GameProgress.Instance != null) ? GameProgress.Instance.totalMainTasks - 1 : 3;

                if (tareasCompletadas >= tareasRequeridas)
                {
                    puedeFacturar = true;
                    UpdateDialogueTexts(npcName, npcDialogue);
                }
                else
                {
                    puedeFacturar = false;
                    UpdateDialogueTexts(npcName, mensajeTareasPendientes);
                }
            }
            else
            {
                // NPC normal: actualizar con el diálogo por defecto
                UpdateDialogueTexts(npcName, npcDialogue);
            }

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
        // Doble seguridad: solo procesar el cierre y completar progreso si el canvas de este diálogo está realmente abierto
        if (npcDialogosCanvas != null && npcDialogosCanvas.activeSelf)
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

            // Lógica de completar tarea y victoria
            if (esNPCFacturacion)
            {
                if (puedeFacturar)
                {
                    if (GameProgress.Instance != null)
                    {
                        GameProgress.Instance.CompleteTask(npcId);
                    }

                    // Activar la pantalla de nivel completado (Victoria)
                    if (LevelComplete.Instance != null)
                    {
                        LevelComplete.Instance.TriggerLevelComplete();
                    }
                }
            }
            else
            {
                // NPC normal
                if (GameProgress.Instance != null)
                {
                    bool canComplete = true;
                    string lowerName = gameObject.name.ToLower();
                    string lowerId = (npcId ?? "").ToLower();

                    bool isCivil = lowerName.Contains("civil") || lowerId.Contains("civil");
                    bool isEnfermero = lowerName.Contains("enfermero") || lowerId.Contains("enfermero");

                    if (isCivil)
                    {
                        canComplete = GameProgress.Instance.IsGuardiaCompleted();
                    }
                    else if (isEnfermero)
                    {
                        canComplete = GameProgress.Instance.IsCivilCompleted();
                    }

                    if (canComplete)
                    {
                        GameProgress.Instance.CompleteTask(npcId);
                    }
                    else
                    {
                        Debug.Log($"[NPCInteraction] No se completó la tarea para {npcId} porque no se cumple el orden de interacción requerido (Guardia -> Civil -> Enfermero).");
                    }
                }
            }
        }
    }

    private void UpdateDialogueTexts(string title, string content)
    {
        if (npcDialogosCanvas == null) return;

        // Actualizar TextMeshPro
        TextMeshProUGUI[] tmproTexts = npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in tmproTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name"))
            {
                txt.text = title;
            }
            else if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content"))
            {
                txt.text = content;
            }
        }

        // Actualizar Text heredado
        Text[] legacyTexts = npcDialogosCanvas.GetComponentsInChildren<Text>(true);
        foreach (var txt in legacyTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name"))
            {
                txt.text = title;
            }
            else if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content"))
            {
                txt.text = content;
            }
        }
    }

    private string GetExistingNameFromCanvas()
    {
        if (npcDialogosCanvas == null) return null;

        TextMeshProUGUI[] tmproTexts = npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in tmproTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name"))
            {
                return txt.text;
            }
        }

        Text[] legacyTexts = npcDialogosCanvas.GetComponentsInChildren<Text>(true);
        foreach (var txt in legacyTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name"))
            {
                return txt.text;
            }
        }

        return null;
    }

    private string GetExistingDialogueFromCanvas()
    {
        if (npcDialogosCanvas == null) return null;

        TextMeshProUGUI[] tmproTexts = npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in tmproTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content"))
            {
                return txt.text;
            }
        }

        Text[] legacyTexts = npcDialogosCanvas.GetComponentsInChildren<Text>(true);
        foreach (var txt in legacyTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content"))
            {
                return txt.text;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (botonContinuar != null)
        {
            botonContinuar.onClick.RemoveListener(CerrarDialogo);
        }
    }
}
