namespace EverRealm.Exiles.Items
{
    /// <summary>
    /// Implemented by anything the player can interact with (loot pickups, chests, etc.).
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Display text for future UI prompt (e.g. "Pick up Sword").</summary>
        string InteractPrompt { get; }

        void Interact(Player.PlayerController player);
    }
}
