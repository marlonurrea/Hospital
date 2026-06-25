using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Gestiona la transición suave entre niveles mediante un fundido (fade in/out)
/// y carga de escenas asíncrona de manera persistente.
/// </summary>
public class LevelTransitionManager : MonoBehaviour
{
    // Instancia única (Singleton) accesible desde cualquier script
    public static LevelTransitionManager Instance { get; private set; }

    [Header("Configuración de Transición")]
    [Tooltip("El CanvasGroup que contiene la imagen oscura para cubrir la pantalla.")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("Duración en segundos del fundido de entrada y salida.")]
    [SerializeField] private float fadeDuration = 0.8f;

    private bool isTransitioning = false;

    private void Awake()
    {
        // Implementar el patrón Singleton persistente
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Asegurarse de que al iniciar la pantalla esté visible y no bloquee clics
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Inicia la transición a una nueva escena.
    /// </summary>
    /// <param name="sceneName">Nombre exacto de la escena en Build Settings.</param>
    public void TransitionToScene(string sceneName)
    {
        if (isTransitioning) return;
        StartCoroutine(TransitionCoroutine(sceneName));
    }

    /// <summary>
    /// Corrutina que realiza el fundido a negro, carga la escena de forma asíncrona y realiza el fundido a transparente.
    /// </summary>
    private IEnumerator TransitionCoroutine(string sceneName)
    {
        isTransitioning = true;

        // 1. Bloquear interacción del usuario con la UI durante la carga
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = true;
        }

        // 2. Fundido a negro (Fade In)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            }
            yield return null;
        }
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;

        // 3. Actualizar datos de escena en GameProgress antes de cargar
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.progressData.currentSceneName = sceneName;
            GameProgress.Instance.SaveProgress();
        }

        // 4. Cargar la escena de manera asíncrona en segundo plano
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        if (asyncLoad == null)
        {
            Debug.LogError($"[LevelTransitionManager] No se pudo cargar la escena '{sceneName}'. Asegúrate de que el nombre sea correcto y que la escena esté agregada en File > Build Settings.");
            
            // Fundido a transparente (Fade Out) para recuperar la pantalla
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                if (fadeCanvasGroup != null)
                {
                    fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                }
                yield return null;
            }
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = 0f;
                fadeCanvasGroup.blocksRaycasts = false;
            }
            isTransitioning = false;
            yield break;
        }

        // Esperar hasta que termine de cargar
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Pequeña espera de seguridad para evitar tirones iniciales
        yield return new WaitForSeconds(0.1f);

        // 5. Fundido a transparente (Fade Out)
        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            }
            yield return null;
        }
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;

        // 6. Desbloquear interacción del usuario
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }

        isTransitioning = false;
    }
}
