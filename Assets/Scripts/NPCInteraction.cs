using UnityEngine; // Herramientas estándar de Unity
using UnityEngine.UI; // Herramientas para interfaces de usuario (Botones, Textos antiguos)
using TMPro; // Herramientas de TextMeshPro (Textos mejorados)

public class NPCInteraction : MonoBehaviour, IInteractable // Clase para hablar con personajes; usa la interfaz IInteractable
{
    [Header("Configuración de NPC")] // Organización en el Inspector
    [Tooltip("El mensaje que aparecerá al acercarse al NPC.")] // Ayuda de Unity
    public string promptText = "Presiona E para hablar con el Guardia"; // Texto que pide pulsar botón

    [Tooltip("ID único para registrar esta conversación en el progreso.")] // Ayuda de Unity
    public string npcId; // Identificador para saber con quién hablamos

    [Header("Contenido del Diálogo")] // Sección de textos
    [Tooltip("El nombre del NPC que se mostrará en la interfaz.")] // Ayuda de Unity
    public string npcName; // Nombre del personaje que habla

    [Tooltip("El texto del diálogo que dirá el NPC.")] // Ayuda de Unity
    [TextArea(3, 10)] // Permite un cuadro de texto más grande en el Inspector
    public string npcDialogue; // Lo que dice el personaje

    [Header("Configuración de Facturación (Recepción/Fin de Nivel)")] // Sección de reglas de final de nivel
    [Tooltip("Si está activo, este NPC actuará como el punto de facturación que completa el nivel.")] // Ayuda
    public bool esNPCFacturacion = false; // Bandera para saber si este es el recepcionista final

    [Tooltip("Mensaje que muestra si intentas facturar pero te faltan tareas por completar.")] // Ayuda
    [TextArea(3, 5)] // Cuadro grande
    public string mensajeTareasPendientes = "Aún no puedes facturar. Habla con el Guardia, Civil y Enfermero primero."; // Rechazo si faltan tareas

    [Header("Referencias de UI")] // Sección visual
    [Tooltip("El objeto/Canvas que contiene los diálogos del NPC.")] // Ayuda
    public GameObject npcDialogosCanvas; // El panel que contiene la ventana de diálogo

    [Tooltip("El botón para continuar/salir del diálogo.")] // Ayuda
    public Button botonContinuar; // El botón físico en pantalla para cerrar

    private bool puedeFacturar = false; // Variable interna para determinar si ganamos el juego al hablarle

    private void Start() // Se ejecuta al inicio del nivel
    {
        // Si olvidamos ponerle un ID en el Inspector, usamos el nombre del GameObject 3D
        if (string.IsNullOrEmpty(npcId)) npcId = gameObject.name;

        // Auto-detección: Si el nombre del modelo contiene "recepcionista", automáticamente se marca como el NPC final
        if (gameObject.name.ToLower().Contains("recepcionista") || npcId.ToLower().Contains("recepcionista"))
        {
            esNPCFacturacion = true;
        }

        if (npcDialogosCanvas != null) // Si conectamos la ventana de diálogo
        {
            npcDialogosCanvas.SetActive(false); // Ocultamos el diálogo al empezar a jugar para que no estorbe

            // Autodetección del botón si olvidamos conectarlo y está dentro de nuestro panel
            if (botonContinuar == null || !botonContinuar.transform.IsChildOf(npcDialogosCanvas.transform))
            {
                Button foundBtn = npcDialogosCanvas.GetComponentInChildren<Button>(true); // Busca cualquier botón en los hijos
                if (foundBtn != null) botonContinuar = foundBtn; // Lo asigna
            }

            // Si olvidamos escribir el nombre en el Inspector, copiamos el que ya esté escrito en la interfaz gráfica
            if (string.IsNullOrEmpty(npcName))
            {
                npcName = GetExistingNameFromCanvas(); // Intenta extraer el nombre del Canvas
                if (string.IsNullOrEmpty(npcName)) npcName = gameObject.name.Replace("Npc", "").Trim(); // Si falla, limpia el nombre del objeto 3D
            }

            // Si olvidamos escribir el diálogo, copiamos el que esté escrito en el Canvas
            if (string.IsNullOrEmpty(npcDialogue))
            {
                npcDialogue = GetExistingDialogueFromCanvas();
            }
        }

        // Si tenemos un botón válido, le enseñamos a cerrar el diálogo al hacerle clic
        if (botonContinuar != null)
        {
            botonContinuar.onClick.AddListener(CerrarDialogo);
        }
        else
        {
            Debug.LogWarning($"[NPCInteraction] No se asignó el botón de continuar en '{gameObject.name}'."); // Aviso en consola
        }
    }

    // Cumple con la interfaz IInteractable: devuelve el texto flotante para el jugador
    public string GetInteractPrompt()
    {
        return promptText;
    }

    // Cumple con la interfaz IInteractable: se ejecuta al presionar "E" sobre el NPC
    public void Interact()
    {
        AbrirDialogo(); // Ejecuta la función principal
    }

