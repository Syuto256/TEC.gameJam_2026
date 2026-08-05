using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.TimingStop
{
    /// <summary>往復するマーカーを、当たりゾーンの上で止める目押しミニゲーム。</summary>
    /// <remarks>
    /// 配置・配色・文字は <c>Assets/Prefabs/MiniGames/TimingStopMiniGame.prefab</c> で調整する。
    /// マーカーとゾーンの位置はアンカーで指定するため、バーの幅を変えても判定はずれない。
    /// クリックを受けるにはルートに Raycast Target が有効な Graphic（Image など）が必要である。
    /// </remarks>
    public sealed class TimingStopMiniGame : MiniGameBase, IPointerDownHandler
    {
        [Header("【表示先】")]
        [Tooltip("当たりゾーン。バーの子に置く。位置と幅はコードが毎回決める。")]
        [SerializeField] private RectTransform targetZone;

        [Tooltip("往復するマーカー。バーの子に置く。横位置はコードが毎フレーム決める。")]
        [SerializeField] private RectTransform marker;

        [Tooltip("成功した回数。")]
        [SerializeField] private TMP_Text progressText;

        [Tooltip("操作の案内。画面下中央に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("ミス数。画面左下に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text missText;

        [Header("【難度の調整】")]
        [Tooltip("レベル 1 で必要な成功回数。")]
        [Min(1)] [SerializeField] private int baseRequiredStops = 3;

        [Tooltip("レベルが 1 上がるごとに増える成功回数。")]
        [Min(0)] [SerializeField] private int stopsPerLevel = 1;

        [Tooltip("レベル 1 でマーカーが 1 往復するのにかかる秒数。短いほど速い。")]
        [Min(0.2f)] [SerializeField] private float baseCycleSeconds = 1.6f;

        [Tooltip("レベルが 1 上がるごとに、往復時間へ掛ける倍率。1 未満で速くなる。")]
        [Range(0.4f, 1f)] [SerializeField] private float cycleScalePerLevel = 0.82f;

        [Tooltip("レベル 1 での当たりゾーンの幅（バー全体に対する割合）。")]
        [Range(0.02f, 0.6f)] [SerializeField] private float baseZoneWidth = 0.24f;

        [Tooltip("レベルが 1 上がるごとに、ゾーン幅へ掛ける倍率。1 未満で狭くなる。")]
        [Range(0.4f, 1f)] [SerializeField] private float zoneScalePerLevel = 0.8f;

        [Tooltip("何回外したら失敗にするか。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

        [Tooltip("スペースキーでも止められるようにする。")]
        [SerializeField] private bool acceptSpaceKey = true;

        [Header("【表示する文言】")]
        [SerializeField] private string prompt = "クリック または スペース で止める";
        [SerializeField] private string hitPrompt = "成功";
        [SerializeField] private string missedPrompt = "外れました";
        [SerializeField] private string progressFormat = "{0} / {1} 回";
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

        private float elapsed;
        private float cycleSeconds;
        private float zoneWidth;
        private float zoneCenter;
        private int requiredStops;
        private int stops;
        private int misses;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this, (nameof(targetZone), targetZone), (nameof(marker), marker)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            var level = Mathf.Clamp(difficulty, 1, 4);
            requiredStops = baseRequiredStops + (level - 1) * stopsPerLevel;
            cycleSeconds = Mathf.Max(0.2f, baseCycleSeconds * Mathf.Pow(cycleScalePerLevel, level - 1));
            zoneWidth = Mathf.Clamp(baseZoneWidth * Mathf.Pow(zoneScalePerLevel, level - 1), 0.02f, 1f);
            elapsed = 0f;
            stops = 0;
            misses = 0;

            PickZone();
            ApplyMarker();
            RefreshStatus(prompt);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            TryStop();
        }

        protected override void OnUpdate(float deltaTime)
        {
            elapsed += deltaTime;
            ApplyMarker();

            if (acceptSpaceKey && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TryStop();
            }
        }

        private void TryStop()
        {
            if (!IsPlaying)
            {
                return;
            }

            if (!TimingStopMath.IsHit(CurrentPosition(), zoneCenter, zoneWidth))
            {
                PlayInputFeedback(false);
                misses++;
                if (misses >= allowedMisses)
                {
                    FinishGame(false, "MISSED");
                    return;
                }

                RefreshStatus(missedPrompt);
                return;
            }

            PlayInputFeedback(true);
            stops++;
            if (stops >= requiredStops)
            {
                RefreshStatus(hitPrompt);
                FinishGame(true, "COMPLETE");
                return;
            }

            PickZone();
            RefreshStatus(hitPrompt);
        }

        private float CurrentPosition()
        {
            return TimingStopMath.PingPong01(elapsed, cycleSeconds);
        }

        /// <summary>当たりゾーンを引き直す。</summary>
        /// <remarks>
        /// 今マーカーがいる場所を避けて選ぶ。避けないと、成功の直後に同じ場所で
        /// もう一度押すだけで通ってしまい、目押しにならないためである。
        /// </remarks>
        private void PickZone()
        {
            var current = CurrentPosition();
            var center = current;
            for (var attempt = 0; attempt < 8 && Mathf.Abs(center - current) < zoneWidth; attempt++)
            {
                center = TimingStopMath.ClampZoneCenter(Random.value, zoneWidth);
            }

            zoneCenter = center;

            var half = zoneWidth * 0.5f;
            targetZone.anchorMin = new Vector2(zoneCenter - half, 0f);
            targetZone.anchorMax = new Vector2(zoneCenter + half, 1f);
            targetZone.offsetMin = Vector2.zero;
            targetZone.offsetMax = Vector2.zero;
        }

        private void ApplyMarker()
        {
            var x = CurrentPosition();
            marker.anchorMin = new Vector2(x, 0f);
            marker.anchorMax = new Vector2(x, 1f);
            marker.anchoredPosition = Vector2.zero;
        }

        private void RefreshStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            if (progressText != null)
            {
                progressText.text = string.Format(progressFormat, stops, requiredStops);
            }

            if (missText != null)
            {
                missText.text = string.Format(missFormat, misses, allowedMisses);
            }
        }
    }
}
