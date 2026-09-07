using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 궁수 즉시 명중 궤적을 프리팹에 미리 구성합니다. 일반 플레이 중에는 실행되지 않습니다.
public static class ArcherEffectSetup
{
    const string Root = "Assets/MYFolder/Prefab/Object/Effect/";

    public static void Run()
    {
        Directory.CreateDirectory("Build/Validation/Backups");
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string[] names = { "ArrowBasic", "ArrowPower", "ArrowRapid" };
        for (int i = 0; i < names.Length; ++i)
        {
            string path = Root + names[i] + ".prefab";
            File.Copy(path, "Build/Validation/Backups/BeforeArcherVFX_" + stamp + "_" + names[i] + ".prefab");
        }

        string materialPath = Root + "ArrowTrajectory.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, materialPath); }
        material.color = Color.white;
        EditorUtility.SetDirty(material);
        Create("ArrowBasic", material, 0.18f,
            new Color(0.94f, 1.00f, 0.78f), new Color(0.48f, 0.82f, 0.42f),
            new float[] { 0, -0.07f, 0.07f }, new float[] { 0.045f, 0.018f, 0.018f }, 0, 0.055f);
        Create("ArrowPower", material, 0.24f,
            new Color(1.00f, 0.78f, 0.22f), new Color(1.00f, 0.94f, 0.62f),
            new float[] { 0, -0.08f, 0.08f, -0.15f, 0.15f }, new float[] { 0.075f, 0.028f, 0.028f, 0.016f, 0.016f }, 1, 0.085f);
        Create("ArrowRapid", material, 0.15f,
            new Color(0.72f, 0.96f, 1.00f), new Color(0.25f, 0.62f, 0.92f),
            new float[] { 0, -0.06f, 0.06f, -0.12f, 0.12f }, new float[] { 0.040f, 0.020f, 0.020f, 0.012f, 0.012f }, 2, 0.060f);
        AssetDatabase.SaveAssets();
        Debug.Log("[ArcherEffectSetup] COMPLETE");
    }

    static void Create(string _name, Material _material, float _duration, Color _core, Color _edge,
        float[] _offsets, float[] _widths, int _style, float _curveStrength)
    {
        GameObject root = new GameObject(_name);
        ArrowShotEffect effect = root.AddComponent<ArrowShotEffect>();
        LineRenderer[] streaks = new LineRenderer[_offsets.Length];
        for (int i = 0; i < streaks.Length; ++i)
        {
            GameObject child = new GameObject(i == 0 ? "Core" : "SpeedStreak_" + i);
            child.transform.SetParent(root.transform, false);
            Color color = i == 0 ? _core : Color.Lerp(_core, _edge, 0.82f);
            streaks[i] = Line(child, _material, _widths[i], color, false, 2);
            streaks[i].sortingOrder = i == 0 ? 2 : 1;
            streaks[i].widthCurve = new AnimationCurve(new Keyframe(0, 0.18f), new Keyframe(0.18f, 1), new Keyframe(0.82f, 1), new Keyframe(1, 0.08f));
        }
        SerializedObject serialized = new SerializedObject(effect);
        serialized.FindProperty("m_trail").objectReferenceValue = streaks[0];
        SetArray(serialized.FindProperty("m_streaks"), streaks);
        SerializedProperty offsets = serialized.FindProperty("m_streakOffsets"); offsets.arraySize = _offsets.Length;
        for (int i = 0; i < _offsets.Length; ++i) offsets.GetArrayElementAtIndex(i).floatValue = _offsets[i];
        serialized.FindProperty("m_style").enumValueIndex = _style;
        serialized.FindProperty("m_curveStrength").floatValue = _curveStrength;
        serialized.FindProperty("m_duration").floatValue = _duration;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, Root + _name + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    static LineRenderer Line(GameObject _object, Material _material, float _width, Color _color, bool _loop, int _positions)
    {
        LineRenderer line = _object.AddComponent<LineRenderer>();
        line.useWorldSpace = true; line.positionCount = _positions; line.loop = _loop;
        line.sharedMaterial = _material; line.widthMultiplier = _width;
        line.numCapVertices = 4; line.numCornerVertices = 2;
        line.startColor = _color; line.endColor = Color.Lerp(_color, Color.white, 0.08f);
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    static void SetArray(SerializedProperty _property, LineRenderer[] _values)
    {
        _property.arraySize = _values.Length;
        for (int i = 0; i < _values.Length; ++i) _property.GetArrayElementAtIndex(i).objectReferenceValue = _values[i];
    }
}
