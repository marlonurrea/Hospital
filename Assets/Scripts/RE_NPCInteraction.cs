using UnityEngine; // Librería estándar de Unity.
using UnityEngine.UI; // Herramienta para Botones (Botón 'Continuar').
using TMPro; // Textos bonitos y nítidos.

// -----------------------------------------------------------------------------
// SCRIPT: RE_NPCInteraction
// METÁFORA: "El Libreto de los Actores"
// Este script está metido adentro de todos los NPCs (Guardia, Civil, Enfermero).
// Contiene lo que te van a decir, sabe cuándo callarse, y sabe cuándo es hora 
// de firmar el documento para que pases de nivel (La Recepcionista).
// -----------------------------------------------------------------------------
public class RE_NPCInteraction : MonoBehaviour, IInteractable 
{
    [Header("Configuración de NPC")]
    [Tooltip("El mensaje que aparecerá al acercarse al NPC.")]
    public string promptText = "Presiona E para hablar"; // Letrero flotante que el jugador ve.

    [Tooltip("ID único para registrar esta conversación en el progreso.")]
    public string npcId; // La cédula de identidad del NPC. Para saber si es Juan o Pedro.

    [Header("Contenido del Diálogo")]
    [Tooltip("El nombre del NPC que se mostrará en la interfaz.")]
    public string npcName; // Su nombre en los subtítulos.

    [Tooltip("El texto del diálogo que dirá el NPC.")]
    [TextArea(3, 10)] // Obliga a Unity a pintar una caja de texto grande en el Inspector (como un bloc de notas).
    public string npcDialogue; // Su parlamento o guión.

    [Header("Configuración de Facturación (Recepción/Fin de Nivel)")]
    [Tooltip("Si está activo, este NPC actuará como el punto de facturación que completa el nivel.")]
    public bool esNPCFacturacion = false; // Interruptor: Falso = NPC normal. Verdadero = NPC Jefe (El que termina el juego).

    [Tooltip("Mensaje que muestra si intentas facturar pero te faltan tareas por completar.")]
    [TextArea(3, 5)] 
    public string mensajeTareasPendientes = "Aún no puedes facturar. Habla con los demás primero."; // El texto de regaño si haces trampa.

    [Header("Referencias de UI")]
    [Tooltip("El objeto/Canvas que contiene los diálogos del NPC.")]
    public GameObject npcDialogosCanvas; // El papel gigante frente a la cámara que tiene dibujada la caja de texto.

    [Tooltip("El botón para continuar/salir del diálogo.")]
    public Button botonContinuar; // El botón físico para apretar.

    private bool puedeFacturar = false; // Bandera de estado temporal: ¿Cumpliste las misiones para ganar?

    private void Start() // Cuando el NPC aparece en el mapa (Nace)...
    {
        // Si se nos olvidó escribir la cédula (ID), el código toma el nombre del modelo 3D y se lo pone.
        if (string.IsNullOrEmpty(npcId)) npcId = gameObject.name;

        // TRAMPA INTELIGENTE: Si el NPC tiene la palabra "recepcionista" en su nombre, se auto-convierte en el NPC Final automáticamente.
        if (gameObject.name.ToLower().Contains("recepcionista") || npcId.ToLower().Contains("recepcionista"))
        {
            esNPCFacturacion = true; 
        }

        if (npcDialogosCanvas != null) 
        {
            npcDialogosCanvas.SetActive(false); // Ocultamos el telón visual de inmediato para que no flote un cuadro gigante negro.

            // Si olvidamos arrastrar el botón "Continuar" al inspector, él busca un botón automáticamente entre sus hijos.
            if (botonContinuar == null || !botonContinuar.transform.IsChildOf(npcDialogosCanvas.transform))
            {
                Button foundBtn = npcDialogosCanvas.GetComponentInChildren<Button>(true); 
                if (foundBtn != null) botonContinuar = foundBtn; 
            }

            // REGLA SUPREMA: Siempre borramos lo que haya en el Inspector y chupamos el texto visualmente escrito en el propio Canvas.
            // Esto evita que clones/copias conserven diálogos antiguos del guardia.
            npcName = GetExistingNameFromCanvas();
            if (string.IsNullOrEmpty(npcName)) npcName = gameObject.name.Replace("Npc", "").Trim(); // Si el nombre está en blanco, usa el nombre del modelo 3D limpiándole la palabra "Npc".

            npcDialogue = GetExistingDialogueFromCanvas();
        }

        // Le atamos un hilo al botón de continuar. Al hacerle clic, disparará la orden "CerrarDialogo()".
        if (botonContinuar != null)
        {
            botonContinuar.onClick.AddListener(CerrarDialogo);
        }
    }

