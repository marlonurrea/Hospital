using UnityEngine;
using UnityEngine.InputSystem; // Requerido para el nuevo Input System

public class KeyboardTester : MonoBehaviour
{
    void Update()
    {
        // Verificar si la tecla E fue presionada en este fotograma
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("¡La tecla E fue presionada correctamente en la consola!");
        }
    }
}
