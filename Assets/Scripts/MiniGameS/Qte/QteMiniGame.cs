using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.Qte
{
    /// <summary>警告ダイアログが次々出てくる。猶予のうちに、それぞれのキーを押していく。</summary>
    /// <remarks>
    /// 仕様と数値の根拠は Docs/Decisions/2026-08-06-qte-rework.md にある。
    /// 判定は <see cref="QteAlertBoard"/> が持ち、このクラスは入力の読み取りと表示だけを担当する。
    ///
    /// 配置・配色・文言は <c>Assets/Prefabs/MiniGames/QteMiniGame.prefab</c> で調整する。
    /// ダイアログは出る枚数が変わるため実行時に複製する。見た目は複製元の
    /// <see cref="alertTemplate"/> を Prefab 上で編集して調整する。
    /// </remarks>
    public sealed class QteMiniGame : MiniGameBase
    {
        /// <summary>お題に使うキー 1 つ分の設定。</summary>
        [Serializable]
        public sealed class KeyChoice
        {
            [Tooltip("受け付けるキー。ここに無いキーは押しても無視される。")]
            public Key key = Key.W;

            [Tooltip("Shift を押しながらでないと当たらないようにする。\n" +
                     "Ctrl と Alt は使わない。ブラウザと OS に横取りされるためである。")]
            public bool requiresShift;

            [Tooltip("画面に出す文字。空にするとキー名がそのまま出る。")]
            public string label = string.Empty;

            [Tooltip("ダイアログに出す文言。空にすると既定の文言が出る。")]
            public string message = string.Empty;
        }

        [Header("【使用データ】")]
        [Tooltip("お題に使うキーの一覧。ここから抽選して警告を作る。\n" +
                 "同時に出せる枚数はこの種類数を超えられない（同じキーを 2 枚出さないため）。")]
        [SerializeField] private KeyChoice[] keyPool =
        {
            new KeyChoice { key = Key.W, label = "W", message = "確認してください" },
            new KeyChoice { key = Key.A, label = "A", message = "確認してください" },
            new KeyChoice { key = Key.S, label = "S", message = "確認してください" },
            new KeyChoice { key = Key.D, label = "D", message = "確認してください" },
            new KeyChoice { key = Key.S, requiresShift = true, label = "Shift + S", message = "保存してください" },
            new KeyChoice { key = Key.Z, requiresShift = true, label = "Shift + Z", message = "直前の作業を取り消してください" },
            new KeyChoice { key = Key.C, requiresShift = true, label = "Shift + C", message = "複製してください" },
            new KeyChoice { key = Key.X, requiresShift = true, label = "Shift + X", message = "切り取ってください" }
        };

        [Header("【表示先】")]
        [Tooltip("ダイアログの置き場所。ここに置いた順が出る順になる。\n" +
                 "位置が毎回変わると探し直しになるため、抽選はしない。")]
        [SerializeField] private RectTransform[] slots;

        [Tooltip("ダイアログ 1 枚分の複製元。非アクティブにしておく。")]
        [SerializeField] private QteAlertView alertTemplate;

        [Tooltip("操作の案内。画面下中央に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("ミス数。画面左下に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text missText;

        [Tooltip("残りの枚数。未設定でも進行に影響しない。")]
        [SerializeField] private TMP_Text remainingText;

        [Header("【難度の調整】")]
        [Tooltip("レベル 1〜4 で出る警告の総数。")]
        [SerializeField] private int[] totalCountByLevel = { 3, 5, 6, 8 };

        [Tooltip("レベル 1〜4 での、警告 1 枚あたりの猶予（秒）。")]
        [SerializeField] private float[] graceSecByLevel = { 4.0f, 3.3f, 2.6f, 2.0f };

        [Tooltip("レベル 1〜4 での、次の警告が出るまでの間隔（秒）。\n" +
                 "猶予より短いほど画面に溜まる。ここが忙しさのつまみである。")]
        [SerializeField] private float[] spawnIntervalByLevel = { 2.0f, 1.6f, 1.3f, 1.0f };

        [Tooltip("レベル 1〜4 での、同時に出しておける枚数。")]
        [SerializeField] private int[] maxConcurrentByLevel = { 3, 3, 3, 3 };

        [Tooltip("何回ミスしたら失敗にするか。他のミニゲームと同じ 2 が既定である。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

        // 「押」「違」は EnkaDotMincho24 SDF にまだ字形が無いため、既定の文言では避けている。
        [Header("【表示する文言】")]
        [SerializeField] private string prompt = "出てきた警告からキー入力";
        [SerializeField] private string missedPrompt = "ミス";
        [SerializeField] private string missFormat = "ミス: {0} / {1}";
        [SerializeField] private string remainingFormat = "残り: {0}";
        [SerializeField] private string defaultMessage = "確認してください";

        private readonly Dictionary<QteAlert, QteAlertView> views = new Dictionary<QteAlert, QteAlertView>();
        private readonly List<QteAlert> finishedAlerts = new List<QteAlert>();
        private readonly System.Random random = new System.Random();
        private QteAlertBoard board;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this,
                    (nameof(alertTemplate), alertTemplate)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            if (keyPool == null || keyPool.Length == 0)
            {
                Debug.LogError(nameof(QteMiniGame) + " (" + name + "): keyPool が空です。", this);
                FinishGame(false, "NO KEYS CONFIGURED");
                return;
            }

            if (slots == null || slots.Length == 0)
            {
                Debug.LogError(nameof(QteMiniGame) + " (" + name + "): slots が空です。", this);
                FinishGame(false, "NO SLOTS CONFIGURED");
                return;
            }

            alertTemplate.gameObject.SetActive(false);
            ClearViews();

            var level = Mathf.Clamp(difficulty, 1, 4);
            board = new QteAlertBoard(new QteAlertBoardSettings
            {
                TotalCount = ValueAt(totalCountByLevel, level, 3),
                GraceSec = ValueAt(graceSecByLevel, level, 4f),
                SpawnIntervalSec = ValueAt(spawnIntervalByLevel, level, 2f),
                MaxConcurrent = ValueAt(maxConcurrentByLevel, level, 3),
                AllowedMisses = allowedMisses,
                KeyPoolSize = keyPool.Length,
                SlotCount = slots.Length
            }, random);

            RefreshStatus(prompt);
        }

        protected override void OnUpdate(float deltaTime)
        {
            if (board == null)
            {
                return;
            }

            board.Tick(deltaTime);

            if (board.ExpiredThisTick.Count > 0)
            {
                PlayInputFeedback(false);
                RefreshStatus(missedPrompt);
            }

            ReadInput();
            SyncViews();

            if (board.IsFailed)
            {
                FinishGame(false, "MISSED");
                return;
            }

            if (board.IsComplete)
            {
                FinishGame(true, "COMPLETE");
            }
        }

        private void ReadInput()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            var shiftHeld = keyboard.shiftKey.isPressed;

            for (var keyId = 0; keyId < keyPool.Length; keyId++)
            {
                var choice = keyPool[keyId];
                if (choice == null || choice.key == Key.None)
                {
                    continue;
                }

                // 修飾キーは完全一致で見る。緩くすると、素の S と Shift + S を
                // 両方 pool に入れたときに Shift + S が素の S にも当たってしまう。
                if (choice.requiresShift != shiftHeld)
                {
                    continue;
                }

                if (!keyboard[choice.key].wasPressedThisFrame)
                {
                    continue;
                }

                HandlePress(keyId);
                if (!IsPlaying || board.IsFailed || board.IsComplete)
                {
                    return;
                }
            }
        }

        private void HandlePress(int keyId)
        {
            switch (board.Press(keyId))
            {
                case QtePressResult.Cleared:
                    PlayInputFeedback(true);
                    RefreshStatus(prompt);
                    break;

                case QtePressResult.Missed:
                    PlayInputFeedback(false);
                    RefreshStatus(missedPrompt);
                    break;
            }
        }

        /// <summary>盤面に合わせてダイアログを作り直す。</summary>
        /// <remarks>
        /// 出現・消滅の通知を受け取るのではなく、毎フレーム突き合わせている。
        /// 同時に出るのは数枚なので総当たりで足り、通知の取りこぼしで表示だけ残る事故も防げる。
        /// </remarks>
        private void SyncViews()
        {
            finishedAlerts.Clear();
            foreach (var pair in views)
            {
                if (!IsActive(pair.Key))
                {
                    finishedAlerts.Add(pair.Key);
                }
            }

            for (var index = 0; index < finishedAlerts.Count; index++)
            {
                var alert = finishedAlerts[index];
                if (views.TryGetValue(alert, out var view) && view != null)
                {
                    // 先に隠してから捨てる。Destroy はフレームの終わりまで実体が残るため、
                    // 空いた置き場所へ次の警告が入ると、その 1 フレームだけ 2 枚重なって見える。
                    view.gameObject.SetActive(false);
                    Destroy(view.gameObject);
                }

                views.Remove(alert);
            }

            var active = board.ActiveAlerts;
            for (var index = 0; index < active.Count; index++)
            {
                var alert = active[index];
                if (!views.TryGetValue(alert, out var view))
                {
                    view = CreateView(alert);
                    views[alert] = view;
                }

                if (view != null)
                {
                    view.SetRemaining(alert.RemainingRatio);
                }
            }

            RefreshCounters();
        }

        private QteAlertView CreateView(QteAlert alert)
        {
            var slot = slots[Mathf.Clamp(alert.SlotIndex, 0, slots.Length - 1)];
            var view = Instantiate(alertTemplate, slot, false);
            view.name = "Alert_" + alert.SlotIndex;
            view.gameObject.SetActive(true);

            var choice = keyPool[Mathf.Clamp(alert.KeyId, 0, keyPool.Length - 1)];
            var label = string.IsNullOrEmpty(choice.label) ? choice.key.ToString() : choice.label;
            var message = string.IsNullOrEmpty(choice.message) ? defaultMessage : choice.message;
            view.Setup(message, label);
            return view;
        }

        private bool IsActive(QteAlert alert)
        {
            var active = board.ActiveAlerts;
            for (var index = 0; index < active.Count; index++)
            {
                if (ReferenceEquals(active[index], alert))
                {
                    return true;
                }
            }

            return false;
        }

        private void ClearViews()
        {
            foreach (var pair in views)
            {
                if (pair.Value != null)
                {
                    pair.Value.gameObject.SetActive(false);
                    Destroy(pair.Value.gameObject);
                }
            }

            views.Clear();
        }

        private void RefreshStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            RefreshCounters();
        }

        private void RefreshCounters()
        {
            if (board == null)
            {
                return;
            }

            if (missText != null)
            {
                missText.text = string.Format(missFormat, board.Misses, allowedMisses);
            }

            if (remainingText != null)
            {
                remainingText.text = string.Format(remainingFormat,
                    board.NotYetSpawned + board.ActiveAlerts.Count);
            }
        }

        private static int ValueAt(IReadOnlyList<int> values, int level, int fallback)
        {
            return values == null || values.Count == 0
                ? fallback
                : values[Mathf.Clamp(level - 1, 0, values.Count - 1)];
        }

        private static float ValueAt(IReadOnlyList<float> values, int level, float fallback)
        {
            return values == null || values.Count == 0
                ? fallback
                : values[Mathf.Clamp(level - 1, 0, values.Count - 1)];
        }
    }
}
