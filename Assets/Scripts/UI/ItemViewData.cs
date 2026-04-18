using UnityEngine;
using EverRealm.Exiles.Data;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Immutable presentation data for a single inventory/stash slot.
    /// Decouples UI rendering from runtime inventory state so visual bugs
    /// cannot mutate underlying data.
    /// </summary>
    public readonly struct ItemViewData
    {
        public readonly bool IsEmpty;
        public readonly bool HasIcon;
        public readonly string DisplayName;
        public readonly Sprite Icon;
        public readonly int Count;
        public readonly string CountText;
        public readonly ItemRarity Rarity;
        public readonly string RarityName;
        public readonly Color RarityColor;
        public readonly ItemType Type;
        public readonly string TypeName;
        public readonly string Description;
        public readonly int Value;
        public readonly float Weight;
        public readonly string ItemId;
        public readonly EquipSlot EquipSlot;
        public readonly float DefenseValue;
        public readonly bool IsEquippable;
        public readonly bool IsWeapon;
        public readonly float WeaponDamage;
        public readonly float WeaponHeavyDamage;
        public readonly string WeaponSpeedTier;

        private ItemViewData(bool isEmpty, string displayName, Sprite icon, int count,
            ItemRarity rarity, ItemType type, string description,
            int value, float weight, string itemId,
            EquipSlot equipSlot, float defenseValue,
            bool isWeapon, float weaponDamage, float weaponHeavyDamage, string weaponSpeedTier)
        {
            IsEmpty = isEmpty;
            HasIcon = icon != null;
            DisplayName = displayName ?? "???";
            Icon = icon;
            Count = count;
            CountText = count > 1 ? count.ToString() : "";
            Rarity = rarity;
            RarityName = rarity.ToString();
            RarityColor = GetRarityColor(rarity);
            Type = type;
            TypeName = type.ToString();
            Description = description ?? "";
            Value = value;
            Weight = weight;
            ItemId = itemId ?? "";
            EquipSlot = equipSlot;
            DefenseValue = defenseValue;
            IsEquippable = equipSlot != EquipSlot.None;
            IsWeapon = isWeapon;
            WeaponDamage = weaponDamage;
            WeaponHeavyDamage = weaponHeavyDamage;
            WeaponSpeedTier = weaponSpeedTier ?? "";
        }

        /// <summary>Create view data from an ItemStack. Null-safe.</summary>
        public static ItemViewData FromStack(ItemStack stack)
        {
            if (stack.IsEmpty || stack.Definition == null)
                return Invalid;

            var def = stack.Definition;
            bool isWeapon = def.LinkedWeapon != null;
            float lightDmg = isWeapon ? def.LinkedWeapon.LightDamage : 0f;
            float heavyDmg = isWeapon ? def.LinkedWeapon.HeavyDamage : 0f;
            string speedTier = isWeapon ? ComputeSpeedTier(def.LinkedWeapon) : "";

            return new ItemViewData(
                false,
                def.DisplayName,
                def.Icon,
                stack.Count,
                def.Rarity,
                def.Type,
                def.Description,
                def.Value,
                def.Weight,
                def.ItemId,
                def.EquipSlot,
                def.DefenseValue,
                isWeapon,
                lightDmg,
                heavyDmg,
                speedTier
            );
        }

        /// <summary>Empty slot representation.</summary>
        public static readonly ItemViewData Empty = new(
            true, "", null, 0,
            ItemRarity.Common, ItemType.Misc, "",
            0, 0f, "",
            EquipSlot.None, 0f,
            false, 0f, 0f, ""
        );

        /// <summary>Fallback for missing or invalid item data.</summary>
        public static readonly ItemViewData Invalid = new(
            false, "???", null, 1,
            ItemRarity.Common, ItemType.Misc, "Missing item data",
            0, 0f, "invalid",
            EquipSlot.None, 0f,
            false, 0f, 0f, ""
        );

        /// <summary>Compute a qualitative speed tier from light attack timing.</summary>
        private static string ComputeSpeedTier(Data.WeaponDefinition w)
        {
            float total = w.LightWindup + w.LightActive + w.LightRecovery;
            if (total <= 0.4f) return "Fast";
            if (total <= 0.7f) return "Medium";
            return "Slow";
        }

        /// <summary>Canonical rarity → colour mapping used by all UI.</summary>
        public static Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.6f, 0.6f, 0.6f, 0.8f),
                ItemRarity.Rare   => new Color(0.2f, 0.5f, 1f, 0.9f),
                ItemRarity.Epic   => new Color(0.7f, 0.3f, 0.9f, 0.9f),
                _                 => Color.white
            };
        }
    }
}
