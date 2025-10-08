using MothHunt.Inventory;

namespace MothHunt.Interaction
{
    public interface IInteractable
    {
        bool Interact(PlayerInventory inventory);
        string Prompt { get; } // not used now, but handy later
    }
}
