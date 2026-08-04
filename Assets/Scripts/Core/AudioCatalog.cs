using System;
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
    MiniGameInputMiss
}

[CreateAssetMenu(fileName = "AudioCatalog", menuName = "Game/Audio Catalog")]
public sealed class AudioCatalog : ScriptableObject
{
    [Serializable]
    public sealed class CueEntry
    {
        [Tooltip("どの場面で鳴る音かを選ぶ。どの種類がいつ鳴るかは Docs/Architecture/audio-manager.md の一覧を参照。")]
        public AudioCue cue;

        [Tooltip("鳴らす音声ファイル。空のままだと、その場面では何も鳴らない（エラーにはならない）。")]
        public AudioClip clip;

        [Tooltip("この音だけの音量。オプション画面の全体音量と掛け合わされる。\n" +
                 "他の音より大きい・小さいと感じたときにここで揃える。")]
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Tooltip("音の種類ごとのクリップと音量。未登録の種類は鳴らないだけで、エラーにはならない。")]
    [SerializeField] private CueEntry[] entries = Array.Empty<CueEntry>();

    /// <summary>クリップがまだ割り当てられていない種類を列挙する。素材の抜けを確認するために使う。</summary>
    public string[] GetMissingCueNames()
    {
        var missing = new System.Collections.Generic.List<string>();
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
