using System;
using NUnit.Framework;
using Overwork.MiniGames.Qte;

public sealed class QteAlertBoardTests
{
    private static QteAlertBoard Build(
        int totalCount = 4,
        float interval = 2f,
        float grace = 4f,
        int maxConcurrent = 3,
        int allowedMisses = 2,
        int keyPoolSize = 4,
        int slotCount = 4,
        int seed = 1234)
    {
        return new QteAlertBoard(new QteAlertBoardSettings
        {
            TotalCount = totalCount,
            SpawnIntervalSec = interval,
            GraceSec = grace,
            MaxConcurrent = maxConcurrent,
            AllowedMisses = allowedMisses,
            KeyPoolSize = keyPoolSize,
            SlotCount = slotCount
        }, new Random(seed));
    }

    [Test]
    public void Tick_SpawnsFirstAlertWithoutWaiting()
    {
        var board = Build(interval: 2f);
        board.Tick(0.1f);

        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(1));
        Assert.That(board.SpawnedThisTick.Count, Is.EqualTo(1));
        Assert.That(board.NotYetSpawned, Is.EqualTo(3));
    }

    [Test]
    public void Tick_SpawnsNextAlertAfterInterval()
    {
        var board = Build(interval: 2f, grace: 100f);
        board.Tick(0.1f);

        board.Tick(1.0f);
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(1), "間隔より前に出ている");

        board.Tick(1.0f);
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(2));
    }

    [Test]
    public void Tick_NeverExceedsMaxConcurrent()
    {
        var board = Build(totalCount: 8, interval: 0.5f, grace: 100f, maxConcurrent: 2, keyPoolSize: 8, slotCount: 8);

        for (var step = 0; step < 20; step++)
        {
            board.Tick(0.5f);
            Assert.That(board.ActiveAlerts.Count, Is.LessThanOrEqualTo(2));
        }
    }

    [Test]
    public void Tick_NeverShowsSameKeyTwice()
    {
        // 同じキーの警告が 2 枚あると、どちらが閉じるのか分からなくなる。
        var board = Build(totalCount: 12, interval: 0.2f, grace: 100f, maxConcurrent: 3, keyPoolSize: 3, slotCount: 4);

        for (var step = 0; step < 30; step++)
        {
            board.Tick(0.2f);

            var active = board.ActiveAlerts;
            for (var i = 0; i < active.Count; i++)
            {
                for (var j = i + 1; j < active.Count; j++)
                {
                    Assert.That(active[i].KeyId, Is.Not.EqualTo(active[j].KeyId), "同じキーが 2 枚出ている");
                }
            }
        }
    }

    [Test]
    public void Press_ClearsMatchingAlert()
    {
        var board = Build(grace: 100f);
        board.Tick(0.1f);
        var keyId = board.ActiveAlerts[0].KeyId;

        Assert.That(board.Press(keyId), Is.EqualTo(QtePressResult.Cleared));
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(0));
        Assert.That(board.ClearedCount, Is.EqualTo(1));
        Assert.That(board.Misses, Is.EqualTo(0));
    }

    [Test]
    public void Press_CountsAsMissWhenNoAlertMatches()
    {
        // ここが無いと、キーボードを端から叩くだけでクリアできてしまう。
        var board = Build(grace: 100f, keyPoolSize: 4);
        board.Tick(0.1f);
        var shown = board.ActiveAlerts[0].KeyId;
        var other = (shown + 1) % 4;

        Assert.That(board.Press(other), Is.EqualTo(QtePressResult.Missed));
        Assert.That(board.Misses, Is.EqualTo(1));
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(1), "当たらない入力で警告が消えている");
    }

    [Test]
    public void Press_IgnoresKeysOutsidePool()
    {
        var board = Build(grace: 100f);
        board.Tick(0.1f);

        Assert.That(board.Press(-1), Is.EqualTo(QtePressResult.Ignored));
        Assert.That(board.Misses, Is.EqualTo(0));
    }

    [Test]
    public void Tick_ExpiresAlertAndCountsMiss()
    {
        var board = Build(totalCount: 4, interval: 100f, grace: 1f);
        board.Tick(0.1f);
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(1));

        board.Tick(1.0f);

        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(0));
        Assert.That(board.ExpiredThisTick.Count, Is.EqualTo(1));
        Assert.That(board.Misses, Is.EqualTo(1));
    }

    [Test]
    public void Board_FailsWhenMissesReachLimit()
    {
        var board = Build(grace: 100f, allowedMisses: 2, keyPoolSize: 4);
        board.Tick(0.1f);
        var shown = board.ActiveAlerts[0].KeyId;
        var other = (shown + 1) % 4;

        board.Press(other);
        Assert.That(board.IsFailed, Is.False);

        board.Press(other);
        Assert.That(board.IsFailed, Is.True);
        Assert.That(board.Press(other), Is.EqualTo(QtePressResult.Ignored), "失敗後も判定が続いている");
    }

    [Test]
    public void Board_CompletesWhenEveryAlertIsCleared()
    {
        var board = Build(totalCount: 3, interval: 0.5f, grace: 100f);

        for (var step = 0; step < 20 && !board.IsComplete; step++)
        {
            board.Tick(0.5f);
            while (board.ActiveAlerts.Count > 0)
            {
                board.Press(board.ActiveAlerts[0].KeyId);
            }
        }

        Assert.That(board.IsComplete, Is.True);
        Assert.That(board.ClearedCount, Is.EqualTo(3));
        Assert.That(board.Misses, Is.EqualTo(0));
    }

    [Test]
    public void Spawn_ReusesTheLowestFreeSlot()
    {
        // 毎回位置が変わると探し直しになるため、抽選せず空いている先頭へ入れる。
        var board = Build(totalCount: 4, interval: 1f, grace: 100f, maxConcurrent: 2, slotCount: 4);
        board.Tick(0.1f);
        board.Tick(1.0f);

        Assert.That(board.ActiveAlerts[0].SlotIndex, Is.EqualTo(0));
        Assert.That(board.ActiveAlerts[1].SlotIndex, Is.EqualTo(1));

        board.Press(board.ActiveAlerts[0].KeyId);
        board.Tick(1.0f);

        var slots = new[] { board.ActiveAlerts[0].SlotIndex, board.ActiveAlerts[1].SlotIndex };
        Assert.That(slots, Is.EquivalentTo(new[] { 0, 1 }));
    }

    [Test]
    public void MaxConcurrent_IsCappedByKeyPoolAndSlotCount()
    {
        var board = Build(maxConcurrent: 5, keyPoolSize: 2, slotCount: 4);
        Assert.That(board.MaxConcurrent, Is.EqualTo(2));
    }

    [Test]
    public void Press_ClearingMakesTheNextAlertAppearWithoutWaiting()
    {
        // 正しく押せているのに画面が空く時間ができると手が止まるため、
        // さばいた直後の Tick で次が出ること。
        var board = Build(interval: 2f, grace: 100f, maxConcurrent: 1);
        board.Tick(0.1f);
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(1));

        board.Press(board.ActiveAlerts[0].KeyId);
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(0));

        // 間隔（2 秒）を待たずに次が出る。
        board.Tick(0.1f);
        Assert.That(board.SpawnedThisTick.Count, Is.EqualTo(1));
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(1));
    }

    [Test]
    public void Press_MissDoesNotMakeTheNextAlertAppearEarly()
    {
        // 早出しは「さばけたご褒美」であり、打ち間違えたときには効かせない。
        var board = Build(interval: 2f, grace: 100f, maxConcurrent: 1, keyPoolSize: 4);
        board.Tick(0.1f);
        var shown = board.ActiveAlerts[0].KeyId;

        board.Press((shown + 1) % 4);
        Assert.That(board.Misses, Is.EqualTo(1));
        Assert.That(board.ActiveAlerts.Count, Is.EqualTo(1), "外した警告は残ったまま");

        board.Tick(0.1f);
        Assert.That(board.SpawnedThisTick.Count, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_RejectsBrokenSettings()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(totalCount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(grace: 0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(keyPoolSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(slotCount: 0));
    }
}
