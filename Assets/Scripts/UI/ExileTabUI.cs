using UnityEngine;
using TMPro;
using EverRealm.Exiles.Core;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Character customization panel shown in the Exile tab.
    /// Displays character info and lifetime stats. Equipment and
    /// cosmetics will expand here in later phases.
    /// </summary>
    public sealed class ExileTabUI : MonoBehaviour
    {
        [Header("Character Info")]
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _titleText;

        [Header("Lifetime Stats")]
        [SerializeField] private TMP_Text _statsText;

        [Header("Hint")]
        [SerializeField] private TMP_Text _inventoryHintText;

        /// <summary>Populate the character panel from save data.</summary>
        public void Show(StashManager stash)
        {
            gameObject.SetActive(true);

            if (stash == null) return;

            if (_nameText != null)
                _nameText.text = stash.PlayerName;

            if (_levelText != null)
                _levelText.text = $"Level {stash.PlayerLevel}";

            if (_titleText != null)
                _titleText.text = "Survivor"; // placeholder title

            if (_statsText != null)
            {
                var s = stash.Stats;
                int minutes = Mathf.FloorToInt(s.TotalPlayTime / 60f);
                _statsText.text =
                    $"Runs: {s.TotalRuns}\n" +
                    $"Extractions: {s.TotalExtractions}\n" +
                    $"Kills: {s.TotalKills}\n" +
                    $"Time Survived: {minutes}m\n" +
                    $"Currency: {s.Currency}";
            }

            if (_inventoryHintText != null)
                _inventoryHintText.text = "Press [Tab] to manage stash & loadout";
        }
    }
}
