using UnityEngine;
using EverRealm.Exiles.Core;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Lives in the MainMenu scene. On start, finds or instantiates the main menu UI
    /// and populates it from the persistent <see cref="StashManager"/>.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _mainMenuUiPrefab;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (GameBootstrap.Instance == null)
            {
                Debug.LogError("[MainMenuController] GameBootstrap.Instance is null — " +
                               "is the Bootstrap GameObject in this scene?");
                return;
            }

            GameBootstrap.Instance.SetState(GameState.Hideout);

            // First, try to find a MainMenuUI already in the scene (placed by editor setup).
            var ui = FindAnyObjectByType<MainMenuUI>();
            Debug.Log($"[MainMenuController] FindAnyObjectByType<MainMenuUI>() = {(ui != null ? ui.gameObject.name : "NULL")}");

            // If not found, instantiate from prefab.
            if (ui == null)
            {
                if (_mainMenuUiPrefab == null)
                {
                    Debug.LogError("[MainMenuController] _mainMenuUiPrefab is not assigned and no MainMenuUI exists in the scene.");
                    return;
                }

                var go = Instantiate(_mainMenuUiPrefab);
                ui = go.GetComponent<MainMenuUI>();
                if (ui == null)
                {
                    Debug.LogError("[MainMenuController] MainMenuUI component not found on prefab.");
                    return;
                }
            }

            var stash = GameBootstrap.Instance.Stash;
            if (stash == null)
                Debug.LogWarning("[MainMenuController] StashManager is null — menu will show without save data.");

            Debug.Log("[MainMenuController] Calling MainMenuUI.Show()");
            ui.Show(stash);
        }
    }
}
