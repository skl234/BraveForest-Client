using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class WorldPresentationSetup
{
	const string Root = "Assets/MYFolder/";
	const string BubblePath = Root + "Prefab/UI/Chat/ChatBubble.prefab";
	const string CameraPath = Root + "Prefab/Camera/PlayerCamera.prefab";
	const uint SectorWidth = 80;

	public static void Run()
	{
		PlayerSettings.defaultScreenWidth = 1920;
		PlayerSettings.defaultScreenHeight = 1080;
		PlayerSettings.resizableWindow = false;
		PlayerSettings.allowFullscreenSwitch = false;
		PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
		CreateSharedCamera();
		CreateBubble();
		string worldUIPath = Root + "Prefab/UI/Player/PlayerWorldUI.prefab";
		GameObject worldUI = PrefabUtility.LoadPrefabContents(worldUIPath);
		AttachChatBubble(worldUI);
		PrefabUtility.SaveAsPrefabAsset(worldUI, worldUIPath);
		PrefabUtility.UnloadPrefabContents(worldUI);
		string uiPath = Root + "Prefab/UI/ZoneInterface.prefab";
		GameObject ui = PrefabUtility.LoadPrefabContents(uiPath);
		UpdateChat(ui);
		PrefabUtility.SaveAsPrefabAsset(ui, uiPath);
		PrefabUtility.UnloadPrefabContents(ui);
		string[] types = { "Ghost", "Lizard", "Knight", "Rat" };
		for (int i = 0; i < types.Length; ++i)
		{
			string path = Root + "Prefab/Object/Monster/MonsterObject_" + types[i] + ".prefab";
			GameObject monster = PrefabUtility.LoadPrefabContents(path);
			SetWanderTimes(monster.GetComponent<MonsterSpawnPoint>());
			PrefabUtility.SaveAsPrefabAsset(monster, path);
			PrefabUtility.UnloadPrefabContents(monster);
		}
		string exporterPath = Root + "Prefab/FieldData/FieldDataExporter.prefab";
		GameObject exporterPrefab = PrefabUtility.LoadPrefabContents(exporterPath);
		SetSectorWidth(exporterPrefab.GetComponent<FieldDataExporter>());
		PrefabUtility.SaveAsPrefabAsset(exporterPrefab, exporterPath);
		PrefabUtility.UnloadPrefabContents(exporterPrefab);
		SetupScene("BeginnerZoneScene", "BeginnerZoneField.txt");
		SetupScene("TownScene", "TownField.txt");
		AssetDatabase.SaveAssets();
		Debug.Log("[WorldPresentation] SETUP PASSED");
	}

	static void CreateSharedCamera()
	{
		Directory.CreateDirectory(Root + "Prefab/Camera");
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPath);
		GameObject root;
		if (prefab != null) root = PrefabUtility.LoadPrefabContents(CameraPath);
		else
		{
			EditorSceneManager.OpenScene(Root + "Scenes/BeginnerZoneScene.unity");
			root = UnityEngine.Object.Instantiate(Camera.main.gameObject);
		}
		root.name = "PlayerCamera";
		root.tag = "MainCamera";
		CameraController oldController = root.GetComponent<CameraController>();
		if (oldController != null) UnityEngine.Object.DestroyImmediate(oldController);
		PlayerFollowCamera follow = root.GetComponent<PlayerFollowCamera>();
		if (follow == null) follow = root.AddComponent<PlayerFollowCamera>();
		follow.enabled = true;
		SerializedObject data = new SerializedObject(follow);
		data.FindProperty("m_target").objectReferenceValue = null;
		data.FindProperty("m_distance").floatValue = 14;
		data.FindProperty("m_minDistance").floatValue = 4.5f;
		data.FindProperty("m_maxDistance").floatValue = 35;
		data.FindProperty("m_zoomSpeed").floatValue = 8;
		data.FindProperty("m_targetHeight").floatValue = 1;
		data.ApplyModifiedPropertiesWithoutUndo();
		root.GetComponent<Camera>().fieldOfView = 45;
		root.transform.position = new Vector3(0, 20, -20);
		root.transform.rotation = Quaternion.Euler(45, 0, 0);
		PrefabUtility.SaveAsPrefabAsset(root, CameraPath);
		if (prefab != null) PrefabUtility.UnloadPrefabContents(root);
		else UnityEngine.Object.DestroyImmediate(root);
	}

	public static void AttachChatBubble(GameObject _worldUI)
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BubblePath);
		if (prefab == null) prefab = CreateBubble();
		Transform old = _worldUI.transform.Find("ChatBubble");
		if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
		GameObject bubble = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _worldUI.transform);
		bubble.name = "ChatBubble";
		((RectTransform)bubble.transform).anchoredPosition = new Vector2(0, 60);
		bubble.SetActive(false);
		SerializedObject data = new SerializedObject(_worldUI.GetComponent<PlayerWorldUI>());
		data.FindProperty("m_chatBubble").objectReferenceValue = bubble.GetComponent<ChatBubbleUI>();
		data.ApplyModifiedPropertiesWithoutUndo();
	}

	public static void AttachNPCBubble(NPCObject _npc)
	{
		Transform old = _npc.transform.Find("PermanentDialogue");
		if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
		GameObject canvasObject = new GameObject("PermanentDialogue", typeof(RectTransform), typeof(Canvas));
		canvasObject.transform.SetParent(_npc.transform, false);
		canvasObject.transform.localPosition = new Vector3(0, 3.2f, 0);
		canvasObject.transform.localScale = Vector3.one * .008f;
		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.WorldSpace;
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BubblePath);
		if (prefab == null) prefab = CreateBubble();
		GameObject bubble = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
		bubble.name = "ChatBubble";
		((RectTransform)bubble.transform).anchoredPosition = Vector2.zero;
		SerializedObject data = new SerializedObject(_npc);
		data.FindProperty("m_chatBubble").objectReferenceValue = bubble.GetComponent<ChatBubbleUI>();
		data.ApplyModifiedPropertiesWithoutUndo();
	}

	static GameObject CreateBubble()
	{
		Directory.CreateDirectory(Root + "Prefab/UI/Chat");
		GameObject root = new GameObject("ChatBubble", typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer), typeof(SpeechBubbleGraphic), typeof(ChatBubbleUI));
		RectTransform rect = (RectTransform)root.transform;
		rect.pivot = new Vector2(.5f, 0);
		rect.sizeDelta = new Vector2(200, 48);
		root.GetComponent<SpeechBubbleGraphic>().color = new Color(.98f, .97f, .91f, 1);
		root.GetComponent<SpeechBubbleGraphic>().raycastTarget = false;
		CanvasGroup group = root.GetComponent<CanvasGroup>();
		group.blocksRaycasts = false;
		group.interactable = false;
		GameObject textObject = new GameObject("Message", typeof(RectTransform), typeof(TextMeshProUGUI));
		textObject.transform.SetParent(root.transform, false);
		TMP_Text text = textObject.GetComponent<TMP_Text>();
		text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Maplestory Bold SDF.asset");
		text.fontSize = 18;
		text.color = new Color(.12f, .15f, .15f, 1);
		text.alignment = TextAlignmentOptions.Center;
		text.richText = false;
		text.raycastTarget = false;
		RectTransform textRect = text.rectTransform;
		textRect.anchorMin = Vector2.zero;
		textRect.anchorMax = Vector2.one;
		textRect.offsetMin = new Vector2(15, 10);
		textRect.offsetMax = new Vector2(-15, -10);
		SerializedObject data = new SerializedObject(root.GetComponent<ChatBubbleUI>());
		data.FindProperty("m_text").objectReferenceValue = text;
		data.FindProperty("m_group").objectReferenceValue = group;
		data.ApplyModifiedPropertiesWithoutUndo();
		GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BubblePath);
		UnityEngine.Object.DestroyImmediate(root);
		return prefab;
	}

	static void UpdateChat(GameObject _root)
	{
		ZoneChatUI chat = _root.GetComponentInChildren<ZoneChatUI>(true);
		if (chat == null) return;
		TMP_InputField input = chat.GetComponentInChildren<TMP_InputField>(true);
		input.characterLimit = 15;
		((TMP_Text)input.placeholder).text = "클릭 또는 Enter로 채팅";
	}

	static void SetWanderTimes(MonsterSpawnPoint _spawn)
	{
		if (_spawn == null) return;
		SerializedObject data = new SerializedObject(_spawn);
		data.FindProperty("m_idleMinTime").floatValue = 4;
		data.FindProperty("m_idleMaxTime").floatValue = 8;
		data.FindProperty("m_wanderMinTime").floatValue = 6;
		data.FindProperty("m_wanderMaxTime").floatValue = 10;
		data.ApplyModifiedPropertiesWithoutUndo();
	}

	static void SetSectorWidth(FieldDataExporter _exporter, uint _width = SectorWidth)
	{
		if (_exporter == null) return;
		SerializedObject data = new SerializedObject(_exporter);
		data.FindProperty("m_sectorWidth").longValue = _width;
		data.ApplyModifiedPropertiesWithoutUndo();
	}

	static void SetupScene(string _name, string _fieldName)
	{
		string path = Root + "Scenes/" + _name + ".unity";
		Directory.CreateDirectory("Build/Validation/Backups");
		File.Copy(path, "Build/Validation/Backups/" + _name + "_Presentation_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
		UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(path);
		uint sectorWidth = SectorWidth;
		Camera previousCamera = Camera.main;
		Vector3 cameraPosition = previousCamera.transform.position;
		UnityEngine.Object.DestroyImmediate(previousCamera.gameObject);
		GameObject cameraObject = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CameraPath));
		cameraObject.transform.position = cameraPosition;
		GameObject[] roots = scene.GetRootGameObjects();
		for (int i = 0; i < roots.Length; ++i) UpdateChat(roots[i]);
		FieldDataExporter[] exporters = UnityEngine.Object.FindObjectsOfType<FieldDataExporter>(true);
		for (int i = 0; i < exporters.Length; ++i) SetSectorWidth(exporters[i], sectorWidth);
		MonsterSpawnPoint[] spawns = UnityEngine.Object.FindObjectsOfType<MonsterSpawnPoint>(true);
		for (int i = 0; i < spawns.Length; ++i) SetWanderTimes(spawns[i]);
		// 지형/Block/스폰 목록은 그대로 두고 섹터 헤더만 갱신
		string fieldPath = Root + "ExportedFieldData/" + _fieldName;
		string[] lines = File.ReadAllLines(fieldPath);
		uint widthX = uint.Parse(lines[0].Split(' ')[1]);
		uint widthZ = uint.Parse(lines[1].Split(' ')[1]);
		lines[4] = "SectorWidth " + sectorWidth;
		lines[5] = "SectorRows " + ((widthX + sectorWidth - 1) / sectorWidth);
		lines[6] = "SectorCols " + ((widthZ + sectorWidth - 1) / sectorWidth);
		File.WriteAllLines(fieldPath, lines);
		Camera camera = Camera.main;
		SerializedObject cameraData = new SerializedObject(camera.GetComponent<PlayerFollowCamera>());
		float maxDistance = cameraData.FindProperty("m_maxDistance").floatValue;
		float targetHeight = cameraData.FindProperty("m_targetHeight").floatValue;
		Vector3 originalPosition = camera.transform.position;
		Quaternion originalRotation = camera.transform.rotation;
		camera.aspect = 1920.0f / 1080;
		camera.transform.rotation = Quaternion.Euler(45, 0, 0);
		camera.transform.position = Vector3.up * targetHeight - camera.transform.forward * maxDistance;
		Plane ground = new Plane(Vector3.up, Vector3.zero);
		float maxExtent = 0;
		for (int i = 0; i < 4; ++i)
		{
			Ray ray = camera.ViewportPointToRay(new Vector3(i % 2, i / 2, 0));
			if (!ground.Raycast(ray, out float distance)) throw new Exception(_name + " 카메라가 지평선 위를 보고 있음");
			Vector3 point = ray.GetPoint(distance);
			maxExtent = Mathf.Max(maxExtent, Mathf.Abs(point.x), Mathf.Abs(point.z));
		}
		camera.transform.position = originalPosition;
		camera.transform.rotation = originalRotation;
		if (maxExtent + 12 > sectorWidth) throw new Exception(_name + " 최대 줌 가시범위에 비해 섹터가 작음: " + maxExtent);
		Debug.Log("[WorldPresentation] " + _name + " maxZoom=" + maxDistance + " maxExtent=" + maxExtent + " sector=" + sectorWidth + " margin=" + (sectorWidth - maxExtent));
		EditorSceneManager.MarkSceneDirty(scene);
		EditorSceneManager.SaveScene(scene);
	}

	public static void VerifyAndBuild()
	{
		Run();
		string[] scenes = { "BeginnerZoneScene", "TownScene" };
		for (int i = 0; i < scenes.Length; ++i)
		{
			EditorSceneManager.OpenScene(Root + "Scenes/" + scenes[i] + ".unity");
			Camera camera = Camera.main;
			if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(camera) != CameraPath) throw new Exception("공용 카메라 연결 누락");
			if (camera.GetComponent<CameraController>() != null || camera.fieldOfView != 45) throw new Exception("카메라 설정 불일치");
			ZoneGameplayUI ui = UnityEngine.Object.FindObjectOfType<ZoneGameplayUI>(true);
			if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(ui) != Root + "Prefab/UI/ZoneInterface.prefab") throw new Exception("공용 HUD 연결 누락");
			TMP_InputField input = UnityEngine.Object.FindObjectOfType<ZoneChatUI>(true).GetComponentInChildren<TMP_InputField>(true);
			if (input.characterLimit != 15 || ((TMP_Text)input.placeholder).text.Contains("30")) throw new Exception("채팅 제한 설정 불일치");
			Debug.Log("[WorldPresentation] VERIFIED shared camera/HUD/chat: " + scenes[i]);
		}
		string[] players = { "LocalPlayerObject_Warrior", "LocalPlayerObject_Archer", "RemotePlayerObject_Warrior", "RemotePlayerObject_Archer" };
		for (int i = 0; i < players.Length; ++i)
		{
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/Object/Player/" + players[i] + ".prefab");
			ChatBubbleUI bubble = prefab.GetComponentInChildren<ChatBubbleUI>(true);
			if (bubble == null) throw new Exception(players[i] + " 말풍선 누락");
			SerializedObject worldUI = new SerializedObject(prefab.GetComponentInChildren<PlayerWorldUI>(true));
			if (worldUI.FindProperty("m_chatBubble").objectReferenceValue != bubble) throw new Exception(players[i] + " 말풍선 참조 불일치");
			Debug.Log("[WorldPresentation] VERIFIED shared bubble: " + players[i]);
		}
		RenderBubblePreview();
		ZoneGameplayUISetup.Build();
	}

	static void RenderBubblePreview()
	{
		EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		Camera camera = new GameObject("PreviewCamera", typeof(Camera)).GetComponent<Camera>();
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(.12f, .19f, .17f);
		camera.orthographic = true;
		camera.orthographicSize = 240;
		RenderTexture target = new RenderTexture(960, 480, 24);
		camera.targetTexture = target;
		Canvas canvas = new GameObject("PreviewCanvas", typeof(Canvas)).GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceCamera;
		canvas.worldCamera = camera;
		canvas.planeDistance = 10;
		string[] samples = { "안녕하세요!", "같이 사냥하러 갈까요?", "가나다라마바사아자차카타파하끝" };
		for (int i = 0; i < samples.Length; ++i)
		{
			GameObject bubble = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(BubblePath), canvas.transform);
			bubble.GetComponent<ChatBubbleUI>().Show(samples[i]);
			bubble.GetComponent<CanvasGroup>().alpha = 1;
			((RectTransform)bubble.transform).anchoredPosition = new Vector2((i - 1) * 290, -15);
		}
		Canvas.ForceUpdateCanvases();
		camera.Render();
		RenderTexture.active = target;
		Texture2D image = new Texture2D(960, 480, TextureFormat.RGB24, false);
		image.ReadPixels(new Rect(0, 0, 960, 480), 0, 0);
		image.Apply();
		File.WriteAllBytes("Build/Validation/ChatBubblePreview.png", image.EncodeToPNG());
		RenderTexture.active = null;
		camera.targetTexture = null;
		UnityEngine.Object.DestroyImmediate(image);
		target.Release();
		UnityEngine.Object.DestroyImmediate(target);
	}
}