    // Parte del contrato "IInteractable". Devolvemos el letrero flotante que el jugador leerá.
    public string GetInteractPrompt()
    {
        return promptText;
    }

    // Parte del contrato "IInteractable". Se acciona cuando el jugador aprieta la tecla E sobre nosotros.
    public void Interact()
    {
        AbrirDialogo(); 
    }

    // METÁFORA: "La Charla". Congela el mundo y abre la boca del personaje.
    private void AbrirDialogo() 
    {
        if (npcDialogosCanvas != null) 
        {
            // SI ESTE NPC ES EL REVISOR FINAL (Recepcionista)
            if (esNPCFacturacion) 
            {
                // Contamos cuántos chulos/checks tiene la libreta del alcalde (GameProgress).
                int tareasCompletadas = RE_GameProgress.Instance != null ? RE_GameProgress.Instance.progressData.completedTasks.Count : 0;
                
                // Si la misión de la recepcionista ya está marcada, la restamos de las matemáticas para no engañar el cálculo.
                if (RE_GameProgress.Instance != null && RE_GameProgress.Instance.IsTaskCompleted(npcId)) tareasCompletadas--;

                // Si hay 4 NPCs en total en el juego, restamos 1 (la recepcionista misma) = Tenemos que haber hablado con 3.
                int tareasRequeridas = RE_GameProgress.Instance != null ? RE_GameProgress.Instance.totalMainTasks - 1 : 3;

                if (tareasCompletadas >= tareasRequeridas) // Si 3 es mayor o igual a 3 (¡Las hicimos todas!)...
                {
                    puedeFacturar = true; // Levantamos el pulgar.
                    UpdateDialogueTexts(npcName, npcDialogue); // Inyectamos su saludo de victoria en la pantalla.
                }
                else // Si intentamos saltarnos a un NPC...
                {
                    puedeFacturar = false; // Bajamos el pulgar.
                    UpdateDialogueTexts(npcName, mensajeTareasPendientes); // Inyectamos el texto del regaño en la pantalla.
                }
            }
            // SI ES UN NPC NORMAL Y CORRIENTE
            else 
            {
                UpdateDialogueTexts(npcName, npcDialogue); // Simplemente inyectamos lo que tienen que decir.
            }

            npcDialogosCanvas.SetActive(true); // Encendemos el panel gigante de la conversación.
            
            // Liberamos al puntero del ratón para que haga clic en el botón.
            Cursor.lockState = CursorLockMode.None; 
            Cursor.visible = true; 

            // Si es un nivel contra reloj, "Zapeamos" al reloj para que se ponga en pausa mientras leemos (O el jugador morirá charlando).
            if (RE_PlayerHealth.Instance != null) RE_PlayerHealth.Instance.SetPaused(true);

            // Buscamos al jugador por la ropa (El script PlayerMovement) y lo inmovilizamos, apagándole los motores.
            RE_PlayerMovement pm = FindFirstObjectByType<RE_PlayerMovement>();
            if (pm != null) pm.enabled = false;
        }
    }

