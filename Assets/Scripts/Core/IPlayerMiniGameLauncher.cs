using System;
using UnityEngine;

/// <summary>個別ミニゲームが Core から受け取る最小の起動契約。</summary>
public interface IPlayerMiniGameLauncher
{
    TaskKind Kind { get; }
    bool IsReady { get; }
    bool TryStart(GameObject host, int level, float timeLimit, Action<bool, string> onCompleted);
}
