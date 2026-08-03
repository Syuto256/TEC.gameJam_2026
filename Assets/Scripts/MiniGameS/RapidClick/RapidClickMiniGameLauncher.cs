using System;
using UnityEngine;
namespace Overwork.MiniGames.RapidClick
{
    public sealed class RapidClickMiniGameLauncher : MonoBehaviour, IPlayerMiniGameLauncher
    {
        [SerializeField] private GameObject miniGamePrefab;
        public TaskKind Kind => TaskKind.RapidClick;
        public bool IsReady => miniGamePrefab != null;
        public bool TryStart(GameObject host,int level,float timeLimit,Action<bool,string> completed)
        {
            if(host==null || miniGamePrefab==null)return false; for(var i=host.transform.childCount-1;i>=0;i--)Destroy(host.transform.GetChild(i).gameObject);
            var root=Instantiate(miniGamePrefab,host.transform);var rect=root.GetComponent<RectTransform>();if(rect==null){Destroy(root);return false;}rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            var game=root.GetComponent<RapidClickMiniGame>() ?? root.AddComponent<RapidClickMiniGame>();game.OnCompleted+=(success,reason)=>{completed?.Invoke(success,reason);if(root!=null)Destroy(root);};game.Initialize(level,timeLimit);return true;
        }
    }
}
