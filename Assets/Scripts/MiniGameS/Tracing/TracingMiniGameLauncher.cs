using System;
using UnityEngine;

namespace Overwork.MiniGames.Tracing
{
    public sealed class TracingMiniGameLauncher : MonoBehaviour, IPlayerMiniGameLauncher
    {
        [SerializeField] private TracingPathDatabase pathDatabase;
        [SerializeField] private GameObject miniGamePrefab;
        public TaskKind Kind => TaskKind.Tracing;
        public bool IsReady => pathDatabase != null && miniGamePrefab != null;
        public bool TryStart(GameObject host, int level, float timeLimit, Action<bool,string> onCompleted)
        {
            if (host == null || pathDatabase == null || miniGamePrefab == null) return false;
            for (var i=host.transform.childCount-1;i>=0;i--) Destroy(host.transform.GetChild(i).gameObject);
            var root=Instantiate(miniGamePrefab,host.transform);
            var rect=root.GetComponent<RectTransform>(); if(rect==null){Destroy(root);return false;} rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var game=root.GetComponent<TracingMiniGame>() ?? root.AddComponent<TracingMiniGame>(); game.Configure(pathDatabase); game.OnCompleted += (success,reason)=>{onCompleted?.Invoke(success,reason);if(root!=null)Destroy(root);}; game.Initialize(level,timeLimit); return true;
        }
    }
}
