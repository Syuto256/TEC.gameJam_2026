using System;
using UnityEngine;

public enum AudioCue
{
    TitleBgm,
    GameBgm,
    ClearBgm,
    GameOverBgm,
    UiConfirm,
    MiniGameSuccess,
    MiniGameFailure
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

    [SerializeField] private CueEntry[] entries = Array.Empty<CueEntry>();

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
