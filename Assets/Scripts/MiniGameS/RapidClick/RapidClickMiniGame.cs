using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Overwork.MiniGames.RapidClick
{
    public sealed class RapidClickMiniGame : MiniGameBase, IPointerClickHandler
    {
        private int requiredClicks;
        private int clicks;
        private TextMeshProUGUI text;
        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            requiredClicks = 8 + Mathf.Clamp(difficulty, 1, 4) * 4;
            gameObject.AddComponent<Image>().color = new Color(.15f,.2f,.32f,.98f);
            var child = new GameObject("Status", typeof(RectTransform), typeof(TextMeshProUGUI)); child.transform.SetParent(transform, false);
            var rect=child.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=new Vector2(16,16);rect.offsetMax=new Vector2(-16,-16);
            text=child.GetComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.fontSize=38;text.alignment=TextAlignmentOptions.Center;text.color=Color.white; Refresh();
        }
        public void OnPointerClick(PointerEventData eventData) { if (IsPlaying) { clicks++; if (clicks>=requiredClicks) FinishGame(true,"COMPLETE"); else Refresh(); } }
        protected override void OnUpdate(float deltaTime) => Refresh();
        private void Refresh() { if(text!=null) text.text="CLICK!\n"+clicks+" / "+requiredClicks+"\nTIME "+Mathf.CeilToInt(TimeRemaining).ToString("00"); }
    }
}
