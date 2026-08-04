using System.Reflection;
using NUnit.Framework;
using Overwork.MiniGames.Typing;
using UnityEditor;
using UnityEngine;

/// <summary>打ち間違えた直後の入力無効時間を、Prefab の実体で確かめる。</summary>
/// <remarks>
/// 速く打つ人は 1 回つまずくと指が数文字ぶん先に進む。間を置かないと、1 度の打ち間違いが
/// そのまま 2 ミス失敗になる。仕様（Docs/Specifications/typing-mini-game.md）の
/// 「入力無効中のキーは完全に無視する」を守れているかを見る。
/// </remarks>
public sealed class TypingMissLockoutTests
{
    private const string PrefabPath = "Assets/Prefabs/MiniGames/TypingMiniGame.prefab";

    private static readonly MethodInfo OnUpdateMethod = typeof(TypingMiniGame).GetMethod(
        "OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

    private GameObject instance;
    private TypingMiniGame game;

    [SetUp]
    public void SetUp()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Assert.That(prefab, Is.Not.Null, PrefabPath + " が見つかりません。");
        instance = Object.Instantiate(prefab);
        game = instance.GetComponent<TypingMiniGame>();
        Assert.That(game, Is.Not.Null);
        game.Initialize(1, 60f);
        Assert.That(game.IsPlaying, Is.True, "初期化に失敗しています。");
    }

    [TearDown]
    public void TearDown()
    {
        if (instance != null) Object.DestroyImmediate(instance);
    }

    /// <summary>お題に関係なく必ず誤入力になる文字。ローマ字候補は英字しか含まない。</summary>
    private const char WrongKey = 'q';

    private static void Advance(TypingMiniGame target, float seconds)
    {
        OnUpdateMethod.Invoke(target, new object[] { seconds });
    }

    [Test]
    public void MissStartsTheLockout()
    {
        Assert.That(game.IsInputLocked, Is.False);
        game.ProcessInput(WrongKey);
        Assert.That(game.IsInputLocked, Is.True, "打ち間違えても入力無効にならない。");
    }

    [Test]
    public void InputDuringLockoutIsIgnoredEntirely()
    {
        game.ProcessInput(WrongKey);

        // 無効時間中は、2 回目の誤入力でミスが増えない。増えるなら 2 ミスで失敗しているはず。
        game.ProcessInput(WrongKey);
        game.ProcessInput(WrongKey);
        Assert.That(game.IsPlaying, Is.True, "無効時間中の入力がミスとして数えられている。");
    }

    [Test]
    public void LockoutExpiresAndInputIsAcceptedAgain()
    {
        game.ProcessInput(WrongKey);
        Assert.That(game.IsInputLocked, Is.True);

        Advance(game, 0.1f);
        Assert.That(game.IsInputLocked, Is.True, "無効時間が短すぎる。");

        Advance(game, 0.15f);
        Assert.That(game.IsInputLocked, Is.False, "無効時間が明けない。");
    }

    [Test]
    public void SecondMissAfterLockoutStillFails()
    {
        // 間を置いた 2 回目の打ち間違いは、これまでどおり失敗になること。
        game.ProcessInput(WrongKey);
        Advance(game, 0.5f);
        game.ProcessInput(WrongKey);
        Assert.That(game.IsPlaying, Is.False, "許容ミス数に達しても失敗していない。");
    }
}
