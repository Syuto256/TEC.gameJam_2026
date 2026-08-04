using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Overwork.MiniGames.Qte
{
    /// <summary>表示された並びを、左から順にキーで押していくミニゲーム。</summary>
    /// <remarks>
    /// 配置・配色・文字は <c>Assets/Prefabs/MiniGames/QteMiniGame.prefab</c> で調整する。
    /// キーの枠だけはお題の長さで個数が変わるため実行時に複製する。
    /// 見た目は複製元の <see cref="keyCellTemplate"/> を Prefab 上で編集して調整する。
    /// </remarks>
    public sealed class QteMiniGame : MiniGameBase
    {
        /// <summary>お題に使うキー 1 つ分の設定。</summary>
        [Serializable]
        public sealed class KeyChoice
        {
            [Tooltip("受け付けるキー。ここに無いキーは押しても無視される。")]
            public Key key = Key.W;

            [Tooltip("画面に出す文字。空にするとキー名がそのまま出る。")]
            public string label = string.Empty;
        }

        [Header("【使用データ】")]
        [Tooltip("お題に使うキーの一覧。ここから抽選して並びを作る。\n" +
                 "種類を増やすと難しくなる。1 種類だけにすると連打と同じになる。")]
        [SerializeField] private KeyChoice[] keyPool =
        {
            new KeyChoice { key = Key.W, label = "W" },
            new KeyChoice { key = Key.A, label = "A" },
            new KeyChoice { key = Key.S, label = "S" },
            new KeyChoice { key = Key.D, label = "D" }
        };

        [Header("【表示先】")]
        [Tooltip("キーの枠を横に並べる場所。Horizontal Layout Group を付けておく。")]
        [SerializeField] private RectTransform sequenceRow;

        [Tooltip("キーの枠 1 つ分の複製元。sequenceRow の子に置き、非アクティブにしておく。")]
        [SerializeField] private QteKeyCell keyCellTemplate;

        [Tooltip("操作の案内。画面下中央に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("ミス数。画面左下に置くのが共通の並びである。")]
        [SerializeField] private TMP_Text missText;

        [Header("【難度の調整】")]
        [Tooltip("レベル 1 でのお題の長さ。")]
        [Min(1)] [SerializeField] private int baseLength = 4;

        [Tooltip("レベルが 1 上がるごとに伸びる長さ。")]
        [Min(0)] [SerializeField] private int lengthPerLevel = 2;

        [Tooltip("何回押し間違えたら失敗にするか。")]
        [Min(1)] [SerializeField] private int allowedMisses = 2;

        [Tooltip("押し間違えたときに、お題の最初からやり直させる。\n" +
                 "外すと、間違えた場所で止まったまま続けられる。")]
        [SerializeField] private bool restartOnMiss = true;

        // 「押」「違」は EnkaDotMincho24 SDF にまだ字形が無いため、既定の文言では避けている。
        [Header("【表示する文言】")]
        [SerializeField] private string prompt = "左から順にキー入力";
        [SerializeField] private string missedPrompt = "ミス。最初からやり直し";
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

        private readonly List<QteKeyCell> cells = new List<QteKeyCell>();
        private readonly System.Random random = new System.Random();
        private QteSequence sequence;
        private int misses;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this,
                    (nameof(sequenceRow), sequenceRow), (nameof(keyCellTemplate), keyCellTemplate)))
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

            var length = baseLength + (Mathf.Clamp(difficulty, 1, 4) - 1) * lengthPerLevel;
            sequence = new QteSequence(QteSequence.BuildRandomKeys(keyPool.Length, length, random));
            misses = 0;
            BuildCells();
            RefreshStatus(prompt);
        }

        protected override void OnUpdate(float deltaTime)
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || sequence == null)
            {
                return;
            }

            for (var keyId = 0; keyId < keyPool.Length; keyId++)
            {
                var choice = keyPool[keyId];
                if (choice == null || choice.key == Key.None)
                {
                    continue;
                }

                if (!keyboard[choice.key].wasPressedThisFrame)
                {
                    continue;
                }

                HandlePress(keyId);
                if (!IsPlaying)
                {
                    return;
                }
            }
        }

        private void HandlePress(int keyId)
        {
            switch (sequence.Press(keyId))
            {
                case QtePressResult.Correct:
                    PlayInputFeedback(true);
                    RefreshCells();
                    if (sequence.IsComplete)
                    {
                        FinishGame(true, "COMPLETE");
                        return;
                    }

                    RefreshStatus(prompt);
                    break;

                case QtePressResult.Wrong:
                    RegisterMiss();
                    break;
            }
        }

        private void RegisterMiss()
        {
            PlayInputFeedback(false);
            misses++;
            if (misses >= allowedMisses)
            {
                FinishGame(false, "MISSED");
                return;
            }

            if (restartOnMiss)
            {
                sequence.Restart();
                RefreshCells();
            }

            RefreshStatus(missedPrompt);
        }

        /// <summary>お題の長さだけキーの枠を複製して並べる。個数が可変なので実行時に作る。</summary>
        private void BuildCells()
        {
            keyCellTemplate.gameObject.SetActive(false);
            cells.Clear();

            for (var index = 0; index < sequence.Length; index++)
            {
                var cell = Instantiate(keyCellTemplate, keyCellTemplate.transform.parent, false);
                cell.name = "KeyCell_" + index;
                cell.gameObject.SetActive(true);
                cell.SetLabel(LabelOf(sequence.KeyAt(index)));
                cells.Add(cell);
            }

            RefreshCells();
        }

        private void RefreshCells()
        {
            for (var index = 0; index < cells.Count; index++)
            {
                var state = index < sequence.Progress ? QteCellState.Done
                    : index == sequence.Progress ? QteCellState.Current
                    : QteCellState.Pending;
                cells[index].SetState(state);
            }
        }

        private string LabelOf(int keyId)
        {
            var choice = keyPool[keyId];
            return string.IsNullOrEmpty(choice.label) ? choice.key.ToString() : choice.label;
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
    }
}
