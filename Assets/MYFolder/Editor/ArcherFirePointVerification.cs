using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ArcherFirePointVerification
{
    const string Key = "ArcherFirePointVerification.Running";
    static float m_startTime;

    static ArcherFirePointVerification()
    {
        if (SessionState.GetBool(Key, false)) Register();
    }

    public static void Run()
    {
        SessionState.SetBool(Key, true);
        EditorSceneManager.OpenScene("Assets/MYFolder/Scenes/BeginnerZoneScene.unity", OpenSceneMode.Single);
        Register();
        EditorApplication.EnterPlaymode();
    }

    static void Register()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    static void OnPlayModeChanged(PlayModeStateChange _state)
    {
        if (_state == PlayModeStateChange.EnteredPlayMode) m_startTime = Time.realtimeSinceStartup;
        if (_state != PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetBool(Key, false);
        EditorApplication.Exit(0);
    }

    static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (Time.realtimeSinceStartup - m_startTime < 1.0f) return;

        try
        {
            OfflineCombatSession session = UnityEngine.Object.FindObjectOfType<OfflineCombatSession>();
            Check(session != null, "OfflineCombatSession missing");
            session.ChooseArcher();
            LocalPlayerObject player = session.GetPlayer();
            Check(player != null && player.GetPlayerClass() == ePlayerClass.Archer, "Archer player missing");

            Transform arrow = FindActiveArrow(player.transform);
            Check(arrow != null, "active Arrow1/Arrow2 missing after runtime character creation");
            Renderer[] rendererList = arrow.GetComponentsInChildren<Renderer>(true);
            Check(rendererList.Length > 0, "arrow renderer missing");

            MonsterHitbox target = UnityEngine.Object.FindObjectOfType<MonsterHitbox>();
            Check(target != null, "test target missing");
            Vector3 targetPosition = target.GetCenter();
            Bounds bounds = rendererList[0].bounds;
            for (int i = 1; i < rendererList.Length; ++i) bounds.Encapsulate(rendererList[i].bounds);

            PlayerAttackEffects effects = player.GetComponent<PlayerAttackEffects>();
            Check(effects != null, "PlayerAttackEffects missing");
            Vector3 start = effects.GetArrowStart(targetPosition);
            Vector3 expected = bounds.ClosestPoint(targetPosition);
            Check(Vector3.Distance(start, expected) < 0.001f, "trajectory does not start at animated arrow renderer edge");

            Debug.Log("[ArcherFirePointVerification] ALL PASSED arrow=" + arrow.name + " start=" + start);
            EditorApplication.ExitPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError("[ArcherFirePointVerification] FAIL " + exception);
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(1);
        }
    }

    static Transform FindActiveArrow(Transform _root)
    {
        Transform[] transformList = _root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transformList.Length; ++i)
        {
            if (transformList[i].name != "Arrow1" && transformList[i].name != "Arrow2") continue;
            if (transformList[i].gameObject.activeInHierarchy) return transformList[i];
        }
        return null;
    }

    static void Check(bool _result, string _message)
    {
        if (!_result) throw new Exception(_message);
    }
}
