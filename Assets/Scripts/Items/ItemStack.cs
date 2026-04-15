using EverRealm.Exiles.Data;

namespace EverRealm.Exiles.Items
{
    /// <summary>
    /// A slot in an inventory: item definition + quantity.
    /// </summary>
    [System.Serializable]
    public struct ItemStack
    {
        public ItemDefinition Definition;
        public int Count;

        public ItemStack(ItemDefinition definition, int count)
        {
            Definition = definition;
            Count = count;
        }

        public bool IsEmpty => Definition == null || Count <= 0;
    }
}
