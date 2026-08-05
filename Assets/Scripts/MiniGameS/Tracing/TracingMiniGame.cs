using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace Overwork.MiniGames.Tracing
{
    /// <summary>指定された経路を左ボタンを押したままなぞるミニゲーム。</summary>
    /// <remarks>
    /// 盤面・マーカー・文字の配置は <c>Assets/Prefabs/MiniGames/TracingMiniGame.prefab</c> で調整する。
    /// ガイド線だけは経路データの点数に応じて本数が変わるため実行時に複製する。
    /// 太さ・色は複製元の <see cref="guideSegmentTemplate"/> を Prefab 上で編集して調整する。
    /// </remarks>
    public sealed class TracingMiniGame : MiniGameBase
    {
        [Header("【使用データ】")]
        [Tooltip("なぞる経路の一覧。この Prefab が自分で持つ。")]
        [SerializeField] private TracingPathDatabase database;

        [Header("【表示先】")]
        [Tooltip("経路を描く領域。マーカーとガイド線はこの子になる。")]
        [SerializeField] private RectTransform board;

        [Tooltip("操作の案内。画面下中央に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("ミス数。画面左上に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text missText;

        [SerializeField] private RectTransform startMarker;
        [SerializeField] private RectTransform endMarker;
        [SerializeField] private RectTransform pointerMarker;

        [Tooltip("ガイド線 1 本分の複製元。board の子に置き、非アクティブにしておく。")]
        [SerializeField] private RectTransform guideSegmentTemplate;

        [Header("【難度の調整】")]
        [Tooltip("始点・終点に触れたと判定する距離（盤面の短辺に対する割合）。")]
        [Range(0.01f, 0.3f)] [SerializeField] private float markerHitRadius = 0.07f;

        [Tooltip("何回経路から外れたら失敗にするか。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

        [Header("【表示する文言】")]
        [SerializeField] private string startPrompt = "始点から左ドラッグでなぞる";
        [SerializeField] private string tracingPrompt = "なぞり中";
        [SerializeField] private string releasedPrompt = "始点からやり直し";
        [SerializeField] private string missedPrompt = "外れました。始点からやり直し";
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

        private TracingPathEntry path;
        private bool tracing;
        private int misses;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this,
                    (nameof(database), database), (nameof(board), board), (nameof(statusText), statusText),
                    (nameof(startMarker), startMarker), (nameof(endMarker), endMarker),
                    (nameof(pointerMarker), pointerMarker), (nameof(guideSegmentTemplate), guideSegmentTemplate)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            if (!database.TryGetRandomPath(difficulty, out path) || path.points == null || path.points.Count < 2)
            {
                FinishGame(false, "NO PATH CONFIGURED");
                return;
            }

            BuildGuide();
            startMarker.anchoredPosition = NormalizedToLocal(path.points[0]);
            endMarker.anchoredPosition = NormalizedToLocal(path.points[path.points.Count - 1]);
            pointerMarker.gameObject.SetActive(false);
            RefreshStatus(startPrompt);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (Mouse.current == null || board == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    board, Mouse.current.position.ReadValue(), null, out var local))
            {
                return;
            }

            var normalized = LocalToNormalized(local);
            if (!tracing)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame
                    && Vector2.Distance(normalized, path.points[0]) <= markerHitRadius)
                {
                    tracing = true;
                    PlayInputFeedback(true);
                    ShowPointer(local);
                    RefreshStatus(tracingPrompt);
                }

                return;
            }

            if (!Mouse.current.leftButton.isPressed)
            {
                ResetTrace();
                RefreshStatus(releasedPrompt);
                return;
            }

            ShowPointer(local);
            if (TracingPathMath.DistanceToPolyline(normalized, path.points) > path.allowedDeviationRatio)
            {
                RegisterMiss();
                return;
            }

            if (Vector2.Distance(normalized, path.points[path.points.Count - 1]) <= markerHitRadius)
            {
                FinishGame(true, "COMPLETE");
            }
        }

        private void RegisterMiss()
        {
            PlayInputFeedback(false);
            misses++;
            ResetTrace();
            // ★ 修正: 制限時間が 90 秒以上（チュートリアル時）はミス失敗を行わず、何度でもやり直せるようにする！
            var isTutorial = TimeLimit >= 90f;
            if (!isTutorial && misses >= allowedMisses)
            {
                FinishGame(false, "MISSED");
                return;
            }

            RefreshStatus(missedPrompt);
        }

        private void ResetTrace()
        {
            tracing = false;
            if (pointerMarker != null)
            {
                pointerMarker.gameObject.SetActive(false);
            }
        }

        private void ShowPointer(Vector2 localPosition)
        {
            pointerMarker.gameObject.SetActive(true);
            pointerMarker.anchoredPosition = localPosition;
        }

        private void RefreshStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            if (missText != null)
            {
                missText.text = string.Format(missFormat, misses, allowedMisses);
            }
        }

        /// <summary>経路の点数だけガイド線を複製して並べる。本数が可変なので実行時に作る。</summary>
        private void BuildGuide()
        {
            guideSegmentTemplate.gameObject.SetActive(false);
            for (var i = 0; i < path.points.Count - 1; i++)
            {
                var from = NormalizedToLocal(path.points[i]);
                var to = NormalizedToLocal(path.points[i + 1]);
                var delta = to - from;

                var segment = Instantiate(guideSegmentTemplate, guideSegmentTemplate.parent, false);
                segment.name = "GuideSegment_" + i;
                segment.gameObject.SetActive(true);
                segment.anchoredPosition = (from + to) * 0.5f;
                segment.sizeDelta = new Vector2(delta.magnitude, guideSegmentTemplate.sizeDelta.y);
                segment.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                segment.SetAsFirstSibling();
            }
        }

        private Vector2 LocalToNormalized(Vector2 local)
        {
            return new Vector2(local.x / board.rect.width + 0.5f, local.y / board.rect.height + 0.5f);
        }

        private Vector2 NormalizedToLocal(Vector2 normalized)
        {
            return new Vector2((normalized.x - 0.5f) * board.rect.width, (normalized.y - 0.5f) * board.rect.height);
        }
    }
}