    private void AbrirDialogo() // Muestra la ventana y detiene al jugador
    {
        if (npcDialogosCanvas != null) // Si el panel existe
        {
            if (esNPCFacturacion) // Si es la recepcionista final...
            {
                // Lógica especial de final: contamos cuántas misiones hemos hecho
                int tareasCompletadas = GameProgress.Instance != null ? GameProgress.Instance.progressData.completedTasks.Count : 0;
                
                // Si la recepcionista ya está contada, la restamos para que evalúe a los demás
                if (GameProgress.Instance != null && GameProgress.Instance.IsTaskCompleted(npcId)) tareasCompletadas--;

                // Necesitamos tener todas las misiones menos 1 (la propia recepcionista)
                int tareasRequeridas = GameProgress.Instance != null ? GameProgress.Instance.totalMainTasks - 1 : 3;

                if (tareasCompletadas >= tareasRequeridas) // Si tenemos todo listo
                {
                    puedeFacturar = true; // Autorizamos la victoria
                    UpdateDialogueTexts(npcName, npcDialogue); // Le decimos su frase normal
                }
                else // Si nos faltan misiones
                {
                    puedeFacturar = false; // Negamos la victoria
                    UpdateDialogueTexts(npcName, mensajeTareasPendientes); // Le decimos la frase de rechazo
                }
            }
            else // Si es un NPC cualquiera (guardia, civil, enfermero)
            {
                UpdateDialogueTexts(npcName, npcDialogue); // Muestra sus diálogos normales
            }

            npcDialogosCanvas.SetActive(true); // Hace visible el cuadro de diálogo
            
            Cursor.lockState = CursorLockMode.None; // Libera el ratón para dar clic en el botón de continuar
            Cursor.visible = true; // Lo muestra en pantalla

            // Pausa el cronómetro de la barra de vida del jugador mientras hablamos
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.SetPaused(true);

            // Congelamos al jugador para que no camine mientras lee
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }
    }

    public void CerrarDialogo() // Se ejecuta al darle clic a "Continuar"
    {
        if (npcDialogosCanvas != null && npcDialogosCanvas.activeSelf) // Verificamos que sí estaba abierto
        {
            npcDialogosCanvas.SetActive(false); // Ocultamos el cuadro
            
            Cursor.lockState = CursorLockMode.Locked; // Volveamos a fijar el ratón al centro
            Cursor.visible = false; // Lo ocultamos

            // Reanudamos la pérdida de vida por tiempo
            if (PlayerHealth.Instance != null) PlayerHealth.Instance.SetPaused(false);

            // Devolvemos el control de movimiento al jugador
            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) pm.enabled = true;

            // Procesamiento de victoria o tareas
            if (esNPCFacturacion) // Si era el último NPC
            {
                if (puedeFacturar) // Y cumplimos las condiciones...
                {
                    if (GameProgress.Instance != null) GameProgress.Instance.CompleteTask(npcId); // Marcamos su misión
                    if (LevelComplete.Instance != null) LevelComplete.Instance.TriggerLevelComplete(); // Activamos la victoria del nivel
                }
            }
            else // Si era un NPC de misiones normales
            {
                if (GameProgress.Instance != null)
                {
                    bool canComplete = true; // Bandera de validación
                    string lowerName = gameObject.name.ToLower();
                    string lowerId = (npcId ?? "").ToLower();

                    bool isCivil = lowerName.Contains("civil") || lowerId.Contains("civil");
                    bool isEnfermero = lowerName.Contains("enfermero") || lowerId.Contains("enfermero");

                    // Verificación de orden: Guardia -> Civil -> Enfermero
                    if (isCivil) canComplete = GameProgress.Instance.IsGuardiaCompleted();
                    else if (isEnfermero) canComplete = GameProgress.Instance.IsCivilCompleted();

                    if (canComplete) // Si hablamos en el orden correcto
                    {
                        GameProgress.Instance.CompleteTask(npcId); // Marcamos la misión como completada
                    }
                    else // Si saltamos el orden
                    {
                        Debug.Log($"[NPCInteraction] Tarea no completada para {npcId} por no seguir el orden (Guardia -> Civil -> Enfermero).");
                    }
                }
            }
        }
    }

    // Función auxiliar que busca los componentes de texto en la interfaz y les inyecta el nombre y el diálogo dinámicamente
    private void UpdateDialogueTexts(string title, string content) 
    {
        if (npcDialogosCanvas == null) return; // Si no hay panel, aborta

        // Actualizamos los textos modernos (TextMeshPro)
        TextMeshProUGUI[] tmproTexts = npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in tmproTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name")) txt.text = title; // Inyecta el nombre
            else if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content")) txt.text = content; // Inyecta la frase
        }

        // Actualizamos los textos antiguos (Legacy Text) por si se están usando
        Text[] legacyTexts = npcDialogosCanvas.GetComponentsInChildren<Text>(true);
        foreach (var txt in legacyTexts)
        {
            string objName = txt.gameObject.name.ToLower();
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name")) txt.text = title;
            else if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content")) txt.text = content;
        }
    }

    // Funciones auxiliares que raspan la interfaz visual en busca de texto si el usuario no rellenó los campos en el Inspector
    private string GetExistingNameFromCanvas() 
    {
        // Devuelve el texto encontrado en un objeto que parezca el "título"
        if (npcDialogosCanvas == null) return null;
        foreach (var txt in npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (txt.gameObject.name.ToLower().Contains("nombre") || txt.gameObject.name.ToLower().Contains("title")) return txt.text;
        return null;
    }

    private string GetExistingDialogueFromCanvas()
    {
        // Devuelve el texto encontrado en un objeto que parezca el "cuerpo" del mensaje
        if (npcDialogosCanvas == null) return null;
        foreach (var txt in npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (txt.gameObject.name.ToLower().Contains("texto") || txt.gameObject.name.ToLower().Contains("dialog")) return txt.text;
        return null;
    }

    private void OnDestroy() // Cuando se destruye el objeto, desvinculamos el evento del botón
    {
        if (botonContinuar != null) botonContinuar.onClick.RemoveListener(CerrarDialogo);
    }
}
