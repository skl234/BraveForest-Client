using System;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// 명시적으로 실행할 때만 동작합니다. 씬을 열거나 게임을 실행할 때 UI/오브젝트를 생성하지 않습니다.
public static class OfflineCombatSetup
{
    const string Root = "Assets/MYFolder/Prefab/Object/";
    const string ScenePath = "Assets/MYFolder/Scenes/BeginnerZoneScene.unity";

    public static void Run()
    {
        Directory.CreateDirectory("Build/Validation/Backups");
        File.Copy(ScenePath, "Build/Validation/Backups/BeginnerZoneScene_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
        Directory.CreateDirectory(Root + "Effect");
        Directory.CreateDirectory("Assets/MYFolder/Animation/Combat");
        AssetDatabase.Refresh();
        RuntimeAnimatorController controller = CreateController();
        GameObject areaEffect = CreateEffect();
        string playerPath = Root + "Player/LocalPlayerObject_Warrior.prefab";
        GameObject playerPrefab = PrefabUtility.LoadPrefabContents(playerPath);
        playerPrefab.layer = LayerMask.NameToLayer("Player");
        PlayerCombat combat = playerPrefab.GetComponent<PlayerCombat>();
        if (combat == null) combat = playerPrefab.AddComponent<PlayerCombat>();
        PlayerAttackEffects effects = playerPrefab.GetComponent<PlayerAttackEffects>();
        if (effects == null) effects = playerPrefab.AddComponent<PlayerAttackEffects>();
        SetReference(effects, "m_areaEffectPrefab", areaEffect);
        SetReference(playerPrefab.GetComponent<LocalPlayerObject>(), "m_gameplayController", controller);
        PrefabUtility.SaveAsPrefabAsset(playerPrefab, playerPath);
        PrefabUtility.UnloadPrefabContents(playerPrefab);

        string[] names = { "Ghost", "Lizard", "Knight", "Rat", "Demon" };
        for (int i = 0; i < names.Length; ++i) SetupMonster(names[i]);
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        LocalPlayerObject player = UnityEngine.Object.FindObjectOfType<LocalPlayerObject>();
        if (player == null) throw new Exception("LocalPlayerObject missing");
        // 기존 씬 인스턴스에 남아 있는 override도 동일하게 맞춥니다.
        player.gameObject.layer = LayerMask.NameToLayer("Player");
        PlayerCombat sceneCombat = player.GetComponent<PlayerCombat>();
        if (sceneCombat == null) sceneCombat = player.gameObject.AddComponent<PlayerCombat>();
        PlayerAttackEffects sceneEffects = player.GetComponent<PlayerAttackEffects>();
        if (sceneEffects == null) sceneEffects = player.gameObject.AddComponent<PlayerAttackEffects>();
        SetReference(sceneEffects, "m_areaEffectPrefab", areaEffect);
        SetReference(player, "m_gameplayController", controller);
        Camera camera = Camera.main;
        CameraController oldCamera = camera.GetComponent<CameraController>();
        if (oldCamera != null) oldCamera.enabled = false;
        PlayerFollowCamera follow = camera.GetComponent<PlayerFollowCamera>();
        if (follow == null) follow = camera.gameObject.AddComponent<PlayerFollowCamera>();
        SetReference(follow, "m_target", player.transform);
        camera.fieldOfView = 45.0f;
        camera.nearClipPlane = 0.1f;
        Transform parent = player.transform.parent;
        Vector3[] offsets = { new Vector3(6, 0, 0), new Vector3(7, 0, 3), new Vector3(4, 0, 5) };
        for (int i = 0; i < offsets.Length; ++i)
        {
            string name = "CombatTest_Rat_" + (i + 1);
            Transform existing = parent.Find(name);
            GameObject monster = existing != null ? existing.gameObject :
                (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Monster/MonsterObject_Rat.prefab"), parent);
            monster.name = name;
            Vector3 wanted = player.transform.position + offsets[i];
            if (!NavMesh.SamplePosition(wanted, out NavMeshHit hit, 4.0f, NavMesh.AllAreas)) throw new Exception("No NavMesh: " + wanted);
            monster.transform.position = hit.position;
            monster.transform.rotation = Quaternion.Euler(0, 180, 0);
            Debug.Log("[CombatSetup] " + name + " " + hit.position);
        }
        GameObject client = GameObject.Find("Client");
        if (client != null) client.SetActive(false);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[CombatSetup] COMPLETE");
    }

    static RuntimeAnimatorController CreateController()
    {
        string path = "Assets/MYFolder/Animation/Combat/WarriorCombat.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) return controller;
        RuntimeAnimatorController source = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/ModularRPGHeroesPolyArt/Animators/SwordShield.controller");
        if (source == null) throw new Exception("SwordShield controller missing");
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        string[] names = { "Idle_SwordShield", "Run_SwordShield", "NormalAttack01_SwordShield", "NormalAttack02_SwordShield", "JumpAttack_SwordShield" };
        AnimationClip[] clips = source.animationClips;
        for (int i = 0; i < names.Length; ++i)
        {
            AnimationClip clip = null;
            for (int j = 0; j < clips.Length; ++j) if (clips[j].name == names[i]) { clip = clips[j]; break; }
            if (clip == null) throw new Exception("Clip missing: " + names[i]);
            AnimatorState state = controller.layers[0].stateMachine.AddState(names[i]);
            state.motion = clip;
            if (i == 0) controller.layers[0].stateMachine.defaultState = state;
            Debug.Log("[CombatSetup] Clip " + names[i] + " length=" + clip.length);
        }
        return controller;
    }

    public static GameObject CreateEffect()
    {
        string path = Root + "Effect/WarriorAreaFire.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Matthew Guz/Hits Effects FREE/Prefab/Fire Hit .prefab");
        if (source == null) throw new Exception("Fire asset missing");
        GameObject root = new GameObject("WarriorAreaFire");
        root.AddComponent<AreaFireEffect>();
        for (int row = -4; row <= 4; ++row)
        {
            for (int col = -4; col <= 4; ++col)
            {
                Vector3 position = new Vector3((col + ((row & 1) == 0 ? 0 : 0.5f)) * 1.15f, 0.1f, row * 1.0f);
                if (new Vector2(position.x, position.z).magnitude > 4.65f) continue;
                GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                child.name = "Fire_" + row + "_" + col;
                child.transform.localPosition = position;
                child.transform.localScale = new Vector3(1.8f, 1.25f, 1.8f);
            }
        }
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    static void SetupMonster(string _name)
    {
        string path = Root + "Monster/MonsterObject_" + _name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Transform old = root.transform.Find("Hitbox");
        if (old == null)
        {
            GameObject hitbox = new GameObject("Hitbox");
            hitbox.transform.SetParent(root.transform, false);
            hitbox.tag = "MonsterHitbox";
            SphereCollider collider = hitbox.AddComponent<SphereCollider>();
            collider.radius = 1.0f;
            collider.center = Vector3.up * 0.8f;
            collider.isTrigger = true;
            MonsterHitbox target = hitbox.AddComponent<MonsterHitbox>();
            GameObject ring = new GameObject("TargetRing");
            ring.transform.SetParent(hitbox.transform, false);
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.widthMultiplier = 0.075f;
            string materialPath = Root + "Effect/TargetRing.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Sprites/Default"));
                material.color = new Color(1.0f, 0.65f, 0.05f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            line.sharedMaterial = material;
            for (int i = 0; i < 48; ++i)
            {
                float angle = i * Mathf.PI * 2.0f / 48;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0.09f, Mathf.Sin(angle)));
            }
            ring.SetActive(false);
            SetReference(target, "m_targetRing", ring);
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void SetReference(UnityEngine.Object _object, string _field, UnityEngine.Object _value)
    {
        SerializedObject serialized = new SerializedObject(_object);
        serialized.FindProperty(_field).objectReferenceValue = _value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
