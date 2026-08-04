using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene 上の View が、必須参照の不足をまとめて報告するための補助。</summary>
public static class SceneUiValidation
{
    /// <summary>未設定の参照があれば名前を列挙して報告し、false を返す。</summary>
    public static bool Require(MonoBehaviour owner, params (string Name, Object Reference)[] references)
    {
        List<string> missing = null;
        foreach (var reference in references)
        {
            if (reference.Reference == null)
            {
                (missing ??= new List<string>()).Add(reference.Name);
            }
        }

        if (missing == null)
        {
            return true;
        }

        Debug.LogError(
            owner.GetType().Name + " (" + owner.name + "): Inspector の参照が未設定です -> " + string.Join(", ", missing),
            owner);
        return false;
    }
}
