using UnityEngine;

namespace EverRealm.Exiles.Data
{
    /// <summary>
    /// Weighted loot table. Roll() returns a random item based on entry weights.
    /// Create via Assets → Create → EverRealm → Loot Table.
    /// </summary>
    [CreateAssetMenu(menuName = "EverRealm/Loot Table", fileName = "LootTable")]
    public sealed class LootTable : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public ItemDefinition Item;
            [Range(1, 100)] public int Weight;
            [Range(1, 10)]  public int MinCount;
            [Range(1, 10)]  public int MaxCount;
        }

        [SerializeField] private Entry[] _entries;

        private int _totalWeight;

        /// <summary>
        /// Weighted random selection. Returns null if the table is empty.
        /// </summary>
        public ItemDefinition Roll(out int count)
        {
            count = 0;
            if (_entries == null || _entries.Length == 0) return null;

            EnsureTotalWeight();

            int roll = Random.Range(0, _totalWeight);
            int cumulative = 0;

            for (int i = 0; i < _entries.Length; i++)
            {
                cumulative += _entries[i].Weight;
                if (roll < cumulative)
                {
                    count = Random.Range(_entries[i].MinCount, _entries[i].MaxCount + 1);
                    return _entries[i].Item;
                }
            }

            // Fallback (shouldn't happen with correct weights).
            var last = _entries[_entries.Length - 1];
            count = Random.Range(last.MinCount, last.MaxCount + 1);
            return last.Item;
        }

        private void EnsureTotalWeight()
        {
            if (_totalWeight > 0) return;
            ComputeTotalWeight();
        }

        private void ComputeTotalWeight()
        {
            _totalWeight = 0;
            if (_entries == null) return;
            for (int i = 0; i < _entries.Length; i++)
                _totalWeight += _entries[i].Weight;
        }

        private void OnEnable()  => ComputeTotalWeight();
        private void OnValidate() => ComputeTotalWeight();
    }
}
