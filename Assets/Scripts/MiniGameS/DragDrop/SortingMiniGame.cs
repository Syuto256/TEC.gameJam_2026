using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Overwork.MiniGames.DragDrop
{
    /// <summary>ファイルを対応するフォルダへ仕分けるミニゲーム。</summary>
    /// <remarks>
    /// フォルダとファイルは Prefab の複製元から実行時に生成する。種類・色・文言は
    /// <see cref="SortingKindStyle"/>、生成数と種類数は <see cref="SortingLevelSetting"/> が持つ。
    /// </remarks>
    public sealed class SortingMiniGame : MiniGameBase
    {
        [Header("【表示先】")]
        [Tooltip("フォルダとファイルを生成する作業領域。")]
        [SerializeField] private RectTransform workArea;

        [Tooltip("ファイル 1 枚の複製元。非アクティブにしておく。")]
        [SerializeField] private SortingDraggable fileCardTemplate;

        [Tooltip("フォルダ 1 個の複製元。非アクティブにしておく。")]
        [SerializeField] private SortingDropBox folderTemplate;

        [Tooltip("ミス数。任意。")]
        [SerializeField] private TMP_Text missText;

        [Tooltip("ミス数の書式。{0} が現在のミス、{1} が上限。")]
        [SerializeField] private string missFormat = "ミス: {0} / {1}";

        [Header("【種類ごとの見た目】")]
        [Tooltip("文書・画像・音声・コードの4種類を登録する。")]
        [SerializeField] private SortingKindStyle[] kindStyles =
        {
            new SortingKindStyle { kind = SortingFileKind.Document, folderTint = Color.white, label = "文書" },
            new SortingKindStyle { kind = SortingFileKind.Image, folderTint = new Color(0.58f, 0.74f, 0.92f, 1f), label = "画像" },
            new SortingKindStyle { kind = SortingFileKind.Audio, folderTint = new Color(0.90f, 0.56f, 0.52f, 1f), label = "音声" },
            new SortingKindStyle { kind = SortingFileKind.Script, folderTint = new Color(0.48f, 0.69f, 0.43f, 1f), label = "コード" }
        };

        [Tooltip("4種類共通のフォルダ絵。")]
        [SerializeField] private Sprite folderIcon;

        [Header("【レベル別の生成設定】")]
        [SerializeField] private SortingLevelSetting[] levelSettings =
        {
            new SortingLevelSetting { level = 1, fileCount = 3, maxKinds = 1, allowedMisses = 2 },
            new SortingLevelSetting { level = 2, fileCount = 4, maxKinds = 2, allowedMisses = 2 },
            new SortingLevelSetting { level = 3, fileCount = 5, maxKinds = 3, allowedMisses = 2 },
            new SortingLevelSetting { level = 4, fileCount = 6, maxKinds = 4, allowedMisses = 2 }
        };

        [Header("【生成位置】")]
        [Tooltip("フォルダ列の縦位置。作業領域の中央を基準にする。")]
        [SerializeField] private float folderRowY = 130f;

        [Tooltip("フォルダ同士の間隔。")]
        [Min(0f)] [SerializeField] private float folderSpacing = 240f;

        [Tooltip("ファイル列の縦位置。")]
        [SerializeField] private float fileRowY = -60f;

        [Tooltip("ファイル同士の間隔。")]
        [Min(0f)] [SerializeField] private float fileSpacing = 130f;

        private readonly List<SortingDraggable> generatedCards = new List<SortingDraggable>();
        private readonly List<SortingDropBox> generatedFolders = new List<SortingDropBox>();
        private SortingLevelSetting activeSetting;
        private int remaining;
        private int misses;

        public override void Initialize(int difficulty, float timeLimit)
        {
            base.Initialize(difficulty, timeLimit);
            if (!SceneUiValidation.Require(this,
                    (nameof(workArea), workArea),
                    (nameof(fileCardTemplate), fileCardTemplate),
                    (nameof(folderTemplate), folderTemplate)))
            {
                FinishGame(false, "PREFAB NOT CONFIGURED");
                return;
            }

            if (!HasAllStyles())
            {
                Debug.LogError(nameof(SortingMiniGame) + " (" + name + "): 4種類の見た目が揃っていません。", this);
                FinishGame(false, "STYLE NOT CONFIGURED");
                return;
            }

            activeSetting = FindSetting(difficulty);
            if (activeSetting == null)
            {
                Debug.LogError(nameof(SortingMiniGame) + " (" + name + "): Lv." + difficulty + " の設定がありません。", this);
                FinishGame(false, "LEVEL NOT CONFIGURED");
                return;
            }

            ClearGenerated();
            fileCardTemplate.gameObject.SetActive(false);
            folderTemplate.gameObject.SetActive(false);
            misses = 0;
            CreateFolders();

            var selectedKinds = PickKinds(Mathf.Clamp(activeSetting.maxKinds, 1, 4));
            var fileKinds = BuildFileKinds(Mathf.Max(1, activeSetting.fileCount), selectedKinds);
            remaining = fileKinds.Count;
            CreateCards(fileKinds);
            Refresh();
        }

        /// <summary>カードがフォルダに落とされたときに呼ばれる。</summary>
        public void Drop(SortingDraggable card, bool matched)
        {
            if (!IsPlaying || card == null)
            {
                return;
            }

            PlayInputFeedback(matched);
            if (matched)
            {
                generatedCards.Remove(card);
                Destroy(card.gameObject);
                remaining--;
                if (remaining <= 0)
                {
                    FinishGame(true, "COMPLETE");
                    return;
                }
            }
            else
            {
                misses++;
                if (misses >= activeSetting.allowedMisses)
                {
                    FinishGame(false, "MISSED");
                    return;
                }
            }

            Refresh();
        }

        protected override void OnUpdate(float deltaTime)
        {
        }

        private void CreateFolders()
        {
            var totalWidth = folderSpacing * (kindStyles.Length - 1);
            var left = -totalWidth * 0.5f;
            for (var index = 0; index < kindStyles.Length; index++)
            {
                var style = kindStyles[index];
                var folder = Instantiate(folderTemplate, workArea, false);
                folder.name = "Folder_" + style.kind;
                folder.gameObject.SetActive(true);
                var rect = folder.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(left + folderSpacing * index, folderRowY);
                folder.Setup(style.kind, folderIcon, style.folderTint, style.label);
                folder.Bind(this);
                generatedFolders.Add(folder);
            }
        }

        private void CreateCards(List<SortingFileKind> fileKinds)
        {
            var totalWidth = fileSpacing * (fileKinds.Count - 1);
            var left = -totalWidth * 0.5f;
            for (var index = 0; index < fileKinds.Count; index++)
            {
                var kind = fileKinds[index];
                var style = FindStyle(kind);
                var card = Instantiate(fileCardTemplate, workArea, false);
                card.name = "File_" + kind + "_" + (index + 1);
                card.gameObject.SetActive(true);
                var rect = card.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(left + fileSpacing * index, fileRowY);
                card.Setup(kind, style.fileIcon);
                generatedCards.Add(card);
            }
        }

        private List<SortingFileKind> PickKinds(int count)
        {
            var allKinds = new List<SortingFileKind>
            {
                SortingFileKind.Document,
                SortingFileKind.Image,
                SortingFileKind.Audio,
                SortingFileKind.Script
            };
            for (var index = allKinds.Count - 1; index > 0; index--)
            {
                var swap = Random.Range(0, index + 1);
                var temp = allKinds[index];
                allKinds[index] = allKinds[swap];
                allKinds[swap] = temp;
            }

            return allKinds.GetRange(0, Mathf.Clamp(count, 1, allKinds.Count));
        }

        private List<SortingFileKind> BuildFileKinds(int count, List<SortingFileKind> selectedKinds)
        {
            var result = new List<SortingFileKind>(count);
            var requiredKinds = Mathf.Min(count, selectedKinds.Count);
            for (var index = 0; index < requiredKinds; index++)
            {
                result.Add(selectedKinds[index]);
            }

            while (result.Count < count)
            {
                result.Add(selectedKinds[Random.Range(0, selectedKinds.Count)]);
            }

            for (var index = result.Count - 1; index > 0; index--)
            {
                var swap = Random.Range(0, index + 1);
                var temp = result[index];
                result[index] = result[swap];
                result[swap] = temp;
            }

            return result;
        }

        private bool HasAllStyles()
        {
            foreach (SortingFileKind kind in System.Enum.GetValues(typeof(SortingFileKind)))
            {
                if (FindStyle(kind) == null || FindStyle(kind).fileIcon == null)
                {
                    return false;
                }
            }

            return folderIcon != null;
        }

        private SortingKindStyle FindStyle(SortingFileKind kind)
        {
            if (kindStyles == null)
            {
                return null;
            }

            foreach (var style in kindStyles)
            {
                if (style != null && style.kind == kind)
                {
                    return style;
                }
            }

            return null;
        }

        private SortingLevelSetting FindSetting(int difficulty)
        {
            var level = Mathf.Clamp(difficulty, 1, 4);
            if (levelSettings == null)
            {
                return null;
            }

            foreach (var setting in levelSettings)
            {
                if (setting != null && setting.level == level)
                {
                    setting.allowedMisses = Mathf.Max(1, setting.allowedMisses);
                    setting.fileCount = Mathf.Max(1, setting.fileCount);
                    setting.maxKinds = Mathf.Clamp(setting.maxKinds, 1, 4);
                    return setting;
                }
            }

            return null;
        }

        private void Refresh()
        {
            if (missText != null && activeSetting != null)
            {
                missText.text = string.Format(missFormat, misses, activeSetting.allowedMisses);
            }
        }

        private void ClearGenerated()
        {
            foreach (var card in generatedCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            generatedCards.Clear();

            foreach (var folder in generatedFolders)
            {
                if (folder != null)
                {
                    Destroy(folder.gameObject);
                }
            }
            generatedFolders.Clear();
        }
    }
}
