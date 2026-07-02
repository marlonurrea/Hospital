using UnityEngine; // Importamos las herramientas principales de Unity para usar scripts y componentes

public class RE_DamageZone : MonoBehaviour // Define la clase principal para la zona de daño; hereda de MonoBehaviour
{
    [Header("Configuración del Daño")] // Agrupa las variables de daño en el Inspector de Unity
    [Tooltip("Cantidad de daño infligido al jugador.")] // Explica en el Inspector para qué sirve la variable
    [SerializeField] private int damageAmount = 10; // Almacena el daño que se aplicará al jugador (10 por defecto)

    [Tooltip("¿Daño por segundo? Si está activo, el jugador recibirá daño continuamente mientras permanezca dentro.")] // Descripción en el Inspector
    [SerializeField] private bool damageOverTime = false; // Define si el daño se aplica una sola vez o repetitivamente

    [Tooltip("Intervalo en segundos entre cada tick de daño (solo si 'damageOverTime' está activo).")] // Descripción en el Inspector
    [SerializeField] private float damageInterval = 1f; // Determina la velocidad a la que el jugador recibe daño continuo

    private float timer = 0f; // Un temporizador interno para llevar la cuenta del tiempo transcurrido
    private bool isPlayerInside = false; // Variable para saber si el jugador está actualmente dentro de la zona de daño

    private void Update() // Método de Unity que se ejecuta en cada fotograma del juego
    {
        if (damageOverTime && isPlayerInside) // Si el daño es continuo y el jugador está dentro de la zona
        {
            timer += Time.deltaTime; // Sumamos el tiempo que ha pasado desde el último fotograma al temporizador
            if (timer >= damageInterval) // Si el tiempo acumulado supera o iguala el intervalo definido
            {
                ApplyDamage(); // Llamamos al método que le quita vida al jugador
                timer = 0f; // Reiniciamos el temporizador para volver a contar el tiempo
            }
        }
    }

    private void OnTriggerEnter(Collider other) // Detecta cuando un objeto entra en la zona invisible (Trigger)
    {
        if (IsPlayer(other.gameObject)) // Usamos nuestro método auxiliar para verificar si entró el jugador
        {
            isPlayerInside = true; // Confirmamos que el jugador está adentro
            timer = damageInterval; // Forzamos que el daño continuo se aplique de inmediato al entrar
            
            if (!damageOverTime) // Si el daño NO es continuo (es decir, es daño de un solo golpe)
            {
                ApplyDamage(); // Aplicamos el daño de inmediato
            }
        }
    }

    private void OnTriggerExit(Collider other) // Detecta cuando un objeto sale de la zona invisible (Trigger)
    {
        if (IsPlayer(other.gameObject)) // Si el objeto que salió es el jugador
        {
            isPlayerInside = false; // Indicamos que el jugador ya no está adentro para detener el daño continuo
        }
    }

    private void OnCollisionEnter(Collision collision) // Detecta colisiones físicas directas (como chocar contra un muro con daño)
    {
        if (IsPlayer(collision.gameObject)) // Si chocamos físicamente contra el jugador
        {
            ApplyDamage(); // Aplicamos daño al instante
        }
    }

    private bool IsPlayer(GameObject obj) // Método auxiliar creado para no repetir código. Verifica si un objeto es el jugador
    {
        // Retorna verdadero (true) si el objeto tiene la etiqueta "Player" o tiene el componente "RE_PlayerMovement"
        return obj.CompareTag("Player") || obj.GetComponent<RE_PlayerMovement>() != null;
    }

    private void ApplyDamage() // Método responsable de ordenar que se reste vida al jugador
    {
        if (RE_PlayerHealth.Instance != null) // Nos aseguramos de que exista el script principal de salud del jugador en la escena
        {
            RE_PlayerHealth.Instance.TakeDamage(damageAmount); // Le indicamos al script de salud que reste la cantidad de daño especificada
        }
        else // Si no hay sistema de salud (por ejemplo, si el jugador murió o no se cargó bien)
        {
            Debug.LogWarning("[RE_DamageZone] Intento de infligir daño pero no se encontró RE_PlayerHealth en la escena."); // Mostramos una advertencia
        }
    }
}
