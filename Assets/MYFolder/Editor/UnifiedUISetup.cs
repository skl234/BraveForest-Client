using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 패널 모양만 공통화함. 네트워크/캐릭터/맵 데이터는 여기서 수정하지 않음
public static class UnifiedUISetup
{
	const string Root = "Assets/MYFolder/";
	const string Common = Root + "Prefab/UI/Common/";
	const string ThemePath = Common + "BraveForestUITheme.asset";
	static UITheme m_theme;
	static TMP_FontAsset m_font;
	static string m_backupFolder;

	[MenuItem("MYFolder/UI/공통 패널 적용")]
	public static void Run()
	{
		m_backupFolder = "Build/Validation/Backups/UITheme_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
		Directory.CreateDirectory(m_backupFolder);
		EnsureAssets();
		StyleZonePrefab(Root + "Prefab/UI/ZoneInterface.prefab");
		StyleZonePrefab(Root + "Prefab/UI/CombatInterface.prefab");
		string[] sceneIds = AssetDatabase.FindAssets("t:Scene", new[] { Root + "Scenes" });
		for (int i = 0; i < sceneIds.Length; ++i)
		{
			string path = AssetDatabase.GUIDToAssetPath(sceneIds[i]);
			Backup(path);
			Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
			LoginScene login = UnityEngine.Object.FindObjectOfType<LoginScene>(true);
			if (login != null) ApplyLogin(login);
			ZoneMapUI map = UnityEngine.Object.FindObjectOfType<ZoneMapUI>(true);
			if (map != null)
			{
				ApplyMap(map);
			}
			ZoneGameplayUI gameplay = UnityEngine.Object.FindObjectOfType<ZoneGameplayUI>(true);
			if (gameplay != null)
			{
				if (new SerializedObject(gameplay).FindProperty("m_noticePanel").objectReferenceValue == null)
					throw new Exception(scene.name + " 공용 안내창 누락");
			}
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			Debug.Log("[UnifiedUI] SAVED " + scene.name);
		}
		AssetDatabase.SaveAssets();
		Verify();
		RenderPreviews();
		EditorSceneManager.OpenScene(Root + "Scenes/LoginScene.unity");
		Debug.Log("[UnifiedUI] COMPLETE");
	}

