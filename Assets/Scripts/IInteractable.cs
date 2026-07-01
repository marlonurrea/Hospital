public interface IInteractable // Interfaz base que deben tener todos los objetos con los que el jugador puede interactuar (puertas, NPCs, objetos)
{
    // Devuelve el texto que se mostrará en la pantalla cuando el jugador mire este objeto (ejemplo: "Presiona E para abrir")
    string GetInteractPrompt();

    // Método principal que contiene la acción o lógica que ocurrirá cuando el jugador pulse el botón de interactuar
    void Interact();
}
