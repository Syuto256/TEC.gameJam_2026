using UnityEngine;

/// <summary>シーンをまたいで生き続ける共通サービスをそろえる。</summary>
/// <remarks>
/// 各シーンの Manager が <c>Start</c> の先頭で一度だけ呼ぶ。どのシーンから再生を始めても同じ状態になる。
/// <c>EventSystem</c> はここでは作らない。各シーンに実体として置き、Hierarchy から見えるようにする。
/// </remarks>
public static class AppServices
{
    public static void Ensure()
    {
        GameFlowController.EnsureInstance();
        AudioManager.EnsureInstance();
        FadeOverlayView.EnsureInstance();
        PcLidView.EnsureInstance();
    }

    /// <summary>決定音を鳴らしてから遷移する、画面共通のボタン動作。</summary>
    public static void PlayConfirm()
    {
        AudioManager.PlaySfx(AudioCue.UiConfirm);
    }
}
