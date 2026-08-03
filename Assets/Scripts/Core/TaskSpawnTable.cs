using System;
using UnityEngine;

/// <summary>どのデバイス面にどの種別のタスクが出るかを決める表。</summary>
/// <remarks>
/// 「PC にはタイピング・仕分け・連打、タブレットにはなぞりだけ」のような出現パターンをここで持つ。
/// 面を増やす場合も行を 1 つ足すだけで、ゲーム進行コードは変更しない。
/// 種別ごとの Prefab や制限時間は <see cref="MiniGameCatalog"/> が持つ。この表は出現場所だけを決める。
/// </remarks>
[CreateAssetMenu(fileName = "TaskSpawnTable", menuName = "Game/Task Spawn Table")]
public sealed class TaskSpawnTable : ScriptableObject
{
    [Serializable]
    public sealed class SurfaceEntry
    {
        [Tooltip("対象のデバイス面。表の中で重複させない。")]
        public TaskSurface surface;

        [Tooltip("この面に出るタスク種別。上から順に繰り返し出る。空にするとこの面にはタスクが出ない。")]
        public TaskKind[] kinds = Array.Empty<TaskKind>();
    }

    [Tooltip("デバイス面ごとの出現タスク。")]
    [SerializeField] private SurfaceEntry[] surfaces = Array.Empty<SurfaceEntry>();

    /// <summary>指定した面に出る種別を返す。1 件も無い面では false を返す。</summary>
    public bool TryGetKinds(TaskSurface surface, out TaskKind[] kinds)
    {
        foreach (var entry in surfaces)
        {
            if (entry != null && entry.surface == surface && entry.kinds != null && entry.kinds.Length > 0)
            {
                kinds = entry.kinds;
                return true;
            }
        }

        kinds = Array.Empty<TaskKind>();
        return false;
    }

    public bool HasAnyKind(TaskSurface surface) => TryGetKinds(surface, out _);

    /// <summary>表の内容を検証する。開始時に一度だけ呼び、不備をすべて列挙する。</summary>
    /// <param name="catalog">登録されていない種別を検出するための照合先。</param>
    public bool Validate(MiniGameCatalog catalog)
    {
        if (surfaces == null || surfaces.Length == 0)
        {
            Debug.LogError("TaskSpawnTable (" + name + "): 行が 1 つもありません。", this);
            return false;
        }

        var valid = true;
        var anyKind = false;

        for (var i = 0; i < surfaces.Length; i++)
        {
            var entry = surfaces[i];
            if (entry == null)
            {
                Debug.LogError("TaskSpawnTable (" + name + "): surfaces[" + i + "] が空です。", this);
                valid = false;
                continue;
            }

            for (var j = i + 1; j < surfaces.Length; j++)
            {
                if (surfaces[j] != null && surfaces[j].surface == entry.surface)
                {
                    Debug.LogError("TaskSpawnTable (" + name + "): 面が重複しています -> " + entry.surface, this);
                    valid = false;
                }
            }

            if (entry.kinds == null || entry.kinds.Length == 0)
            {
                // 意図的に空にする場合もあるため、停止はさせずに気づけるようにする。
                Debug.LogWarning("TaskSpawnTable (" + name + "): " + entry.surface + " にはタスクが出ません。", this);
                continue;
            }

            anyKind = true;
            foreach (var kind in entry.kinds)
            {
                if (catalog != null && !catalog.TryGetEntry(kind, out _))
                {
                    Debug.LogError(
                        "TaskSpawnTable (" + name + "): " + entry.surface + " の " + kind
                        + " が MiniGameCatalog に登録されていません。", this);
                    valid = false;
                }
            }
        }

        if (!anyKind)
        {
            Debug.LogError("TaskSpawnTable (" + name + "): どの面にもタスクが出ません。", this);
            valid = false;
        }

        return valid;
    }
}
