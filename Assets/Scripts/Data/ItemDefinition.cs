using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Data asset for an item type.
    /// Create via Assets → Create → EverRealm → Item Definition.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Item Definition", fileName = "Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Machine-readable key (e.g. 'sword_iron'). Stable across renames.")]
        public string ItemId;
        public string DisplayName;
        public Sprite Icon;
        public ItemRarity Rarity = ItemRarity.Common;

        [Header("Stacking")]
        public bool Stackable = true;
        [Tooltip("Max per stack. Ignored if not stackable.")]
        public int MaxStack = 99;

        [Header("Economy")]
        public float Weight = 1f;
        [Tooltip("Base gold value for extraction scoring.")]
        public int Value = 1;
    }
}
