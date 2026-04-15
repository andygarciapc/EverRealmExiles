using UnityEngine;
using EverRealm.Exiles.Core;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Lives in the MainMenu scene. On start, instantiates the hub UI
    /// and populates it from the persistent <see cref="StashManager"/>.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject _hubUiPrefab;

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

            if (_hubUiPrefab == null)
            {
                Debug.LogError("[MainMenuController] _hubUiPrefab is not assigned.");
                return;
            }

            // Destroy any leftover HideoutUI before instantiating a fresh one.
            var existing = FindAnyObjectByType<HideoutUI>();
            if (existing != null)
                Destroy(existing.gameObject);

            var go = Instantiate(_hubUiPrefab);
            var ui = go.GetComponent<HideoutUI>();
            if (ui == null)
            {
                Debug.LogError("[MainMenuController] HideoutUI component not found on prefab.");
                return;
            }

            var stash = GameBootstrap.Instance.Stash;
            if (stash == null)
                Debug.LogWarning("[MainMenuController] StashManager is null — hub will show without save data.");

            ui.Show(stash);
        }
    }
}
