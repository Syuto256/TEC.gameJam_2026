using UnityEngine;

namespace Overwork.MiniGames.TimingStop
{
    /// <summary>目押しの位置計算と当たり判定。表示に依存しないため単体で確かめられる。</summary>
    /// <remarks>
    /// 位置はすべて 0〜1 の割合で扱う。0 がバーの左端、1 が右端である。
    /// バーの実際の幅を知らなくて済むので、Prefab で大きさを変えても判定は変わらない。
    /// </remarks>
    public static class TimingStopMath
    {
        /// <summary>往復するマーカーの位置（0〜1）。<paramref name="cycleSec"/> で 1 往復する。</summary>
        public static float PingPong01(float elapsedSec, float cycleSec)
        {
            if (cycleSec <= 0f)
            {
                return 0f;
            }

            var phase = Mathf.Repeat(elapsedSec / cycleSec, 1f);
            return phase < 0.5f ? phase * 2f : (1f - phase) * 2f;
        }

        /// <summary>マーカーが当たりゾーンに入っているか。</summary>
        public static bool IsHit(float marker01, float zoneCenter01, float zoneWidth01)
        {
            return Mathf.Abs(marker01 - zoneCenter01) <= zoneWidth01 * 0.5f;
        }

        /// <summary>ゾーンがバーからはみ出さない位置へ中心を寄せる。</summary>
        public static float ClampZoneCenter(float center01, float zoneWidth01)
        {
            var half = Mathf.Clamp01(zoneWidth01) * 0.5f;
            return Mathf.Clamp(center01, half, 1f - half);
        }
    }
}