	static void EnsureAssets()
	{
		Directory.CreateDirectory(Common);
		AssetDatabase.Refresh();
		string previousTheme = Root + "UI/BraveForestUITheme.asset";
		if (AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath) == null && AssetDatabase.LoadAssetAtPath<UITheme>(previousTheme) != null)
		{
			AssetDatabase.MoveAsset(previousTheme, ThemePath);
			if (Directory.GetFileSystemEntries(Root + "UI").Length == 0) AssetDatabase.DeleteAsset(Root + "UI");
		}
		m_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Maplestory Bold SDF.asset");
		m_theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
		if (m_theme == null)
		{
			m_theme = ScriptableObject.CreateInstance<UITheme>();
			m_theme.panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
			m_theme.buttonSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
			AssetDatabase.CreateAsset(m_theme, ThemePath);
		}
		if (AssetDatabase.LoadAssetAtPath<GameObject>(Common + "PanelSurface.prefab") == null)
		{
			GameObject surface = new GameObject("PanelSurface", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(UIStyle));
			surface.GetComponent<Image>().raycastTarget = false;
			surface.GetComponent<RectTransform>().sizeDelta = new Vector2(520, 320);
			surface.GetComponent<UIStyle>().Setup(m_theme, eUIStyle.Panel);
			Save(surface, Common + "PanelSurface.prefab");
		}
		if (AssetDatabase.LoadAssetAtPath<GameObject>(Common + "CommonButton.prefab") == null)
		{
			GameObject button = new GameObject("CommonButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline), typeof(UIStyle));
			button.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 48);
			Text("Text", button.transform, "확인", Vector2.zero, new Vector2(164, 44), 20);
			button.GetComponent<UIStyle>().Setup(m_theme, eUIStyle.Button);
			Save(button, Common + "CommonButton.prefab");
		}
		RefreshTemplate(Common + "PanelSurface.prefab");
		RefreshTemplate(Common + "CommonButton.prefab");
		CreateNotice();
	}

	static void RefreshTemplate(string _path)
	{
		GameObject root = PrefabUtility.LoadPrefabContents(_path);
		UIStyle[] styles = root.GetComponentsInChildren<UIStyle>(true);
		for (int i = 0; i < styles.Length; ++i) styles[i].Apply();
		PrefabUtility.SaveAsPrefabAsset(root, _path);
		PrefabUtility.UnloadPrefabContents(root);
	}

	static void CreateNotice()
	{
		if (AssetDatabase.LoadAssetAtPath<GameObject>(Common + "NoticePanel.prefab") != null) return;
		GameObject root = new GameObject("Notice_Panel", typeof(RectTransform), typeof(Image), typeof(UIStyle), typeof(NoticePanelUI));
		Stretch((RectTransform)root.transform);
		root.GetComponent<UIStyle>().Setup(m_theme, eUIStyle.Overlay);
		root.GetComponent<Image>().raycastTarget = true;
		RectTransform dialog = Rect("Dialog", root.transform, Vector2.zero, new Vector2(520, 300));
		Panel(dialog);
		Text("Title", dialog, "안내", new Vector2(0, 108), new Vector2(440, 42), 28);
		TMP_Text message = Text("Message_Text", dialog, "", new Vector2(0, 12), new Vector2(456, 126), 22);
		GameObject button = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Common + "CommonButton.prefab"), dialog);
		button.name = "Confirm_Button";
		((RectTransform)button.transform).anchoredPosition = new Vector2(0, -102);
		NoticePanelUI notice = root.GetComponent<NoticePanelUI>();
		Ref(notice, "m_messageText", message);
		Ref(notice, "m_buttonText", button.GetComponentInChildren<TMP_Text>(true));
		Ref(notice, "m_button", button.GetComponent<Button>());
		root.SetActive(false);
		Save(root, Common + "NoticePanel.prefab");
	}

	public static NoticePanelUI AttachNotice(Transform _parent)
	{
		if (AssetDatabase.LoadAssetAtPath<GameObject>(Common + "NoticePanel.prefab") == null) EnsureAssets();
		Transform previous = _parent.Find("Notice_Panel");
		if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
		GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Common + "NoticePanel.prefab"), _parent);
		root.name = "Notice_Panel";
		Stretch((RectTransform)root.transform);
		// 두 Canvas의 기준 해상도가 달라도 실제 안내창 크기는 같게 맞춤
		CanvasScaler scaler = _parent.GetComponentInParent<CanvasScaler>(true);
		if (scaler != null)
		{
			Transform dialog = root.transform.Find("Dialog");
			float scale = 1.5f / scaler.scaleFactor;
			if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize) scale = scaler.referenceResolution.y / 720.0f;
			dialog.localScale = Vector3.one * scale;
			PrefabUtility.RecordPrefabInstancePropertyModifications(dialog);
		}
		root.SetActive(false);
		return root.GetComponent<NoticePanelUI>();
	}

	public static void ApplyLogin(LoginScene _login)
	{
		if (m_theme == null) EnsureAssets();
		Canvas canvas = UnityEngine.Object.FindObjectOfType<Canvas>(true);
		Panel(Find(canvas.transform, "Account_Panel"));
		Transform account = Find(canvas.transform, "CreateAccount_Panel");
		Panel(account);
		TMP_Text[] labels = account.GetComponentsInChildren<TMP_Text>(true);
		for (int i = 0; i < labels.Length; ++i)
		{
			if (labels[i].GetComponentInParent<TMP_InputField>() != null || labels[i].GetComponentInParent<Button>() != null) continue;
			labels[i].color = m_theme.textColor;
		}
		if (account.Find("Title") == null) Text("Title", account, "회원가입", new Vector2(0, 214), new Vector2(800, 40), 32);
		if (account.Find("ModalVeil") == null)
		{
			RectTransform veil = Rect("ModalVeil", account, Vector2.zero, new Vector2(1920, 1080));
			veil.gameObject.AddComponent<Image>().raycastTarget = true;
			Style(veil, eUIStyle.Overlay);
			veil.SetAsFirstSibling();
		}
		Panel(Find(canvas.transform, "Create_Character_Dialog/Window"));
		Transform create = Find(canvas.transform, "Create_Character_Dialog");
		Style(create, eUIStyle.Overlay);
		Controls(canvas.gameObject);
		Ref(_login, "m_noticePanel", AttachNotice(canvas.transform));
	}

	public static void ApplyMap(ZoneMapUI _map)
	{
		if (m_theme == null) EnsureAssets();
		Panel(_map.transform);
		Controls(_map.gameObject);
	}

	static void StyleZonePrefab(string _path)
	{
		Backup(_path);
		GameObject root = PrefabUtility.LoadPrefabContents(_path);
		ApplyZone(root);
		PrefabUtility.SaveAsPrefabAsset(root, _path);
		PrefabUtility.UnloadPrefabContents(root);
	}

	public static void ApplyZone(GameObject _root)
	{
		if (m_theme == null) EnsureAssets();
		GameObject root = _root;
		string[] panels = { "HUD/ActionBar", "HUD/TargetPanel", "HUD/ZoneChat", "HUD/ChannelPanel", "ChannelUI/ChannelWindow/Panel", "ClassSelection/SelectionCard" };
		for (int i = 0; i < panels.Length; ++i) Panel(root.transform.Find(panels[i]));
		Style(root.transform.Find("HUD/ActionBar/PlayerVitals"), eUIStyle.Inset);
		Style(root.transform.Find("ChannelUI/ChannelWindow/Panel/Viewport"), eUIStyle.Inset);
		Style(root.transform.Find("ChannelUI/ChannelWindow"), eUIStyle.Overlay);
		Style(root.transform.Find("ClassSelection"), eUIStyle.Overlay);
		Controls(root);
		ZoneGameplayUI gameplay = root.GetComponent<ZoneGameplayUI>();
		if (gameplay != null)
		{
			Transform oldConfirm = root.transform.Find("LoadingOverlay/Confirm");
			if (oldConfirm != null) UnityEngine.Object.DestroyImmediate(oldConfirm.gameObject);
			Ref(gameplay, "m_noticePanel", AttachNotice(root.transform));
		}
	}

	static void Controls(GameObject _root)
	{
		Button[] buttons = _root.GetComponentsInChildren<Button>(true);
		for (int i = 0; i < buttons.Length; ++i)
		{
			// 스킬 아이콘/쿨타임 색상은 기능 표시이므로 그대로 둠
			if (buttons[i].GetComponent<SkillSlotUI>() != null) continue;
			Style(buttons[i].transform, eUIStyle.Button);
		}
		TMP_InputField[] inputs = _root.GetComponentsInChildren<TMP_InputField>(true);
		for (int i = 0; i < inputs.Length; ++i) Style(inputs[i].transform, eUIStyle.Input);
	}

	static void Panel(Transform _panel)
	{
		if (_panel == null) return;
		Image original = _panel.GetComponent<Image>();
		if (original != null) original.color = Color.clear;
		Outline outline = _panel.GetComponent<Outline>();
		if (outline != null) outline.enabled = false;
		Transform existing = _panel.Find("PanelSurface");
		if (existing != null) return;
		GameObject surface = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Common + "PanelSurface.prefab"), _panel);
		surface.name = "PanelSurface";
		Stretch((RectTransform)surface.transform);
		surface.transform.SetAsFirstSibling();
	}

	static void Style(Transform _target, eUIStyle _style)
	{
		if (_target == null || _target.GetComponent<Image>() == null) return;
		if (_style != eUIStyle.Overlay && _target.GetComponent<Outline>() == null) _target.gameObject.AddComponent<Outline>();
		UIStyle style = _target.GetComponent<UIStyle>();
		if (style == null) style = _target.gameObject.AddComponent<UIStyle>();
		style.Setup(m_theme, _style);
	}

	public static void Verify()
	{
		m_theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
		m_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Maplestory Bold SDF.asset");
		string[] scenes = { "LoginScene", "BeginnerZoneScene", "TownScene" };
		for (int i = 0; i < scenes.Length; ++i)
		{
			EditorSceneManager.OpenScene(Root + "Scenes/" + scenes[i] + ".unity");
			NoticePanelUI notice = UnityEngine.Object.FindObjectOfType<NoticePanelUI>(true);
			if (notice == null || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(notice) != Common + "NoticePanel.prefab") throw new Exception(scenes[i] + " 공용 안내창 연결 오류");
			if (notice.gameObject.activeSelf) throw new Exception(scenes[i] + " 안내창은 기본 비활성이어야 함");
			CanvasScaler scaler = notice.GetComponentInParent<CanvasScaler>();
			float canvasScale = scaler.scaleFactor;
			if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize) canvasScale = 1080 / scaler.referenceResolution.y;
			RectTransform dialog = (RectTransform)notice.transform.Find("Dialog");
			float pixelWidth = dialog.sizeDelta.x * dialog.localScale.x * canvasScale;
			if (Mathf.Abs(pixelWidth - 780) > 0.1f) throw new Exception(scenes[i] + " 안내창 크기 불일치: " + pixelWidth);
			int count = 0;
			Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
			for (int j = 0; j < canvases.Length; ++j)
			{
				if (canvases[j].renderMode == RenderMode.WorldSpace) continue;
				UIStyle[] styles = canvases[j].GetComponentsInChildren<UIStyle>(true);
				for (int k = 0; k < styles.Length; ++k)
				{
					if (styles[k].GetTheme() != m_theme) throw new Exception("UI 테마 불일치: " + styles[k].name);
					++count;
				}
				TMP_Text[] texts = canvases[j].GetComponentsInChildren<TMP_Text>(true);
				for (int k = 0; k < texts.Length; ++k)
					if (texts[k].font != m_font) throw new Exception("메이플 폰트 불일치: " + texts[k].name);
			}
			int called = 0;
			notice.Initialize();
			notice.Show("첫 안내", "확인", () => ++called);
			notice.Show("두 번째 안내", "확인", () => ++called);
			notice.GetComponentInChildren<Button>(true).onClick.Invoke();
			if (called != 1 || notice.gameObject.activeSelf) throw new Exception("안내창 확인/콜백 중복 오류");
			notice.Show("다시 표시", "확인", null);
			notice.Hide();
			Debug.Log("[UnifiedUI] VERIFIED " + scenes[i] + " styles=" + count + " Maple font / shared notice / close callback");
		}
	}

	public static void VerifyAndBuild()
	{
		Verify();
		LoginSceneUIBuilder.Validate();
		Debug.Log("[UnifiedUI] VERIFIED character selection / input / create button");
		ZoneGameplayUISetup.Build();
	}

	public static void ApplyAndBuild()
	{
		Run();
		VerifyAndBuild();
	}

	public static void RenderPreviews()
	{
		EditorSceneManager.OpenScene(Root + "Scenes/LoginScene.unity");
		Canvas loginCanvas = UnityEngine.Object.FindObjectOfType<Canvas>(true);
		Capture("LoginPanel");
		Find(loginCanvas.transform, "CreateAccount_Panel").gameObject.SetActive(true);
		Capture("AccountPanel");
		Find(loginCanvas.transform, "Login_Panel").gameObject.SetActive(false);
		Find(loginCanvas.transform, "Character_Panel").gameObject.SetActive(true);
		Find(loginCanvas.transform, "Create_Character_Dialog").gameObject.SetActive(true);
		// 렌더 전용 화면은 캐릭터를 생성하지 않으므로 미할당 RenderTexture만 숨김
		RawImage[] previews = loginCanvas.GetComponentsInChildren<RawImage>(true);
		for (int i = 0; i < previews.Length; ++i)
			if (previews[i].texture == null) previews[i].gameObject.SetActive(false);
		Capture("CharacterPanel");
		UnityEngine.Object.FindObjectOfType<NoticePanelUI>(true).Show("서버와 연결이 되지않았습니다.", "확인", null);
		Capture("LoginNotice");
		string[] zones = { "TownScene", "BeginnerZoneScene" };
		for (int i = 0; i < zones.Length; ++i)
		{
			EditorSceneManager.OpenScene(Root + "Scenes/" + zones[i] + ".unity");
			ZoneGameplayUI ui = UnityEngine.Object.FindObjectOfType<ZoneGameplayUI>(true);
			ui.SetLoading(false);
			Transform channel = ui.transform.Find("ChannelUI/ChannelWindow");
			channel.gameObject.SetActive(true);
			ZoneChannelUI channels = ui.GetComponentInChildren<ZoneChannelUI>(true);
			ZoneChannelRowUI[] rows = channels.GetComponentsInChildren<ZoneChannelRowUI>(true);
			for (int j = 0; j < rows.Length; ++j)
			{
				rows[j].gameObject.SetActive(j < 3);
				if (j < 3) rows[j].Bind(channels, (ulong)j + 1, (ulong)(18 + j * 24), 128, true, j == 0);
			}
			channels.SetChannel(1);
			ZoneSceneInfo info = UnityEngine.Object.FindObjectOfType<ZoneSceneInfo>();
			ZoneMapUI map = UnityEngine.Object.FindObjectOfType<ZoneMapUI>(true);
			map.GetComponentInChildren<RawImage>(true).texture = info.GetMapImage();
			Capture(zones[i] + "_Channel");
			channel.gameObject.SetActive(false);
			ui.ShowMessage("채널을 이동할 수 없는 상태입니다.");
			Capture(zones[i] + "_Notice");
		}
	}

	static void Capture(string _name)
	{
		// 실제 씬의 저장된 UI를 렌더함. 플레이 모드/서버 접속은 하지 않음
		GameObject cameraObject = new GameObject("UIPreviewCamera", typeof(Camera));
		Camera camera = cameraObject.GetComponent<Camera>();
		camera.clearFlags = CameraClearFlags.SolidColor;
		camera.backgroundColor = new Color(0.10f, 0.10f, 0.10f);
		camera.cullingMask = ~0;
		camera.transform.position = new Vector3(0, 0, -1000);
		camera.orthographic = true;
		camera.orthographicSize = 540;
		camera.nearClipPlane = 0.1f;
		camera.farClipPlane = 100;
		RenderTexture target = new RenderTexture(1920, 1080, 24);
		camera.targetTexture = target;
		Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
		for (int i = 0; i < canvases.Length; ++i)
		{
			if (canvases[i].renderMode == RenderMode.WorldSpace) continue;
			canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
			canvases[i].worldCamera = camera;
			canvases[i].planeDistance = 10;
		}
		Canvas.ForceUpdateCanvases();
		camera.Render();
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = target;
		Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
		image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
		image.Apply();
		Directory.CreateDirectory("Build/Validation/UITheme");
		File.WriteAllBytes("Build/Validation/UITheme/" + _name + ".png", image.EncodeToPNG());
		RenderTexture.active = previous;
		camera.targetTexture = null;
		UnityEngine.Object.DestroyImmediate(image);
		UnityEngine.Object.DestroyImmediate(target);
		UnityEngine.Object.DestroyImmediate(cameraObject);
	}

	static Transform Find(Transform _root, string _name)
	{
		Transform found = _root.Find(_name);
		if (found != null) return found;
		for (int i = 0; i < _root.childCount; ++i)
		{
			found = Find(_root.GetChild(i), _name);
			if (found != null) return found;
		}
		return null;
	}

	static void Backup(string _path)
	{
		if (m_backupFolder != null && File.Exists(_path)) File.Copy(_path, m_backupFolder + "/" + Path.GetFileName(_path), true);
	}
	static void Save(GameObject _root, string _path)
	{
		PrefabUtility.SaveAsPrefabAsset(_root, _path);
		UnityEngine.Object.DestroyImmediate(_root);
	}
	static RectTransform Rect(string _name, Transform _parent, Vector2 _position, Vector2 _size)
	{
		RectTransform rect = new GameObject(_name, typeof(RectTransform)).GetComponent<RectTransform>();
		rect.SetParent(_parent, false);
		rect.anchoredPosition = _position;
		rect.sizeDelta = _size;
		return rect;
	}
	static TMP_Text Text(string _name, Transform _parent, string _value, Vector2 _position, Vector2 _size, float _fontSize)
	{
		RectTransform rect = Rect(_name, _parent, _position, _size);
		TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
		text.font = m_font;
		text.text = _value;
		text.fontSize = _fontSize;
		text.color = Color.white;
		text.alignment = TextAlignmentOptions.Center;
		text.raycastTarget = false;
		return text;
	}
	static void Stretch(RectTransform _rect)
	{
		_rect.anchorMin = Vector2.zero;
		_rect.anchorMax = Vector2.one;
		_rect.offsetMin = Vector2.zero;
		_rect.offsetMax = Vector2.zero;
	}
	static void Ref(UnityEngine.Object _owner, string _name, UnityEngine.Object _value)
	{
		SerializedObject data = new SerializedObject(_owner);
		SerializedProperty property = data.FindProperty(_name);
		if (property == null) throw new Exception(_owner.name + "." + _name + " 누락");
		property.objectReferenceValue = _value;
		data.ApplyModifiedPropertiesWithoutUndo();
	}
}
