using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

public static class ZoneFieldDataSetup
{
    const string Root = "Assets/MYFolder/";
    const string ScenePath = Root + "Scenes/BeginnerZoneScene.unity";
    const string MonsterPrefabRoot = Root + "Prefab/Object/Monster/";
    const string ToolPrefabRoot = Root + "Prefab/FieldData/";

    public static void Run()
    {
        Directory.CreateDirectory("Build/Validation/Backups");
        File.Copy(ScenePath,
            "Build/Validation/Backups/BeforeZoneFieldData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
        Directory.CreateDirectory(ToolPrefabRoot);
        Directory.CreateDirectory(Root + "ExportedFieldData");
        AssetDatabase.Refresh();

        SetupArcherFirePoint();
        CreateMonsterCombatPresentation();
        SetupMonsterPrefab("Ghost", eMonsterType.Ghost, 10, 420, 260, 25, 0.70f, 4.0f, 30.0f, 1.5f, 7.0f);
        SetupMonsterPrefab("Lizard", eMonsterType.Lizard, 6, 260, 180, 17, 0.78f, 4.0f, 30.0f, 1.5f, 6.0f);
        SetupMonsterPrefab("Knight", eMonsterType.Knight, 4, 260, 38, 13, 0.88f, 4.0f, 30.0f, 1.5f, 7.0f);
        SetupMonsterPrefab("Rat", eMonsterType.Rat, 1, 90, 60, 7, 0.62f, 3.0f, 30.0f, 1.5f, 5.0f);
        CreateToolPrefabs();
        SetupScene();

        AssetDatabase.SaveAssets();
        Debug.Log("[ZoneFieldDataSetup] COMPLETE");
    }

    static void SetupArcherFirePoint()
    {
        string path = Root + "Prefab/Object/Player/LocalPlayerObject_Archer.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        PlayerAttackEffects effects = root.GetComponent<PlayerAttackEffects>();
        if (effects == null) effects = root.AddComponent<PlayerAttackEffects>();
        SerializedObject serialized = new SerializedObject(effects);
        serialized.FindProperty("m_arrowFirePoint").objectReferenceValue = null;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[ZoneFieldDataSetup] Archer fire point resolves from the animated Arrow1/Arrow2 renderer at shot time");
    }

    static void SetupMonsterPrefab(string _name, eMonsterType _type, uint _level,
        uint _maxHP, uint _experience, uint _attackPower, float _hitboxRadius, float _wanderRadius,
        float _maxAggroRange, float _respawnRadius, float _respawnDelay)
    {
        string path = MonsterPrefabRoot + "MonsterObject_" + _name + ".prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        MonsterSpawnPoint spawnPoint = root.GetComponent<MonsterSpawnPoint>();
        if (spawnPoint == null) spawnPoint = root.AddComponent<MonsterSpawnPoint>();
        spawnPoint.SetEditorValues(_type, _level, _maxHP, _experience, _attackPower, _hitboxRadius,
            _wanderRadius, _maxAggroRange, _respawnRadius, _respawnDelay);
		bool aggressive = _type == eMonsterType.Knight;
		float moveSpeed = 4.0f;
		uint attackIntervalMs = 2200;
		if (_type == eMonsterType.Lizard)
		{
			moveSpeed = 4.2f;
			attackIntervalMs = 2000;
		}
		spawnPoint.SetCombatEditorValues(aggressive, 6.0f, 1.6f, moveSpeed, attackIntervalMs);

        MonsterHitbox hitbox = root.GetComponentInChildren<MonsterHitbox>(true);
        if (hitbox == null) throw new Exception("MonsterHitbox missing: " + path);
        SphereCollider sphere = hitbox.GetComponent<SphereCollider>();
        sphere.radius = _hitboxRadius;

        MonsterWander wander = root.GetComponent<MonsterWander>();
        if (wander != null) wander.enabled = false;

        MonsterObject monster = root.GetComponent<MonsterObject>();
        monster.SetState(eMonsterState.Idle);

        MonsterCombatStatus combatStatus = root.GetComponent<MonsterCombatStatus>();
        if (combatStatus == null) combatStatus = root.AddComponent<MonsterCombatStatus>();
        Transform oldUI = root.transform.Find("MonsterWorldUI");
        if (oldUI != null) UnityEngine.Object.DestroyImmediate(oldUI.gameObject);
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/UI/Monster/MonsterWorldUI.prefab");
        GameObject worldUI = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, root.transform);
        worldUI.name = "MonsterWorldUI";
        worldUI.transform.localPosition = Vector3.up * 2.45f;
        worldUI.transform.localRotation = Quaternion.identity;
        worldUI.transform.localScale = Vector3.one * 0.01f;

        SerializedObject statusSerialized = new SerializedObject(combatStatus);
        statusSerialized.FindProperty("m_worldUI").objectReferenceValue = worldUI.GetComponent<MonsterWorldUI>();
        statusSerialized.FindProperty("m_hitEffectPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Travis Game Assets/Hit Impact Effects/Prefabs/Hits/Hit_02.prefab");
        GameObject damagePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/UI/Monster/DamageNumberEffect.prefab");
        statusSerialized.FindProperty("m_damageNumberPrefab").objectReferenceValue = damagePrefab.GetComponent<DamageNumberEffect>();
        statusSerialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void CreateMonsterCombatPresentation()
    {
        string directory = Root + "Prefab/UI/Monster/";
        Directory.CreateDirectory(directory);
        AssetDatabase.Refresh();

        GameObject damageRoot = new GameObject("DamageNumberEffect");
        TextMeshPro damageText = damageRoot.AddComponent<TextMeshPro>();
        damageText.alignment = TextAlignmentOptions.Center;
        damageText.fontSize = 7.5f;
        damageText.fontStyle = FontStyles.Bold;
        damageText.color = new Color(1.0f, 0.82f, 0.18f, 1.0f);
        damageText.outlineWidth = 0.25f;
        damageText.outlineColor = new Color32(60, 10, 0, 255);
        DamageNumberEffect damageEffect = damageRoot.AddComponent<DamageNumberEffect>();
        SerializedObject damageSerialized = new SerializedObject(damageEffect);
        damageSerialized.FindProperty("m_text").objectReferenceValue = damageText;
        damageSerialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(damageRoot, directory + "DamageNumberEffect.prefab");
        UnityEngine.Object.DestroyImmediate(damageRoot);

        GameObject uiRoot = new GameObject("MonsterWorldUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        RectTransform rootRect = uiRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(220.0f, 78.0f);
        Canvas canvas = uiRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        Image panel = CreateImage("Panel", uiRoot.transform, new Vector2(220.0f, 78.0f), new Color(0.03f, 0.04f, 0.05f, 0.72f));
        TMP_Text nameText = CreateText("Name", panel.transform, new Vector2(25.0f, 20.0f), new Vector2(135.0f, 28.0f), 20.0f, TextAlignmentOptions.Center);
        TMP_Text levelText = CreateText("Level", panel.transform, new Vector2(-80.0f, 20.0f), new Vector2(60.0f, 28.0f), 16.0f, TextAlignmentOptions.Center);
        Image hpBack = CreateImage("HPBackground", panel.transform, new Vector2(190.0f, 16.0f), new Color(0.08f, 0.08f, 0.08f, 0.95f));
        hpBack.rectTransform.anchoredPosition = new Vector2(0.0f, -19.0f);
        Image hpFill = CreateImage("HPFill", hpBack.transform, new Vector2(184.0f, 10.0f), new Color(0.78f, 0.08f, 0.08f, 1.0f));
        hpFill.type = Image.Type.Filled;
        hpFill.fillMethod = Image.FillMethod.Horizontal;
        hpFill.fillOrigin = 0;
        hpFill.fillAmount = 1.0f;

        MonsterWorldUI worldUI = uiRoot.AddComponent<MonsterWorldUI>();
        SerializedObject uiSerialized = new SerializedObject(worldUI);
        uiSerialized.FindProperty("m_nameText").objectReferenceValue = nameText;
        uiSerialized.FindProperty("m_levelText").objectReferenceValue = levelText;
        uiSerialized.FindProperty("m_hpFill").objectReferenceValue = hpFill;
        uiSerialized.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(uiRoot, directory + "MonsterWorldUI.prefab");
        UnityEngine.Object.DestroyImmediate(uiRoot);
    }

    static Image CreateImage(string _name, Transform _parent, Vector2 _size, Color _color)
    {
        GameObject gameObject = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        gameObject.transform.SetParent(_parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.sizeDelta = _size;
        Image image = gameObject.GetComponent<Image>();
        image.color = _color;
        return image;
    }

    static TMP_Text CreateText(string _name, Transform _parent, Vector2 _position, Vector2 _size,
        float _fontSize, TextAlignmentOptions _alignment)
    {
        GameObject gameObject = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(_parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchoredPosition = _position;
        rect.sizeDelta = _size;
        TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Maplestory Bold SDF.asset");
        text.fontSize = _fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = _alignment;
        text.color = Color.white;
        return text;
    }

    static void CreateToolPrefabs()
    {
        GameObject spawnRoot = new GameObject("MonsterSpawnExporter");
        spawnRoot.AddComponent<MonsterSpawnExporter>();
        PrefabUtility.SaveAsPrefabAsset(spawnRoot, ToolPrefabRoot + "MonsterSpawnExporter.prefab");
        UnityEngine.Object.DestroyImmediate(spawnRoot);

        GameObject fieldRoot = new GameObject("FieldDataExporter");
        fieldRoot.AddComponent<FieldDataExporter>();
        PrefabUtility.SaveAsPrefabAsset(fieldRoot, ToolPrefabRoot + "FieldDataExporter.prefab");
        UnityEngine.Object.DestroyImmediate(fieldRoot);
    }

    static void SetupScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Terrain terrain = UnityEngine.Object.FindObjectOfType<Terrain>();
        if (terrain == null) throw new Exception("Terrain missing in BeginnerZoneScene");

        GameObject field = GameObject.Find("Field");
        if (field == null) field = new GameObject("Field");
        Transform spawnRoot = field.transform.Find("MonsterSpawnList");
        if (spawnRoot == null)
        {
            GameObject list = new GameObject("MonsterSpawnList");
            spawnRoot = list.transform;
            spawnRoot.SetParent(field.transform, false);
        }
		spawnRoot.gameObject.SetActive(true);

        MonsterSpawnPoint[] oldList = spawnRoot.GetComponentsInChildren<MonsterSpawnPoint>(true);
        for (int i = 0; i < oldList.Length; ++i) UnityEngine.Object.DestroyImmediate(oldList[i].gameObject);

        string[] names = { "Ghost", "Lizard", "Knight", "Rat" };
        Vector3[] ratAreaList =
        {
            new Vector3(44.0f, 0.0f, 39.0f),
            new Vector3(38.0f, 0.0f, 83.0f),
            new Vector3(74.0f, 0.0f, 63.0f),
            new Vector3(47.0f, 0.0f, 119.0f),
            new Vector3(73.0f, 0.0f, 136.0f),
            new Vector3(91.0f, 0.0f, 85.0f),
            new Vector3(89.0f, 0.0f, 106.0f)
        };
        Vector3[] lizardAreaList =
        {
            new Vector3(128.0f, 0.0f, 156.0f),
            new Vector3(162.0f, 0.0f, 101.0f),
            new Vector3(145.0f, 0.0f, 48.0f),
            new Vector3(188.0f, 0.0f, 169.0f),
            new Vector3(177.0f, 0.0f, 115.0f),
            new Vector3(168.0f, 0.0f, 68.0f),
            new Vector3(108.0f, 0.0f, 158.0f),
            new Vector3(208.0f, 0.0f, 167.0f)
        };
        CreateSpawnGroup(spawnRoot, "Rat", ratAreaList, 70);
        CreateSpawnGroup(spawnRoot, "Lizard", lizardAreaList, 80);

        Transform tools = field.transform.Find("FieldDataTools");
        if (tools == null)
        {
            GameObject toolObject = new GameObject("FieldDataTools");
            tools = toolObject.transform;
            tools.SetParent(field.transform, false);
        }
        MonsterSpawnExporter spawnExporter = tools.GetComponent<MonsterSpawnExporter>();
        if (spawnExporter == null) spawnExporter = tools.gameObject.AddComponent<MonsterSpawnExporter>();
        FieldDataExporter fieldExporter = tools.GetComponent<FieldDataExporter>();
        if (fieldExporter == null) fieldExporter = tools.gameObject.AddComponent<FieldDataExporter>();
		ZoneSceneNetwork zoneNetwork = tools.GetComponent<ZoneSceneNetwork>();
		if (zoneNetwork == null) zoneNetwork = tools.gameObject.AddComponent<ZoneSceneNetwork>();
		Transform networkObjectRoot = field.transform.Find("NetworkObjectList");
		if (networkObjectRoot == null)
		{
			GameObject rootObject = new GameObject("NetworkObjectList");
			networkObjectRoot = rootObject.transform;
			networkObjectRoot.SetParent(field.transform, false);
		}

        SerializedObject spawnSerialized = new SerializedObject(spawnExporter);
        spawnSerialized.FindProperty("m_spawnRoot").objectReferenceValue = spawnRoot;
		spawnSerialized.FindProperty("m_terrain").objectReferenceValue = terrain;
        spawnSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject fieldSerialized = new SerializedObject(fieldExporter);
        fieldSerialized.FindProperty("m_terrain").objectReferenceValue = terrain;
        fieldSerialized.FindProperty("m_monsterSpawnExporter").objectReferenceValue = spawnExporter;
        fieldSerialized.FindProperty("m_obstacleMask").intValue = LayerMask.GetMask("Obstacle");
        fieldSerialized.ApplyModifiedPropertiesWithoutUndo();
		SerializedObject networkSerialized = new SerializedObject(zoneNetwork);
		networkSerialized.FindProperty("m_localWarriorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/Object/Player/LocalPlayerObject_Warrior.prefab");
		networkSerialized.FindProperty("m_localArcherPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/Object/Player/LocalPlayerObject_Archer.prefab");
		networkSerialized.FindProperty("m_remoteWarriorPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/Object/Player/RemotePlayerObject_Warrior.prefab");
		networkSerialized.FindProperty("m_remoteArcherPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/Object/Player/RemotePlayerObject_Archer.prefab");
		networkSerialized.FindProperty("m_objectRoot").objectReferenceValue = networkObjectRoot;
		SerializedProperty monsterPrefabList = networkSerialized.FindProperty("m_monsterPrefabList");
		monsterPrefabList.arraySize = names.Length;
		for (int i = 0; i < names.Length; ++i)
		{
			monsterPrefabList.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabRoot + "MonsterObject_" + names[i] + ".prefab");
		}
		networkSerialized.ApplyModifiedPropertiesWithoutUndo();

		MonsterSpawnPoint[] spawnPointList = spawnRoot.GetComponentsInChildren<MonsterSpawnPoint>(true);
		if (spawnPointList.Length != 150) throw new Exception("BeginnerZone monster count mismatch: " + spawnPointList.Length);
		for (int i = 0; i < spawnPointList.Length; ++i)
		{
			if (!fieldExporter.FindNearestWalkablePosition(spawnPointList[i].transform.position, out Vector3 position))
			{
				throw new Exception("Walkable monster spawn position was not found: " + spawnPointList[i].name);
			}
			spawnPointList[i].transform.position = position;
			if (spawnPointList[i].IsAggressive()) throw new Exception("BeginnerZone monster must be passive: " + spawnPointList[i].name);
			if (spawnPointList[i].GetMonsterType() == eMonsterType.Rat && spawnPointList[i].GetLevel() != 1) throw new Exception("Rat level mismatch");
			if (spawnPointList[i].GetMonsterType() == eMonsterType.Lizard && spawnPointList[i].GetLevel() != 6) throw new Exception("Lizard level mismatch");
		}

        fieldExporter.Export();
        spawnExporter.Export();
		CaptureSpawnPreview();
		spawnRoot.gameObject.SetActive(false);
		EditorSceneManager.MarkSceneDirty(field.scene);
		EditorSceneManager.SaveScene(field.scene);
		Debug.Log("[ZoneFieldDataSetup] BeginnerZone passive monster placement passed. Rat=70 Lizard=80 Total=150");
    }

	static void CreateSpawnGroup(Transform _spawnRoot, string _prefabName, Vector3[] _areaList, int _count)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabRoot + "MonsterObject_" + _prefabName + ".prefab");
		if (prefab == null) throw new Exception("Monster prefab missing: " + _prefabName);
		for (int i = 0; i < _count; ++i)
		{
			int areaIndex = i % _areaList.Length;
			int ringIndex = i / _areaList.Length;
			float angle = (i * 137.5f + areaIndex * 19.0f) * Mathf.Deg2Rad;
			float radius = 1.8f + (ringIndex % 5) * 1.15f;
			Vector3 wanted = _areaList[areaIndex] + new Vector3(Mathf.Cos(angle) * radius, 0.0f, Mathf.Sin(angle) * radius);
			if (!NavMesh.SamplePosition(wanted, out NavMeshHit hit, 8.0f, NavMesh.AllAreas))
			{
				throw new Exception("Walkable monster spawn position was not found: " + _prefabName + " " + i);
			}
			GameObject monster = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _spawnRoot);
			monster.name = "Spawn_" + _prefabName + "_" + i.ToString("000");
			monster.transform.position = hit.position;
			monster.transform.rotation = Quaternion.Euler(0.0f, (i * 47) % 360, 0.0f);
			MonsterWander wander = monster.GetComponent<MonsterWander>();
			if (wander != null) wander.enabled = false;
			MonsterObject monsterObject = monster.GetComponent<MonsterObject>();
			if (monsterObject != null) monsterObject.SetState(eMonsterState.Idle);
		}
	}

	static void CaptureSpawnPreview()
	{
		Directory.CreateDirectory("Build/Validation");
		GameObject markerRoot = new GameObject("MonsterSpawnCaptureMarkers");
		MonsterSpawnPoint[] spawnPointList = UnityEngine.Object.FindObjectsOfType<MonsterSpawnPoint>(true);
		for (int i = 0; i < spawnPointList.Length; ++i)
		{
			GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
			marker.transform.SetParent(markerRoot.transform);
			marker.transform.position = spawnPointList[i].transform.position + Vector3.up * 30.0f;
			marker.transform.localScale = new Vector3(1.3f, 5.0f, 1.3f);
			Material material = new Material(Shader.Find("Unlit/Color"));
			if (spawnPointList[i].GetMonsterType() == eMonsterType.Rat) material.color = new Color(1.0f, 0.78f, 0.12f);
			else material.color = new Color(0.9f, 0.18f, 0.12f);
			marker.GetComponent<Renderer>().sharedMaterial = material;
		}
		GameObject cameraObject = new GameObject("MonsterSpawnPreviewCamera", typeof(Camera));
		Camera camera = cameraObject.GetComponent<Camera>();
		camera.transform.position = new Vector3(110.0f, 240.0f, 110.0f);
		camera.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
		camera.orthographic = true;
		camera.orthographicSize = 110.0f;
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(0.05f, 0.08f, 0.06f);
		RenderTexture render = new RenderTexture(1280, 1280, 24);
		camera.targetTexture = render;
		camera.Render();
		RenderTexture.active = render;
		Texture2D texture = new Texture2D(1280, 1280, TextureFormat.RGB24, false);
		texture.ReadPixels(new Rect(0, 0, 1280, 1280), 0, 0);
		texture.Apply();
		File.WriteAllBytes("Build/Validation/BeginnerMonsterSpawns.png", texture.EncodeToPNG());
		RenderTexture.active = null;
		camera.targetTexture = null;
		UnityEngine.Object.DestroyImmediate(render);
		UnityEngine.Object.DestroyImmediate(texture);
		UnityEngine.Object.DestroyImmediate(cameraObject);
		UnityEngine.Object.DestroyImmediate(markerRoot);
	}
}
