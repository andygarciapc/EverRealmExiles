using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using EverRealm.Exiles.Extraction;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Full-screen overlay shown at the end of a run.
    /// Displays success/failure, stats, total loot value, and item list.
    /// </summary>
    public sealed class RunSummaryUI : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _subtitleText;

        [Header("Stats")]
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private TMP_Text _killsText;
        [SerializeField] private TMP_Text _totalValueText;

        [Header("Items")]
        [SerializeField] private Transform _itemListParent;
        [SerializeField] private GameObject _itemRowPrefab;
        [SerializeField] private TMP_Text _noItemsText;

        [Header("Actions")]
        [SerializeField] private Button _continueButton;

        private void Awake()
        {
            // Ensure an EventSystem exists for future UI interactions.
            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
        }

        /// <summary>
        /// Populate the summary overlay with run result data.
        /// </summary>
        public void Show(RunResult result)
        {
            gameObject.SetActive(true);

            // --- Header ---
            if (_titleText != null)
            {
                _titleText.text = result.Success ? "EXTRACTED" : "KILLED IN ACTION";
                _titleText.color = result.Success
                    ? new Color(0.2f, 0.8f, 0.2f)   // green
                    : new Color(0.9f, 0.2f, 0.2f);   // red
            }

            if (_subtitleText != null)
            {
                _subtitleText.text = result.Success
                    ? "You made it out alive."
                    : "Your loot has been lost.";
            }

            // --- Stats ---
            if (_timeText != null)
            {
                int minutes = Mathf.FloorToInt(result.ElapsedTime / 60f);
                int seconds = Mathf.FloorToInt(result.ElapsedTime % 60f);
                _timeText.text = $"{minutes:00}:{seconds:00}";
            }

            if (_killsText != null)
                _killsText.text = result.KillCount.ToString();

            // --- Continue button ---
            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);

            // --- Item list ---
            int totalValue = 0;

            if (result.Items != null && result.Items.Count > 0)
            {
                if (_noItemsText != null)
                {
                    _noItemsText.gameObject.SetActive(!result.Success);
                    _noItemsText.text = "All items lost";
                }

                if (_itemListParent != null && _itemRowPrefab != null)
                {
                    foreach (var stack in result.Items)
                    {
                        if (stack.IsEmpty) continue;

                        var viewData = ItemViewData.FromStack(stack);
                        totalValue += viewData.Value * viewData.Count;

                        var row = Instantiate(_itemRowPrefab, _itemListParent);
                        var rowUI = row.GetComponent<RunSummaryItemRow>();
                        if (rowUI != null)
                            rowUI.Populate(viewData);
                    }
                }
            }
            else
            {
                if (_noItemsText != null)
                {
                    _noItemsText.gameObject.SetActive(true);
                    _noItemsText.text = result.Success
                        ? "No items collected"
                        : "All items lost";
                }
            }

            // --- Total value ---
            if (_totalValueText != null)
            {
                _totalValueText.text = result.Success
                    ? $"Total Value: {totalValue}g"
                    : "Total Value: 0g";
            }
        }

        private void OnContinueClicked()
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
