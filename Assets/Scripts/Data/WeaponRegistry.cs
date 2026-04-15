using System.Collections.Generic;
using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Central registry of all weapon definitions. Provides O(1) lookup by WeaponId.
    /// Create via Assets > Create > EverRealm > Weapon Registry.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Weapon Registry", fileName = "WeaponRegistry")]
    public sealed class WeaponRegistry : ScriptableObject
    {
        [SerializeField] private WeaponDefinition[] _weapons;

        private Dictionary<string, WeaponDefinition> _lookup;

        public IReadOnlyList<WeaponDefinition> All => _weapons;

        /// <summary>Build the lookup dictionary. Called automatically on first query.</summary>
        public void Initialize()
        {
            _lookup = new Dictionary<string, WeaponDefinition>();
            if (_weapons == null) return;

            foreach (var weapon in _weapons)
            {
                if (weapon == null || string.IsNullOrEmpty(weapon.WeaponId)) continue;

                if (!_lookup.TryAdd(weapon.WeaponId, weapon))
                    Debug.LogWarning($"[WeaponRegistry] Duplicate WeaponId '{weapon.WeaponId}' — skipping.");
            }
        }

        /// <summary>
        /// Look up a weapon by its stable WeaponId. Returns null with a warning if not found.
        /// </summary>
        public WeaponDefinition GetById(string weaponId)
        {
            if (_lookup == null) Initialize();

            if (string.IsNullOrEmpty(weaponId)) return null;

            if (_lookup.TryGetValue(weaponId, out var def))
                return def;

            Debug.LogWarning($"[WeaponRegistry] No weapon found with id '{weaponId}'.");
            return null;
        }
    }
}
