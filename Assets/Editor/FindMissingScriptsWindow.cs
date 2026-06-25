using UnityEngine;
using UnityEditor;

public class FindMissingScriptsWindow : EditorWindow
{
    [MenuItem("Tools/Buscar Scripts Faltantes")]
    public static void ShowWindow()
    {
        GetWindow<FindMissingScriptsWindow>("Scripts Faltantes");
    }

    private void OnGUI()
    {
        GUILayout.Label("Buscador de Componentes Faltantes (Missing Scripts)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Buscar en la Escena Activa", GUILayout.Height(30)))
        {
            FindInActiveScene();
        }
    }

    private static void FindInActiveScene()
    {
        GameObject[] allGameObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int missingCount = 0;
        int affectedObjectsCount = 0;

        foreach (GameObject go in allGameObjects)
        {
            Component[] components = go.GetComponents<Component>();
            bool hasMissingInObject = false;

            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                    hasMissingInObject = true;
                    Debug.LogWarning($"[Missing Script] El GameObject '{go.name}' tiene un componente faltante (índice {i}).", go);
                }
            }

            if (hasMissingInObject)
            {
                affectedObjectsCount++;
            }
        }

        Debug.Log($"[Búsqueda Finalizada] Se encontraron {missingCount} componentes faltantes en {affectedObjectsCount} GameObjects.");
    }

    [MenuItem("Tools/Restablecer Progreso Guardado")]
    public static void ResetSavedProgress()
    {
        // Buscar el componente GameProgress en la escena para leer su clave de guardado real
        GameProgress gp = GameObject.FindAnyObjectByType<GameProgress>();
        string keyToDelete = "HospitalGameProgress"; // Fallback por defecto

        if (gp != null)
        {
            // Usar SerializedObject para leer el campo privado 'saveKey' configurado en el inspector
            SerializedObject so = new SerializedObject(gp);
            SerializedProperty prop = so.FindProperty("saveKey");
            if (prop != null && !string.IsNullOrEmpty(prop.stringValue))
            {
                keyToDelete = prop.stringValue;
            }
        }

        // Borrar la clave detectada
        if (PlayerPrefs.HasKey(keyToDelete))
        {
            PlayerPrefs.DeleteKey(keyToDelete);
            Debug.Log($"[Editor] Se eliminó la clave de guardado activa: '{keyToDelete}'");
        }

        // Borrar variantes comunes por si acaso
        PlayerPrefs.DeleteKey("HospitalGameProgress");
        PlayerPrefs.DeleteKey("HospitalGam");
        PlayerPrefs.Save();

        Debug.Log("[Editor] ¡Restablecimiento completado! Todos los datos de progreso guardados han sido borrados de PlayerPrefs.");
    }
}
