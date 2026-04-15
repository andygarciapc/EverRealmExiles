using System.Collections.Generic;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.Extraction
{
    /// <summary>
    /// Immutable record of a completed extraction run.
    /// Built by <see cref="Core.RunManager"/>, consumed by <see cref="UI.RunSummaryUI"/>.
    /// </summary>
    public sealed class RunResult
    {
        public bool Success { get; }
        public float ElapsedTime { get; }
        public int KillCount { get; }

        /// <summary>
        /// Snapshot of the player's inventory at run end.
        /// On success these items are kept; on failure they are lost.
        /// </summary>
        public IReadOnlyList<ItemStack> Items { get; }

        public RunResult(bool success, float elapsedTime, int killCount,
                         IReadOnlyList<ItemStack> items)
        {
            Success = success;
            ElapsedTime = elapsedTime;
            KillCount = killCount;
            Items = items;
        }
    }
}
