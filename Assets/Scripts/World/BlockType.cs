namespace EverRealm.Exiles.World
{
    /// <summary>
    /// All voxel block types. Air = 0 so default arrays are air-filled.
    /// Keep contiguous and ordered — used as atlas row indices.
    /// </summary>
    public enum BlockType : byte
    {
        Air      = 0,
        Grass    = 1,
        Dirt     = 2,
        Stone    = 3,
        Sand     = 4,
        CoalOre  = 5,
        IronOre  = 6,
        GoldOre  = 7,
        Chest              = 8,
        ExtractionCore     = 9,
        EnemyCampCore      = 10,
        TreasureCacheCore  = 11,
        Wood               = 12,
        Leaves             = 13,
        Snow               = 14,
    }
}
