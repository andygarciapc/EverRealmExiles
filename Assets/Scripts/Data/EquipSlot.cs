namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Which equipment slot an item occupies on the player's loadout.
    /// <c>None</c> means the item is not equippable (goes to backpack or stash only).
    /// </summary>
    public enum EquipSlot
    {
        None,
        Head,
        Chest,
        Legs,
        PrimaryWeapon,
        SecondaryWeapon
    }
}
