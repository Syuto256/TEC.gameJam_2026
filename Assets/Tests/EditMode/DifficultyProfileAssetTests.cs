using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>難易度の設定漏れを見つける。</summary>
/// <remarks>
/// 行が無い難易度は既定値（`maxTaskLevel = 1`）で動くため、**その難易度だけ静かに
/// いちばん簡単になる**。エラーも警告も出ないので、遊んで気づけるとは限らない。
/// 実際に 2026-08-04 まで Very Hard がこの状態だった。
/// </remarks>
public sealed class DifficultyProfileAssetTests
{
    private const string SettingsPath = "Assets/Data/GameTuningSettings.asset";
    private const string ScenePath = "Assets/Scenes/DifficultySelect.unity";

    private static GameTuningSettings LoadSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<GameTuningSettings>(SettingsPath);
        Assert.That(settings, Is.Not.Null, SettingsPath + " が見つかりません。");
        return settings;
    }

    [Test]
    public void EveryDifficultyHasProfile()
    {
        var missing = LoadSettings().FindMissingDifficultyProfiles();
        Assert.That(missing, Is.Empty,
            "難易度プロファイルの行がありません -> " + string.Join(", ", missing));
    }

    /// <summary>難易度選択に並べたボタンが、すべて設定のある難易度を指しているか。</summary>
    [Test]
    public void EverySelectableDifficultyHasProfile()
    {
        var settings = LoadSettings();
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            var selectable = new List<GameDifficulty>();
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var manager in root.GetComponentsInChildren<DifficultySelectManager>(true))
                {
                    var choices = new SerializedObject(manager).FindProperty("choices");
                    for (var i = 0; i < choices.arraySize; i++)
                    {
                        var choice = choices.GetArrayElementAtIndex(i);
                        Assert.That(choice.FindPropertyRelative("button").objectReferenceValue, Is.Not.Null,
                            "ボタンが未設定の行があります。");
                        selectable.Add((GameDifficulty)choice.FindPropertyRelative("difficulty").enumValueIndex);
                    }
                }
            }

            Assert.That(selectable, Is.Not.Empty, "難易度選択にボタンが 1 つもありません。");
            var missing = settings.FindMissingDifficultyProfiles();
            foreach (var difficulty in selectable)
            {
                Assert.That(missing, Has.No.Member(difficulty),
                    difficulty + " は選べるのに設定の行がありません。既定値（レベル 1 固定）で動いてしまいます。");
            }

            Assert.That(selectable, Is.Unique, "同じ難易度のボタンが 2 つあります。");
        }
        finally
        {
            UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
        }
    }

    /// <summary>難易度が上がるほどタスクの問題レベルが下がらないこと。</summary>
    /// <remarks>
    /// 具体的な数値はプレイテストで動くため縛らない。ただし「上の難易度のほうが簡単」
    /// という逆転だけは事故なので、順序関係だけを見る。
    /// </remarks>
    [Test]
    public void HarderDifficultiesDoNotHaveLowerTaskLevels()
    {
        var settings = LoadSettings();
        var order = new[]
        {
            GameDifficulty.Easy, GameDifficulty.Normal, GameDifficulty.Hard, GameDifficulty.VeryHard
        };

        for (var i = 1; i < order.Length; i++)
        {
            var previous = settings.GetDifficultyProfile(order[i - 1]);
            var current = settings.GetDifficultyProfile(order[i]);
            foreach (var elapsed in new[] { 0f, 45f, 90f, 150f })
            {
                Assert.That(current.GetTaskLevel(elapsed), Is.GreaterThanOrEqualTo(previous.GetTaskLevel(elapsed)),
                    order[i] + " の " + elapsed + " 秒時点のレベルが " + order[i - 1] + " より低くなっています。");
            }
        }
    }
}
