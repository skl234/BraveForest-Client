using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// 테스트 몬스터 배회 컴포넌트와 미리 만들어진 토글 버튼을 프리팹에 저장합니다.
public static class MonsterMovementSetup
{
    const string Root = "Assets/MYFolder/";

    public static void Run()
    {
        Directory.CreateDirectory("Build/Validation/Backups");
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(Root + "Animation/Monster");
        AssetDatabase.Refresh();
        string scenePath = Root + "Scenes/BeginnerZoneScene.unity";
        File.Copy(scenePath, "Build/Validation/Backups/BeforeMonsterMovement_" + stamp + ".unity");
        string[] names = { "Ghost", "Lizard", "Knight", "Rat", "Demon" };
        for (int i = 0; i < names.Length; ++i) SetupMonster(Root + "Prefab/Object/Monster/MonsterObject_" + names[i] + ".prefab", names[i]);
        SetupInterface(Root + "Prefab/UI/CombatInterface.prefab");
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        GameObject canvas = GameObject.Find("CombatCanvas");
        if (canvas != null && PrefabUtility.IsPartOfPrefabInstance(canvas))
            PrefabUtility.RevertPrefabInstance(canvas, InteractionMode.AutomatedAction);
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log("[MonsterMovementSetup] COMPLETE");
    }

    static void SetupMonster(string _path, string _name)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(_path);
        NavMeshAgent agent = root.GetComponent<NavMeshAgent>();
        if (agent == null) agent = root.AddComponent<NavMeshAgent>();
        agent.radius = 0.65f; agent.height = 1.8f; agent.speed = 6.0f;
        agent.angularSpeed = 720.0f; agent.acceleration = 100.0f; agent.stoppingDistance = 0.05f;
        if (root.GetComponent<MonsterWander>() == null) root.AddComponent<MonsterWander>();
        SerializedObject monster = new SerializedObject(root.GetComponent<MonsterObject>());
        monster.FindProperty("m_wanderController").objectReferenceValue = CreateController(_name);
        monster.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, _path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static RuntimeAnimatorController CreateController(string _name)
    {
        string controllerPath = Root + "Animation/Monster/" + _name + "Wander.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        stateMachine.states = new ChildAnimatorState[0];
        stateMachine.anyStateTransitions = new AnimatorStateTransition[0];
        stateMachine.entryTransitions = new AnimatorTransition[0];
        AnimatorState idle = stateMachine.AddState("Idle"); idle.motion = Clip(_name, "IdleNormal");
        AnimatorState move = stateMachine.AddState("Move"); move.motion = Clip(_name, _name == "Rat" ? "Run" : "WalkFWD");
        AnimatorState dead = stateMachine.AddState("Dead"); dead.motion = Clip(_name, "Die");
        stateMachine.defaultState = idle;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static AnimationClip Clip(string _name, string _clip)
    {
        string folder = _name == "Ghost" ? "Specter" : _name == "Lizard" ? "Lizard Warrior" : _name == "Knight" ? "Black Knight" : _name == "Rat" ? "Rat Assassin" : "Flying Demon";
        string path = "Assets/RPGMonsterWave02Polyart/Animations/" + folder + "/" + _clip + ".fbx";
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; ++i)
        {
            AnimationClip clip = assets[i] as AnimationClip;
            if (clip != null && clip.name == _clip) return clip;
        }
        throw new Exception("Animation clip missing: " + path + " / " + _clip);
    }

    static void SetupInterface(string _path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(_path);
        OfflineCombatSession session = root.GetComponent<OfflineCombatSession>();
        RectTransform tools = root.transform.Find("HUD/TestTools") as RectTransform;
        tools.anchoredPosition = new Vector2(-112, -119); tools.sizeDelta = new Vector2(194, 202);
        SetPosition(tools.Find("Label"), 77);
        SetPosition(tools.Find("KillTarget"), 2);
        SetPosition(tools.Find("ResetMonsters"), -36);
        SetPosition(tools.Find("ChangeClass"), -74);
        Transform movement = tools.Find("MonsterMovement");
        if (movement == null)
        {
            movement = UnityEngine.Object.Instantiate(tools.Find("ResetMonsters").gameObject, tools).transform;
            movement.name = "MonsterMovement";
        }
        SetPosition(movement, 40);
        Image image = movement.GetComponent<Image>(); image.color = new Color(.24f, .32f, .22f);
        TMP_Text label = movement.Find("Label").GetComponent<TMP_Text>(); label.text = "몬스터 이동";
        Button button = movement.GetComponent<Button>();
        while (button.onClick.GetPersistentEventCount() > 0) UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, session.ToggleMonsterMovement);
        SerializedObject serialized = new SerializedObject(session);
        serialized.FindProperty("m_monsterMovementLabel").objectReferenceValue = label;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, _path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void SetPosition(Transform _transform, float _y)
    {
        RectTransform rect = _transform as RectTransform;
        rect.anchoredPosition = new Vector2(0, _y);
    }
}
