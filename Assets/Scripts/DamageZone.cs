using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [Header("Configuración del Daño")]
    [Tooltip("Cantidad de daño infligido al jugador.")]
    [SerializeField] private int damageAmount = 10;

    [Tooltip("¿Daño por segundo? Si está activo, el jugador recibirá daño continuamente mientras permanezca dentro.")]
    [SerializeField] private bool damageOverTime = false;

    [Tooltip("Intervalo en segundos entre cada tick de daño (solo si 'damageOverTime' está activo).")]
    [SerializeField] private float damageInterval = 1f;

    private float timer = 0f;
    private bool isPlayerInside = false;

    private void Update()
    {
        if (damageOverTime && isPlayerInside)
        {
            timer += Time.deltaTime;
            if (timer >= damageInterval)
            {
                ApplyDamage();
                timer = 0f;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Comprobar si el objeto es el jugador (por Tag o por script de movimiento)
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInside = true;
            timer = damageInterval; // Para hacer daño instantáneo al entrar si está en modo "over time"
            
            if (!damageOverTime)
            {
                ApplyDamage();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null)
        {
            isPlayerInside = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Soporte para colisionadores físicos normales (que no son Triggers)
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.GetComponent<PlayerMovement>() != null)
        {
            ApplyDamage();
        }
    }

    private void ApplyDamage()
    {
        if (PlayerHealth.Instance != null)
        {
            PlayerHealth.Instance.TakeDamage(damageAmount);
        }
        else
        {
            Debug.LogWarning("[DamageZone] Intento de infligir daño pero no hay ninguna instancia de PlayerHealth en la escena.");
        }
    }
}
