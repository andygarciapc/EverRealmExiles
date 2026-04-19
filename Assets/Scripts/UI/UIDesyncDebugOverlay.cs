#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using EverRealm.Exiles.Core;
using EverRealm.Exiles.Items;

namespace EverRealm.Exiles.UI
{
    /// <summary>
    /// Dev-only IMGUI overlay. Exposes live counts for registered
    /// <see cref="InventoryPresenter"/> and <see cref="StashPresenter"/>
    /// instances alongside their backing <see cref="Inventory"/> / stash data,
    /// so UI desyncs are visible the moment they happen. Toggle with F10.
    /// Compiled out of release builds via the wrapping preprocessor guard.
    /// </summary>
    public sealed class UIDesyncDebugOverlay : MonoBehaviour
    {
        private const int LogRingSize = 16;
        private const int VisibleLogLines = 8;

        private static UIDesyncDebugOverlay _instance;

        private readonly List<InventoryBinding> _inventoryBindings = new();
        private readonly List<StashBinding> _stashBindings = new();
        private readonly Queue<string> _log = new();
        private bool _visible;

        // -----------------------------------------------------------------

        /// <summary>Return the process-wide overlay, creating it on first call.</summary>
        public static UIDesyncDebugOverlay EnsureInstance()
        {
            if (_instance != null) return _instance;

            var go = new GameObject("UIDesyncDebugOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UIDesyncDebugOverlay>();
            return _instance;
        }

        // -----------------------------------------------------------------
        // Registration

        public void RegisterInventoryPresenter(InventoryPresenter presenter, Inventory source)
        {
            if (presenter == null || source == null) return;

            var binding = new InventoryBinding
            {
                Presenter = presenter,
                Source = source,
                LastRefreshAt = Time.unscaledTime
            };
            binding.Handler = () => OnInventoryRefreshed(binding);
            presenter.OnRefreshed += binding.Handler;
            _inventoryBindings.Add(binding);
        }

        public void UnregisterInventoryPresenter(InventoryPresenter presenter)
        {
            if (presenter == null) return;

            for (int i = _inventoryBindings.Count - 1; i >= 0; i--)
            {
                var b = _inventoryBindings[i];
                if (!ReferenceEquals(b.Presenter, presenter)) continue;

                b.Presenter.OnRefreshed -= b.Handler;
                _inventoryBindings.RemoveAt(i);
            }
        }

        public void RegisterStashPresenter(StashPresenter presenter, StashManager source)
        {
            if (presenter == null || source == null) return;

            var binding = new StashBinding
            {
                Presenter = presenter,
                Source = source,
                LastRefreshAt = Time.unscaledTime
            };
            binding.Handler = () => OnStashRefreshed(binding);
            presenter.OnRefreshed += binding.Handler;
            _stashBindings.Add(binding);
        }

        public void UnregisterStashPresenter(StashPresenter presenter)
        {
            if (presenter == null) return;

            for (int i = _stashBindings.Count - 1; i >= 0; i--)
            {
                var b = _stashBindings[i];
                if (!ReferenceEquals(b.Presenter, presenter)) continue;

                b.Presenter.OnRefreshed -= b.Handler;
                _stashBindings.RemoveAt(i);
            }
        }

        // -----------------------------------------------------------------
        // Refresh callbacks — stamp time and run mismatch check

        private void OnInventoryRefreshed(InventoryBinding b)
        {
            b.LastRefreshAt = Time.unscaledTime;

            int actual = CountNonEmpty(b.Source);
            int view = b.Presenter.OccupiedSlots;
            if (actual != view)
                PushLog($"[INV] src={actual} view={view}");
        }

        private void OnStashRefreshed(StashBinding b)
        {
            b.LastRefreshAt = Time.unscaledTime;

            int actual = CountNonEmpty(b.Source.Stash);
            int view = b.Presenter.OccupiedSlots;
            if (actual != view)
                PushLog($"[STASH] src={actual} view={view}");
        }

        private static int CountNonEmpty(Inventory inv)
        {
            if (inv == null) return 0;

            int n = 0;
            foreach (var slot in inv.Slots)
                if (!slot.IsEmpty) n++;
            return n;
        }

        private void PushLog(string entry)
        {
            _log.Enqueue($"{Time.unscaledTime:F1}s  {entry}");
            while (_log.Count > LogRingSize) _log.Dequeue();
        }

        // -----------------------------------------------------------------
        // Input + render

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.f10Key.wasPressedThisFrame)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            const float w = 360f;
            const float h = 260f;
            const float lh = 16f;

            var rect = new Rect(10, 10, w, h);
            GUI.Box(rect, "UI Desync — [F10] close");

            float x = 20;
            float y = 32;
            float lineW = w - 20;

            for (int i = 0; i < _inventoryBindings.Count; i++)
            {
                var b = _inventoryBindings[i];
                int actual = CountNonEmpty(b.Source);
                int view = b.Presenter.OccupiedSlots;
                float dt = Time.unscaledTime - b.LastRefreshAt;

                GUI.Label(new Rect(x, y, lineW, lh),
                    $"Inv #{i}: src={actual}  view={view}  Δ{dt:F1}s");
                y += lh;
            }

            for (int i = 0; i < _stashBindings.Count; i++)
            {
                var b = _stashBindings[i];
                int actual = CountNonEmpty(b.Source.Stash);
                int view = b.Presenter.OccupiedSlots;
                float dt = Time.unscaledTime - b.LastRefreshAt;

                int equipped = 0;
                foreach (var kvp in b.Source.Loadout.Equipment)
                    if (!kvp.Value.IsEmpty) equipped++;
                int backpack = CountNonEmpty(b.Source.Loadout.Backpack);

                GUI.Label(new Rect(x, y, lineW, lh),
                    $"Stash #{i}: src={actual}  view={view}  Δ{dt:F1}s");
                y += lh;
                GUI.Label(new Rect(x, y, lineW, lh),
                    $"  Loadout: equipped={equipped}  backpack={backpack}");
                y += lh;
            }

            if (_inventoryBindings.Count == 0 && _stashBindings.Count == 0)
            {
                GUI.Label(new Rect(x, y, lineW, lh), "(no presenters registered)");
                y += lh;
            }

            y += 8;
            GUI.Label(new Rect(x, y, lineW, lh), "Recent mismatches:");
            y += lh;

            int shown = 0;
            var entries = _log.ToArray();
            for (int i = entries.Length - 1; i >= 0 && shown < VisibleLogLines; i--, shown++)
            {
                GUI.Label(new Rect(x, y, lineW, lh), entries[i]);
                y += lh;
            }

            if (shown == 0)
                GUI.Label(new Rect(x, y, lineW, lh), "  (none)");
        }

        private void OnDestroy()
        {
            foreach (var b in _inventoryBindings)
                if (b.Presenter != null) b.Presenter.OnRefreshed -= b.Handler;
            foreach (var b in _stashBindings)
                if (b.Presenter != null) b.Presenter.OnRefreshed -= b.Handler;
            _inventoryBindings.Clear();
            _stashBindings.Clear();

            if (_instance == this) _instance = null;
        }

        // -----------------------------------------------------------------

        private class InventoryBinding
        {
            public InventoryPresenter Presenter;
            public Inventory Source;
            public float LastRefreshAt;
            public Action Handler;
        }

        private class StashBinding
        {
            public StashPresenter Presenter;
            public StashManager Source;
            public float LastRefreshAt;
            public Action Handler;
        }
    }
}
#endif
