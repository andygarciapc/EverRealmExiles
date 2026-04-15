using UnityEngine;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.World;

namespace EverRealm.Exiles.Extraction
{
    /// <summary>
    /// Block entity for an extraction core block. Inactive by default.
    /// When the player interacts, spawns an <see cref="ExtractionZone"/>
    /// that manages the countdown and proximity check (Arc Raiders style).
    /// </summary>
    public sealed class ExtractionBlockEntity : IBlockEntity
    {
        public Vector3Int Position { get; }

        private bool _activated;

        public string InteractPrompt => _activated ? "Extraction active" : "Activate Extraction";

        public ExtractionBlockEntity(Vector3Int position)
        {
            Position = position;
        }

        public void OnInteract(Player.PlayerController player)
        {
            if (_activated) return;

            // Check if another extraction zone is already active nearby
            // (multiple ExtractionCore blocks form one platform).
            var existing = Object.FindObjectOfType<ExtractionZone>();
            if (existing != null)
            {
                float dist = Vector3.Distance(
                    existing.transform.position,
                    new Vector3(Position.x + 0.5f, Position.y + 1f, Position.z + 0.5f));
                if (dist < 10f)
                {
                    _activated = true;
                    return;
                }
            }

            _activated = true;

            // Spawn a runtime zone that handles the timer and proximity.
            var go = new GameObject("ExtractionZone");
            go.transform.position = new Vector3(Position.x + 0.5f, Position.y + 1f, Position.z + 0.5f);
            var zone = go.AddComponent<ExtractionZone>();
            zone.Init(this);

            Debug.Log($"[Extraction] Activated at {Position}");
        }

        public void OnRemoved()
        {
            // Chunk unloaded — zone will self-destruct via its own check.
        }
    }
}
