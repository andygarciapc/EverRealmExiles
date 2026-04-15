using UnityEngine;

namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Extra data/behavior for a special block (chest, furnace, etc.).
    /// Most blocks are just a BlockType — only blocks that need state get an entity.
    /// Analogous to Minecraft's TileEntity / BlockEntity system.
    /// </summary>
    public interface IBlockEntity
    {
        /// <summary>World-space block position this entity belongs to.</summary>
        Vector3Int Position { get; }

        /// <summary>Prompt shown to the player when aiming at this block.</summary>
        string InteractPrompt { get; }

        /// <summary>Called when the player interacts with this block.</summary>
        void OnInteract(Player.PlayerController player);

        /// <summary>Called when the block is destroyed or the chunk unloads.</summary>
        void OnRemoved();
    }
}
