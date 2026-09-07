using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MonsterCombatPresentationVerification
{
    const string Key = "MonsterCombatPresentationVerification.Running";
    static float m_startTime;

    static MonsterCombatPresentationVerification()
    {
        if (SessionState.GetBool(Key, false)) Register();
    }

    public static void Run()
    {
        VerifyDamageFormula();
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
            GameObject spawnRoot = GameObject.Find("MonsterSpawnList");
            Check(spawnRoot != null, "MonsterSpawnList missing");
            MonsterCombatStatus[] statusList = spawnRoot.GetComponentsInChildren<MonsterCombatStatus>(true);
            Check(statusList.Length == 4, "four MonsterCombatStatus components required");
            for (int i = 0; i < statusList.Length; ++i)
            {
                MonsterCombatStatus status = statusList[i];
                MonsterWorldUI worldUI = status.GetComponentInChildren<MonsterWorldUI>(true);
                Check(worldUI != null, "MonsterWorldUI missing");
                Check(status.GetLevel() > 0 && status.GetMaxHP() > 0, "monster status is not initialized");
                MonsterWander wander = status.GetComponent<MonsterWander>();
                Check(wander == null || !wander.enabled, "dummy monster movement enabled");
            }

            MonsterCombatStatus target = statusList[0];
            uint oldHP = target.GetHP();
            target.PlayPredictedHit(37);
            Check(target.GetHP() == oldHP, "predicted hit changed authoritative HP");
            Check(UnityEngine.Object.FindObjectOfType<DamageNumberEffect>() != null, "predicted damage number missing");
            target.ApplyServerHP(oldHP - 10);
            Check(target.GetHP() == oldHP - 10, "server HP result was not applied");
            Image hpFill = target.GetComponentInChildren<MonsterWorldUI>(true).transform.Find("Panel/HPBackground/HPFill").GetComponent<Image>();
            Check(hpFill.fillAmount < 1.0f, "HP bar did not refresh from server HP");

            Debug.Log("[MonsterCombatPresentationVerification] ALL PASSED");
            EditorApplication.ExitPlaymode();
        }
        catch (Exception exception)
        {
            Debug.LogError("[MonsterCombatPresentationVerification] FAIL " + exception);
            SessionState.SetBool(Key, false);
            EditorApplication.Exit(1);
        }
    }

    static void VerifyDamageFormula()
    {
        for (int level = 1; level <= CombatDamageCalculator.MaxLevel; ++level)
        {
            int warriorBasic = CombatDamageCalculator.Calculate(ePlayerClass.Warrior, level, eCombatAttackType.Basic);
            int warriorSkill = CombatDamageCalculator.Calculate(ePlayerClass.Warrior, level, eCombatAttackType.SkillA);
            CombatDamageCalculator.SplitWarriorSkillA(warriorSkill, out int firstDamage, out int secondDamage);
            Check(warriorBasic > 0, "warrior damage must be positive");
            Check(firstDamage + secondDamage == warriorSkill, "warrior two-hit presentation changed total damage");
            Check(CombatDamageCalculator.Calculate(ePlayerClass.Archer, level, eCombatAttackType.Basic) > 0,
                "archer damage must be positive");
            Check(CombatDamageCalculator.Calculate(ePlayerClass.Archer, level, eCombatAttackType.RapidShot) > 0,
                "rapid damage must be positive");
        }
    }

    static void Check(bool _result, string _message)
    {
        if (!_result) throw new Exception(_message);
    }
}
