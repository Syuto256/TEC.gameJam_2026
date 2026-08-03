using System;
using UnityEngine;

namespace Overwork.MiniGames.DragDrop
{
    public sealed class SortingMiniGameLauncher : MonoBehaviour, IPlayerMiniGameLauncher
    {
        [SerializeField] private GameObject miniGamePrefab;

        public TaskKind Kind => TaskKind.DragDrop;
        public bool IsReady => miniGamePrefab != null;

        public bool TryStart(GameObject host, int level, float timeLimit, Action<bool, string> onCompleted)
        {
            if (host == null || miniGamePrefab == null) return false;
            for (var i = host.transform.childCount - 1; i >= 0; i--) Destroy(host.transform.GetChild(i).gameObject);

            var root = Instantiate(miniGamePrefab, host.transform);
            var rect = root.GetComponent<RectTransform>();
            if (rect == null) { Destroy(root); return false; }
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var game = root.GetComponent<SortingMiniGame>() ?? root.AddComponent<SortingMiniGame>();
            game.OnCompleted += (success, reason) => { onCompleted?.Invoke(success, reason); if (root != null) Destroy(root); };
            game.Initialize(level, timeLimit);
            return true;
        }
    }
}
