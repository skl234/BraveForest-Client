using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CombatSyncSetup
{
    const string Root = "Assets/MYFolder/";

    public static void Run()
    {
        string backup = "Build/Validation/CombatSyncBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(backup);
        string[] players = { "LocalPlayerObject_Warrior", "LocalPlayerObject_Archer", "RemotePlayerObject_Warrior", "RemotePlayerObject_Archer" };
        string[] monsters = { "Ghost", "Lizard", "Knight", "Rat" };
        string[] folders = { "Specter", "Lizard Warrior", "Black Knight", "Rat Assassin" };
        for (int i = 0; i < players.Length; ++i) SetupFeedback(Root + "Prefab/Object/Player/" + players[i] + ".prefab", true, backup);
        for (int i = 0; i < monsters.Length; ++i)
        {
            SetupFeedback(Root + "Prefab/Object/Monster/MonsterObject_" + monsters[i] + ".prefab", false, backup);
            string path = Root + "Animation/Monster/" + monsters[i] + "Wander.controller";
            File.Copy(path, backup + "/" + Path.GetFileName(path));
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimationClip attack = CopyClip(monsters[i] + "Attack", "Assets/RPGMonsterWave02Polyart/Animations/" + folders[i] + "/Attack01.fbx");
            AnimationClip dead = CopyClip(monsters[i] + "Dead", "Assets/RPGMonsterWave02Polyart/Animations/" + folders[i] + "/Die.fbx");
            SetState(machine, "Attack", attack);
            SetState(machine, "Dead", dead);
            EditorUtility.SetDirty(controller);
        }
        string numberPath = Root + "Prefab/UI/Monster/DamageNumberEffect.prefab";
        File.Copy(numberPath, backup + "/DamageNumberEffect.prefab");
        GameObject number = PrefabUtility.LoadPrefabContents(numberPath);
        TMP_Text text = number.GetComponent<TMP_Text>();
        string[] fonts = AssetDatabase.FindAssets("Maple t:TMP_FontAsset");
        if (fonts.Length > 0) text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(fonts[0]));
        text.outlineWidth = 0.22f;
        PrefabUtility.SaveAsPrefabAsset(number, numberPath);
        PrefabUtility.UnloadPrefabContents(number);
        AssetDatabase.SaveAssets();
        Debug.Log("[CombatSyncSetup] COMPLETE shared feedback: 4 players + 4 monsters, real attack/death clips. Backup=" + backup);
    }

    static void SetupFeedback(string _path, bool _player, string _backup)
    {
        File.Copy(_path, _backup + "/" + Path.GetFileName(_path));
        GameObject root = PrefabUtility.LoadPrefabContents(_path);
        CombatHitFeedback feedback = root.GetComponent<CombatHitFeedback>();
        if (feedback == null) feedback = root.AddComponent<CombatHitFeedback>();
        SerializedObject data = new SerializedObject(feedback);
        data.FindProperty("m_hitEffectPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Travis Game Assets/Hit Impact Effects/Prefabs/Hits/Hit_02.prefab");
        data.FindProperty("m_damageNumberPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/UI/Monster/DamageNumberEffect.prefab").GetComponent<DamageNumberEffect>();
        Color color = new Color(1, 0.82f, 0.18f, 1);
        if (_player) color = new Color(1, 0.25f, 0.2f, 1);
        data.FindProperty("m_damageColor").colorValue = color;
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, _path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static AnimationClip CopyClip(string _name, string _source)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(_source);
        AnimationClip source = null;
        for (int i = 0; i < assets.Length; ++i)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip != null && !clip.name.StartsWith("__preview__")) { source = clip; break; }
        }
        if (source == null) throw new Exception("Animation missing: " + _source);
        string path = Root + "Animation/Monster/" + _name + ".anim";
        AnimationClip result = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (result == null) { result = new AnimationClip(); AssetDatabase.CreateAsset(result, path); }
        EditorUtility.CopySerialized(source, result);
        result.name = _name;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(result);
        settings.loopTime = false; settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(result, settings);
        AnimationUtility.SetAnimationEvents(result, new AnimationEvent[0]);
        EditorUtility.SetDirty(result);
        return result;
    }

    static void SetState(AnimatorStateMachine _machine, string _name, AnimationClip _clip)
    {
        AnimatorState state = null;
        ChildAnimatorState[] states = _machine.states;
        for (int i = 0; i < states.Length; ++i) if (states[i].state.name == _name) state = states[i].state;
        if (state == null) state = _machine.AddState(_name);
        state.motion = _clip; state.transitions = new AnimatorStateTransition[0];
    }
}