    // Se ejecuta al hacer clic en el botón de pantalla.
    public void CerrarDialogo() 
    {
        if (npcDialogosCanvas != null && npcDialogosCanvas.activeSelf) // Si de verdad estaba abierto...
        {
            npcDialogosCanvas.SetActive(false); // Bajamos el telón y lo escondemos.
            
            // Volvemos a encerrar al ratón en el centro para seguir jugando FPS/TPS.
            Cursor.lockState = CursorLockMode.Locked; 
            Cursor.visible = false; 

            // Le quitamos la pausa al cronómetro de la muerte.
            if (RE_PlayerHealth.Instance != null) RE_PlayerHealth.Instance.SetPaused(false);

            // Le devolvemos las piernas al jugador.
            RE_PlayerMovement pm = FindFirstObjectByType<RE_PlayerMovement>();
            if (pm != null) pm.enabled = true;

            // -------------------------------------------------------------
            // LÓGICA TRAS BASTIDORES: Procesar qué pasa DESPUÉS de la charla.
            // -------------------------------------------------------------
            if (esNPCFacturacion) // Si era el NPC Final...
            {
                if (puedeFacturar) // Y además de ser el NPC Final, le habíamos levantado el pulgar arriba...
                {
                    if (RE_GameProgress.Instance != null) RE_GameProgress.Instance.CompleteTask(npcId); // Ponle un chulo a la recepcionista.
                    if (RE_LevelComplete.Instance != null) RE_LevelComplete.Instance.TriggerLevelComplete(); // Llama al Juez del Nivel para que tire confeti.
                }
            }
            else // Si era un NPC común de historia (Guardia, Civil, Enfermero)...
            {
                if (RE_GameProgress.Instance != null)
                {
                    bool canComplete = true; // Por ahora, asumimos que hicimos las cosas bien.
                    
                    // Pasamos su nombre a minúsculas para que el computador no se confunda entre "Civil" y "CIVIL".
                    string lowerName = gameObject.name.ToLower();
                    string lowerId = (npcId ?? "").ToLower();

                    bool isCivil = lowerName.Contains("civil") || lowerId.Contains("civil");
                    bool isEnfermero = lowerName.Contains("enfermero") || lowerId.Contains("enfermero");

                    // REGLAS ESPECÍFICAS DE TU HISTORIA: 
                    // Si hablo con el civil... ¡Espero que ya hayas hablado con el guardia!
                    if (isCivil) canComplete = RE_GameProgress.Instance.IsGuardiaCompleted();
                    // Si hablo con el enfermero... ¡Espero que ya hayas hablado con el civil!
                    else if (isEnfermero) canComplete = RE_GameProgress.Instance.IsCivilCompleted();

                    if (canComplete) // Si fuiste un buen chico y seguiste el orden...
                    {
                        RE_GameProgress.Instance.CompleteTask(npcId); // Márcate un chulo en la libreta.
                    }
                    else // Si fuiste directo al enfermero saltándote la historia...
                    {
                        // Anotamos en el registro secreto que no le valemos la misión.
                        Debug.Log($"[RE_NPCInteraction] Tarea no completada para {npcId} por no seguir el orden.");
                    }
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------------
    // FUNCIONES MÁGICAS AUXILIARES 
    // Su único propósito es coger tus textos y literalmente escribirlos dentro de los cuadritos de Unity.
    // -----------------------------------------------------------------------------------------
    private void UpdateDialogueTexts(string title, string content) 
    {
        if (npcDialogosCanvas == null) return; 

        // Escanea a todos los Hijos y Nietos visuales del Canvas buscando cualquier texto "TextMeshProUGUI".
        TextMeshProUGUI[] tmproTexts = npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var txt in tmproTexts)
        {
            // ¿Cómo se llama ese cuadrito? (ej: "Texto Nombre guardia" o "Textos npc")
            string objName = txt.gameObject.name.ToLower();
            
            // Si el cuadrito se llama "nombre", o "title"... le inyectamos en la vena la variable 'title' (Que es tu npcName).
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name")) txt.text = title; 
            
            // Si el cuadrito se llama "texto", o "cuerpo"... le inyectamos en la vena el parlamento (Tu npcDialogue).
            else if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content")) txt.text = content; 
        }
    }

    private string GetExistingNameFromCanvas() 
    {
        if (npcDialogosCanvas == null) return null;
        // Revisa todos los textos buscando el que sea el título, y lo "roba" para devolvértelo.
        foreach (var txt in npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true))
            if (txt.gameObject.name.ToLower().Contains("nombre") || txt.gameObject.name.ToLower().Contains("title")) return txt.text;
        return null;
    }

    private string GetExistingDialogueFromCanvas()
    {
        if (npcDialogosCanvas == null) return null;
        foreach (var txt in npcDialogosCanvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            string objName = txt.gameObject.name.ToLower();
            // ¡Peligro! Ignora el texto del nombre para no confundirlo con el diálogo.
            if (objName.Contains("nombre") || objName.Contains("title") || objName.Contains("name")) continue; 
            
            // Si encontró algo parecido al diálogo... "Róbatelo" y devuélvemelo en crudo.
            if (objName.Contains("texto") || objName.Contains("dialog") || objName.Contains("cuerpo") || objName.Contains("content")) return txt.text;
        }
        return null;
    }

    private void OnDestroy() 
    {
        // Limpiamos los hilos invisibles del botón si pasamos de nivel, para evitar errores fantasmas.
        if (botonContinuar != null) botonContinuar.onClick.RemoveListener(CerrarDialogo);
    }
}
