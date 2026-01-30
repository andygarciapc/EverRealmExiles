using UnityEngine;

namespace EverRealmExiles.Voxel
{
    public enum BlockType : byte
    {
        Air = 0,
        Grass = 1,
        Dirt = 2,
        Stone = 3,
        Sand = 4,
        Water = 5,
        Wood = 6,
        Leaves = 7,
        Cobblestone = 8,
        Bedrock = 9,
        Gravel = 10,
        Coal = 11,
        Iron = 12,
        Gold = 13,
        Diamond = 14,
        Snow = 15
    }

    [System.Serializable]
    public struct BlockData
    {
        public BlockType type;
        public bool isSolid;
        public bool isTransparent;
        public int textureTop;
        public int textureSide;
        public int textureBottom;

        public BlockData(BlockType type, bool isSolid, bool isTransparent, int textureTop, int textureSide, int textureBottom)
        {
            this.type = type;
            this.isSolid = isSolid;
            this.isTransparent = isTransparent;
            this.textureTop = textureTop;
            this.textureSide = textureSide;
            this.textureBottom = textureBottom;
        }
    }

    public static class BlockDatabase
    {
        private static readonly BlockData[] blocks = new BlockData[]
        {
            new BlockData(BlockType.Air, false, true, 0, 0, 0),
            new BlockData(BlockType.Grass, true, false, 0, 1, 2),      // top=grass, side=grass_side, bottom=dirt
            new BlockData(BlockType.Dirt, true, false, 2, 2, 2),
            new BlockData(BlockType.Stone, true, false, 3, 3, 3),
            new BlockData(BlockType.Sand, true, false, 4, 4, 4),
            new BlockData(BlockType.Water, false, true, 5, 5, 5),
            new BlockData(BlockType.Wood, true, false, 7, 6, 7),       // top=wood_top, side=wood_side, bottom=wood_top
            new BlockData(BlockType.Leaves, true, true, 8, 8, 8),
            new BlockData(BlockType.Cobblestone, true, false, 9, 9, 9),
            new BlockData(BlockType.Bedrock, true, false, 10, 10, 10),
            new BlockData(BlockType.Gravel, true, false, 11, 11, 11),
            new BlockData(BlockType.Coal, true, false, 12, 12, 12),
            new BlockData(BlockType.Iron, true, false, 13, 13, 13),
            new BlockData(BlockType.Gold, true, false, 14, 14, 14),
            new BlockData(BlockType.Diamond, true, false, 15, 15, 15),
            new BlockData(BlockType.Snow, true, false, 16, 16, 16)
        };

        public static BlockData GetBlockData(BlockType type)
        {
            int index = (int)type;
            if (index >= 0 && index < blocks.Length)
                return blocks[index];
            return blocks[0];
        }

        public static bool IsSolid(BlockType type)
        {
            return GetBlockData(type).isSolid;
        }

        public static bool IsTransparent(BlockType type)
        {
            return GetBlockData(type).isTransparent;
        }
    }
}
