public interface IInteractable
{
    // Devuelve el texto que se le mostrará al jugador (ej: "Abrir puerta", "Hablar con NPC")
    string GetInteractPrompt();

    // Método que se ejecuta al interactuar
    void Interact();
}
