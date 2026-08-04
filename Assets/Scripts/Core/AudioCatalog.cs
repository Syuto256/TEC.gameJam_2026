using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>鳴らす音の種類。</summary>
/// <remarks>
/// アセットは選択肢を整数で保存する。**並びを変えたり途中に挿入したりしないこと。**
/// 追加するときは必ず末尾に足す。途中に挿入すると、既存の登録が黙って別の音を指すようになる。
/// </remarks>
public enum AudioCue
{
    TitleBgm,
    GameBgm,
    ClearBgm,
    GameOverBgm,
    UiConfirm,
    MiniGameSuccess,
    MiniGameFailure,

    // ここから 2026-08-04 追加
    DifficultySelectBgm,
    UiCancel,
    TaskSpawned,
    TaskExpired,
    AiRequested,
    AiSucceeded,
    AiFailed,
    PauseOpen,
    PauseClose,
    HpLow,
    MiniGameInputHit,
    MiniGameInputMiss,
    ComboMilestone,

    AmbientSound1,
    AmbientSound2,
    AmbientSound3,
    AmbientSound4,

    // ★ 必ず末尾に追加する
    ClearJingle,
    ClearJingle2,
    GameOverJingle
    
}


[CreateAssetMenu(fileName = "AudioCatalog", menuName = "Game/Audio Catalog")]
public sealed class AudioCatalog : ScriptableObject
{
    [Serializable]
    public sealed class CueEntry
    {
        public AudioCue cue;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("【通常音源】")]
    [SerializeField] private CueEntry[] entries = Array.Empty<CueEntry>();

    // ★追加: 環境音の設定項目
    [Header("【環境音の設定】")]
    [Tooltip("環境音が発生する最小間隔（秒）")]
    [SerializeField] private float ambientMinIntervalSec = 5f;

    [Tooltip("環境音が発生する最大間隔（秒）")]
    [SerializeField] private float ambientMaxIntervalSec = 15f;

    [Tooltip("ランダム再生する環境音のリスト")]
    [SerializeField] private AudioCue[] ambientCues = Array.Empty<AudioCue>();

    // ★ 2. クリア時の2つ目のSEの遅延時間（秒）を追加
    [Header("【クリア演出設定】")]
    [Tooltip("クリア時の2つ目のSEを鳴らすまでの遅延時間（秒）")]
    [SerializeField] private float clearJingle2DelaySec = 0.8f; 

    public float AmbientMinIntervalSec => ambientMinIntervalSec;
    public float AmbientMaxIntervalSec => ambientMaxIntervalSec;
    public IReadOnlyList<AudioCue> AmbientCues => ambientCues;

    // ★ 3. 外部から遅延時間を読み取るプロパティ
    public float ClearJingle2DelaySec => clearJingle2DelaySec;

   
    public string[] GetMissingCueNames()
    {
        var missing = new List<string>();
        foreach (AudioCue cue in Enum.GetValues(typeof(AudioCue)))
        {
            if (!TryGet(cue, out _, out _))
            {
                missing.Add(cue.ToString());
            }
        }
        return missing.ToArray();
    }

    public bool TryGet(AudioCue cue, out AudioClip clip, out float volume)
    {
        foreach (var entry in entries)
        {
            if (entry != null && entry.cue == cue && entry.clip != null)
            {
                clip = entry.clip;
                volume = entry.volume;
                return true;
            }
        }

        clip = null;
        volume = 0f;
        return false;
    }
}
