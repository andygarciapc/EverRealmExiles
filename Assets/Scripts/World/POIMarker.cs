namespace EverRealm.Exiles.World
{
    /// <summary>
    /// Types of Points of Interest placed during world generation.
    /// </summary>
    public enum POIType : byte
    {
        EnemyCamp,
        TreasureCache,
        ExtractionZone,
        DungeonEntrance // stub for future phases
    }

    /// <summary>
    /// Lightweight marker placed during world generation (background thread).
    /// Carried on the <see cref="Chunk"/> and consumed by WorldManager on the
    /// main thread to spawn GameObjects and register block entities.
    /// </summary>
    public readonly struct POIMarker
    {
        public readonly POIType Type;
        public readonly int WorldX;
        public readonly int WorldZ;
        public readonly int SurfaceY;
        public readonly RiskZone Zone;

        public POIMarker(POIType type, int worldX, int worldZ, int surfaceY, RiskZone zone)
        {
            Type = type;
            WorldX = worldX;
            WorldZ = worldZ;
            SurfaceY = surfaceY;
            Zone = zone;
        }
    }
}
