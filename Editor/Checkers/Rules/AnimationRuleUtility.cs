using UnityEditor;
using UnityEngine;

namespace majsoul.tools.assetscoretool.editor
{
    /// <summary>
    /// 提供 AnimationClip 曲线统计的只读辅助方法。
    /// </summary>
    internal static class AnimationRuleUtility
    {
        internal static int GetCurveCount(AnimationClip clip)
        {
            if (clip == null)
            {
                return 0;
            }

            return AnimationUtility.GetCurveBindings(clip).Length +
                   AnimationUtility.GetObjectReferenceCurveBindings(clip)
                       .Length;
        }

        internal static int GetKeyframeCount(AnimationClip clip)
        {
            if (clip == null)
            {
                return 0;
            }

            int keyframeCount = 0;
            EditorCurveBinding[] curveBindings =
                AnimationUtility.GetCurveBindings(clip);

            for (int i = 0; i < curveBindings.Length; i++)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(
                    clip,
                    curveBindings[i]);

                if (curve != null)
                {
                    keyframeCount += curve.length;
                }
            }

            EditorCurveBinding[] objectReferenceBindings =
                AnimationUtility.GetObjectReferenceCurveBindings(clip);

            for (int i = 0; i < objectReferenceBindings.Length; i++)
            {
                keyframeCount += AnimationUtility.GetObjectReferenceCurve(
                    clip,
                    objectReferenceBindings[i]).Length;
            }

            return keyframeCount;
        }
    }
}