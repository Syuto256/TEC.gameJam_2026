using System;
using UnityEngine;

namespace Overwork.MiniGames.Typing
{
    /// <summary>Core の起動契約をタイピングミニゲームに接続するアダプター。</summary>
    public sealed class TypingMiniGameLauncher : MonoBehaviour, IPlayerMiniGameLauncher
    {
        [SerializeField] private TypingQuestionDatabase questionDatabase;

        public TaskKind Kind => TaskKind.Typing;
        public bool IsReady => questionDatabase != null;

        public bool TryStart(GameObject host, int level, float timeLimit, Action<bool, string> onCompleted)
        {
            if (host == null || questionDatabase == null)
            {
                return false;
            }

            for (var i = host.transform.childCount - 1; i >= 0; i--)
            {
                Destroy(host.transform.GetChild(i).gameObject);
            }

            var root = new GameObject("TypingMiniGame", typeof(RectTransform));
            root.transform.SetParent(host.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var miniGame = root.AddComponent<TypingMiniGame>();
            miniGame.Configure(questionDatabase);
            miniGame.OnCompleted += (success, reason) =>
            {
                onCompleted?.Invoke(success, reason);
                if (root != null)
                {
                    Destroy(root);
                }
            };
            miniGame.Initialize(level, timeLimit);
            return true;
        }
    }
}
