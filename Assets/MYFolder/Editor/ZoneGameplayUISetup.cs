using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// UI는 실행 중에 만들지 않고 여기서 프리팹/씬에 저장함
public static class ZoneGameplayUISetup
{
    const string Root = "Assets/MYFolder/";
    static TMP_FontAsset m_font;
    static Sprite m_solid;

    [MenuItem("MYFolder/실전 UI 연결")]
    public static void Run()
    {
        m_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Maplestory Bold SDF.asset");
        m_solid = CreateSolidSprite();
        GameObject uiPrefab = CreateInterface();
        SetupZone("BeginnerZoneScene", uiPrefab);
        SetupZone("TownScene", uiPrefab);
        SetupLogin();
        FixWorldBars();
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(Root + "Scenes/LoginScene.unity", true),
            new EditorBuildSettingsScene(Root + "Scenes/BeginnerZoneScene.unity", true),
            new EditorBuildSettingsScene(Root + "Scenes/TownScene.unity", true)
        };
        AssetDatabase.SaveAssets();
        Debug.Log("[ZoneGameplayUISetup] COMPLETE");
    }

    static void Backup(string _path)
    {
        Directory.CreateDirectory("Build/Validation/Backups");
        File.Copy(_path, "Build/Validation/Backups/" + Path.GetFileNameWithoutExtension(_path) + "_OnlineUI_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity");
    }
    static Sprite CreateSolidSprite()
    {
        string path = Root + "Image/UISolid.asset";
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int i = 0; i < assets.Length; ++i) if (assets[i] is Sprite) return (Sprite)assets[i];
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.name = "UISolid";
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(.5f, .5f));
        sprite.name = "UISolid";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        AssetDatabase.SaveAssets();
        return sprite;
    }
    static GameObject CreateInterface()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(Root + "Prefab/UI/CombatInterface.prefab");
        root.name = "ZoneInterface";
        Remove(root.transform.Find("ClassSelection"));
        Remove(root.transform.Find("HUD/TestTools"));
        Remove(root.transform.Find("HUD/InputGuide"));
        UnityEngine.Object.DestroyImmediate(root.GetComponent<OfflineCombatSession>());
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;

        Transform hudRoot = root.transform.Find("HUD");
        hudRoot.gameObject.SetActive(true);
        CombatHUD hud = hudRoot.GetComponent<CombatHUD>();
        ZoneGameplayUI gameplay = root.AddComponent<ZoneGameplayUI>();
        Ref(gameplay, "m_hud", hud);
        RectTransform bar = (RectTransform)hudRoot.Find("ActionBar");
        bar.anchorMin = bar.anchorMax = new Vector2(1, 0);
        bar.anchoredPosition = new Vector2(-448, 89);
        bar.sizeDelta = new Vector2(864, 146);
        ((RectTransform)bar.Find("PlayerVitals")).anchoredPosition = new Vector2(-264, 0);
        string[] slots = { "LMB", "A", "S", "D" };
        UnityAction[] actions = { gameplay.BasicAttack, gameplay.SkillA, gameplay.SkillS, gameplay.UsePotion };
        for (int i = 0; i < slots.Length; ++i)
        {
            Button button = bar.Find("Skill_" + slots[i]).GetComponent<Button>();
            ((RectTransform)button.transform).anchoredPosition = new Vector2(-48 + 116 * i, 8);
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(button.onClick, actions[i]);
        }
        ((RectTransform)hudRoot.Find("Status")).anchoredPosition = new Vector2(180, 181);
        ((RectTransform)hudRoot.Find("ChannelPanel")).anchoredPosition = new Vector2(180, 220);
        FixBars(root);
        SetupChat(hudRoot.Find("ZoneChat"));
        CreateChannels(root.transform);
        ChatScrollbarSetup.ConfigureChat(hudRoot.Find("ZoneChat").gameObject);
        ChatScrollbarSetup.ConfigureChannels(root.transform.Find("ChannelUI/ChannelWindow/Panel"));
        RectTransform loading = Box("LoadingOverlay", root.transform, Vector2.zero, Vector2.zero, new Color(.015f, .025f, .03f, 1));
        Stretch(loading);
        loading.GetComponent<Image>().raycastTarget = true;
        TMP_Text loadingText = Text("LoadingText", loading, "BRAVE FOREST\n\nLOADING...", Vector2.zero, new Vector2(900, 320), 34);
        Ref(gameplay, "m_loading", loading.gameObject);
        Ref(gameplay, "m_loadingText", loadingText);
        Ref(gameplay, "m_noticePanel", UnifiedUISetup.AttachNotice(root.transform));
        loading.gameObject.SetActive(false);
        UnifiedUISetup.ApplyZone(root);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "Prefab/UI/ZoneInterface.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        return prefab;
    }
    static void SetupChat(Transform _root)
    {
        RectTransform panel = (RectTransform)_root;
        panel.anchoredPosition = new Vector2(188, 136);
        panel.sizeDelta = new Vector2(344, 240);
        panel.GetComponent<Image>().raycastTarget = true;
        RectTransform viewport = (RectTransform)panel.Find("Viewport");
        viewport.sizeDelta = new Vector2(320, 176);
        RectTransform input = (RectTransform)panel.Find("Input");
        input.sizeDelta = new Vector2(320, 36);
        ((RectTransform)input.Find("Text")).sizeDelta = new Vector2(304, 30);
        ((RectTransform)input.Find("Placeholder")).sizeDelta = new Vector2(304, 30);
        input.Find("Placeholder").GetComponent<TMP_Text>().text = "클릭 또는 Enter로 채팅";
        input.GetComponent<TMP_InputField>().characterLimit = 15;
        TMP_Text history = viewport.Find("ChatHistory").GetComponent<TMP_Text>();
        history.richText = false;
        history.alignment = TextAlignmentOptions.TopLeft;
        RectTransform content = history.rectTransform;
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = new Vector2(1, 1);
        content.pivot = new Vector2(.5f, 1);
        content.anchoredPosition = new Vector2(0, -4);
        content.sizeDelta = new Vector2(-12, 168);
        ContentSizeFitter fitter = history.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = history.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
    static void CreateChannels(Transform _parent)
    {
        RectTransform root = new GameObject("ChannelUI", typeof(RectTransform), typeof(ZoneChannelUI)).GetComponent<RectTransform>();
        root.SetParent(_parent, false);
        Stretch(root);
        ZoneChannelUI ui = root.GetComponent<ZoneChannelUI>();
        Button open = Button("CurrentChannel", root, "채널 연결 중", new Vector2(-120, -42), new Vector2(208, 46), ui.Open);
        RectTransform openRect = (RectTransform)open.transform;
        openRect.anchorMin = openRect.anchorMax = Vector2.one;
        Ref(ui, "m_currentChannel", open.transform.Find("Label").GetComponent<TMP_Text>());

        RectTransform veil = Box("ChannelWindow", root, Vector2.zero, Vector2.zero, new Color(0, 0, 0, .6f));
        Stretch(veil);
        veil.GetComponent<Image>().raycastTarget = true;
        RectTransform panel = Box("Panel", veil, Vector2.zero, new Vector2(520, 520), new Color(.08f, .12f, .14f, .99f));
        Text("Title", panel, "채널 변경", new Vector2(0, 218), new Vector2(420, 42), 26);
        TMP_Text message = Text("Message", panel, "", new Vector2(0, 171), new Vector2(476, 50), 16);
        Button("Close", panel, "닫기", new Vector2(0, -219), new Vector2(136, 38), ui.Close);
        RectTransform viewport = Box("Viewport", panel, new Vector2(0, -14), new Vector2(472, 310), new Color(.04f, .065f, .08f));
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.GetComponent<Image>().raycastTarget = true;
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform content = new GameObject("Rows", typeof(RectTransform)).GetComponent<RectTransform>();
        content.SetParent(viewport, false);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(.5f, 1);
        content.sizeDelta = new Vector2(0, 0);
        VerticalLayoutGroup layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 6;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = content;

        ZoneChannelRowUI[] rows = new ZoneChannelRowUI[40];
        for (int i = 0; i < rows.Length; ++i)
        {
            Button button = Button("ChannelRow_" + (i + 1), content, "", Vector2.zero, new Vector2(450, 44), null);
            LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = 44;
            ZoneChannelRowUI row = button.gameObject.AddComponent<ZoneChannelRowUI>();
            Ref(row, "m_label", button.transform.Find("Label").GetComponent<TMP_Text>());
            Ref(row, "m_button", button);
            UnityEventTools.AddPersistentListener(button.onClick, row.Select);
            rows[i] = row;
        }
        Ref(ui, "m_panel", veil.gameObject);
        Ref(ui, "m_message", message);
        SerializedObject so = new SerializedObject(ui);
        SerializedProperty list = so.FindProperty("m_rows");
        list.arraySize = rows.Length;
        for (int i = 0; i < rows.Length; ++i) list.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        veil.gameObject.SetActive(false);
    }
    static void SetupZone(string _name, GameObject _uiPrefab)
    {
        string path = Root + "Scenes/" + _name + ".unity";
        Backup(path);
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        OfflineCombatSession[] sessions = UnityEngine.Object.FindObjectsOfType<OfflineCombatSession>(true);
        for (int i = 0; i < sessions.Length; ++i) UnityEngine.Object.DestroyImmediate(sessions[i].gameObject);
        ZoneGameplayUI[] oldUI = UnityEngine.Object.FindObjectsOfType<ZoneGameplayUI>(true);
        for (int i = 0; i < oldUI.Length; ++i) UnityEngine.Object.DestroyImmediate(oldUI[i].gameObject);
        PlayerObject[] players = UnityEngine.Object.FindObjectsOfType<PlayerObject>(true);
        for (int i = 0; i < players.Length; ++i) UnityEngine.Object.DestroyImmediate(players[i].gameObject);
        MonsterObject[] monsters = UnityEngine.Object.FindObjectsOfType<MonsterObject>(true);
        for (int i = 0; i < monsters.Length; ++i)
        {
            MonsterSpawnPoint spawn = monsters[i].GetComponentInParent<MonsterSpawnPoint>(true);
            if (spawn != null) spawn.gameObject.SetActive(false);
            else UnityEngine.Object.DestroyImmediate(monsters[i].gameObject);
        }
        GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(_uiPrefab);
        ui.SetActive(true);
        // 예전 샘플씬의 UserCount 등 실전에서 쓰지 않는 화면 UI만 제거
        Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; ++i)
        {
            if (canvases[i] == null || canvases[i].renderMode == RenderMode.WorldSpace) continue;
            if (canvases[i].transform.IsChildOf(ui.transform)) continue;
            if (canvases[i].GetComponentInChildren<ZoneMapUI>(true) != null) continue;
            UnityEngine.Object.DestroyImmediate(canvases[i].gameObject);
        }
        ZoneSceneNetwork network = UnityEngine.Object.FindObjectOfType<ZoneSceneNetwork>(true);
        if (network == null) network = new GameObject("ZoneSceneNetwork").AddComponent<ZoneSceneNetwork>();
        network.gameObject.SetActive(true);
        network.enabled = true;
        Ref(network, "m_localWarriorPrefab", Load("Prefab/Object/Player/LocalPlayerObject_Warrior.prefab"));
        Ref(network, "m_localArcherPrefab", Load("Prefab/Object/Player/LocalPlayerObject_Archer.prefab"));
        Ref(network, "m_remoteWarriorPrefab", Load("Prefab/Object/Player/RemotePlayerObject_Warrior.prefab"));
        Ref(network, "m_remoteArcherPrefab", Load("Prefab/Object/Player/RemotePlayerObject_Archer.prefab"));
        string[] names = { "Ghost", "Lizard", "Knight", "Rat" };
        SerializedObject so = new SerializedObject(network);
        SerializedProperty list = so.FindProperty("m_monsterPrefabList");
        list.arraySize = names.Length;
        for (int i = 0; i < names.Length; ++i) list.GetArrayElementAtIndex(i).objectReferenceValue = Load("Prefab/Object/Monster/MonsterObject_" + names[i] + ".prefab");
        so.ApplyModifiedPropertiesWithoutUndo();
        GameObject objectsRoot = GameObject.Find("NetworkObjects");
        if (objectsRoot == null) objectsRoot = new GameObject("NetworkObjects");
        Transform objects = objectsRoot.transform;
        Ref(network, "m_objectRoot", objects);

        ZoneMapUI map = UnityEngine.Object.FindObjectOfType<ZoneMapUI>(true);
        if (map == null) throw new Exception(_name + " 지도 UI 누락");
        Canvas mapCanvas = map.GetComponentInParent<Canvas>();
        mapCanvas.gameObject.SetActive(true);
        map.gameObject.SetActive(true);
        mapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mapCanvas.sortingOrder = 49;
        CanvasScaler scaler = mapCanvas.GetComponent<CanvasScaler>();
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = .5f;
        SerializedObject mapData = new SerializedObject(map);
        mapData.FindProperty("m_largeSize").vector2Value = new Vector2(600, 600);
        SerializedProperty markers = mapData.FindProperty("m_portalMarkerList");
        for (int i = 0; i < markers.arraySize; ++i)
        {
            RectTransform marker = (RectTransform)markers.GetArrayElementAtIndex(i).objectReferenceValue;
            marker.anchorMin = marker.anchorMax = new Vector2(.5f, .5f);
        }
        mapData.ApplyModifiedPropertiesWithoutUndo();
        Camera camera = Camera.main;
        if (camera == null) throw new Exception(_name + " MainCamera 누락");
        CameraController oldCamera = camera.GetComponent<CameraController>();
        if (oldCamera != null) oldCamera.enabled = false;
        PlayerFollowCamera follow = camera.GetComponent<PlayerFollowCamera>();
        if (follow == null) follow = camera.gameObject.AddComponent<PlayerFollowCamera>();
        follow.enabled = true;
        Ref(follow, "m_target", null);
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        if (_name == "TownScene")
        {
            FieldDataExporter exporter = UnityEngine.Object.FindObjectOfType<FieldDataExporter>(true);
            if (exporter == null) exporter = new GameObject("TownFieldExporter").AddComponent<FieldDataExporter>();
            MonsterSpawnExporter spawns = exporter.GetComponent<MonsterSpawnExporter>();
            if (spawns == null) spawns = exporter.gameObject.AddComponent<MonsterSpawnExporter>();
			spawns.SetExportInfo(1, Terrain.activeTerrain);
            Ref(exporter, "m_terrain", Terrain.activeTerrain);
            Ref(exporter, "m_monsterSpawnExporter", spawns);
            SerializedObject data = new SerializedObject(exporter);
            data.FindProperty("m_fileName").stringValue = "TownField.txt";
            data.FindProperty("m_zoneId").ulongValue = 1;
            data.FindProperty("m_requireNavMesh").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            exporter.Export();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ZoneGameplayUISetup] " + _name + " saved");
    }
    static void SetupLogin()
    {
        string path = Root + "Scenes/LoginScene.unity";
        Backup(path);
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        LoginScene login = UnityEngine.Object.FindObjectOfType<LoginScene>(true);
        LoginSceneNetwork network = login.GetComponent<LoginSceneNetwork>();
        if (network == null) network = login.gameObject.AddComponent<LoginSceneNetwork>();
        Ref(login, "m_network", network);
        LoginButton[] buttons = UnityEngine.Object.FindObjectsOfType<LoginButton>(true);
        for (int i = 0; i < buttons.Length; ++i) Ref(buttons[i], "m_network", network);
        CreateAccountOkButton[] createButtons = UnityEngine.Object.FindObjectsOfType<CreateAccountOkButton>(true);
        for (int i = 0; i < createButtons.Length; ++i) Ref(createButtons[i], "m_network", network);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
    static void FixWorldBars()
    {
        string[] paths = { "Prefab/UI/Player/PlayerWorldUI.prefab", "Prefab/UI/Monster/MonsterWorldUI.prefab" };
        for (int i = 0; i < paths.Length; ++i)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(Root + paths[i]);
            FixBars(root);
            PrefabUtility.SaveAsPrefabAsset(root, Root + paths[i]);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
    static void FixBars(GameObject _root)
    {
        Image[] images = _root.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; ++i)
        {
            if (images[i].type == Image.Type.Filled && images[i].fillMethod == Image.FillMethod.Horizontal) images[i].sprite = m_solid;
        }
    }
    static GameObject Load(string _path) { return AssetDatabase.LoadAssetAtPath<GameObject>(Root + _path); }
    static void Remove(Transform _target) { if (_target != null) UnityEngine.Object.DestroyImmediate(_target.gameObject); }
    static void Ref(UnityEngine.Object _owner, string _name, UnityEngine.Object _value)
    {
        SerializedObject so = new SerializedObject(_owner);
        SerializedProperty property = so.FindProperty(_name);
        if (property == null) throw new Exception(_owner.name + "." + _name + " 누락");
        property.objectReferenceValue = _value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static RectTransform Box(string _name, Transform _parent, Vector2 _position, Vector2 _size, Color _color)
    {
        RectTransform rect = new GameObject(_name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        rect.SetParent(_parent, false);
        rect.anchoredPosition = _position;
        rect.sizeDelta = _size;
        Image image = rect.GetComponent<Image>();
        image.color = _color;
        image.raycastTarget = false;
        return rect;
    }
    static TMP_Text Text(string _name, Transform _parent, string _text, Vector2 _position, Vector2 _size, float _sizeFont)
    {
        TMP_Text text = new GameObject(_name, typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(_parent, false);
        text.rectTransform.anchoredPosition = _position;
        text.rectTransform.sizeDelta = _size;
        text.font = m_font;
        text.text = _text;
        text.fontSize = _sizeFont;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
    static Button Button(string _name, Transform _parent, string _text, Vector2 _position, Vector2 _size, UnityAction _action)
    {
        RectTransform rect = Box(_name, _parent, _position, _size, new Color(.18f, .27f, .29f));
        rect.GetComponent<Image>().raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        Text("Label", rect, _text, Vector2.zero, _size - new Vector2(12, 4), 18);
        if (_action != null) UnityEventTools.AddPersistentListener(button.onClick, _action);
        return button;
    }
    static void Stretch(RectTransform _rect)
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = _rect.offsetMax = Vector2.zero;
    }
    public static void Build()
    {
        Directory.CreateDirectory("Build/Online");
        UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Root + "Scenes/LoginScene.unity", Root + "Scenes/BeginnerZoneScene.unity", Root + "Scenes/TownScene.unity" },
            locationPathName = "Build/Online/BraveForest.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Build failed");
        Debug.Log("[ZoneGameplayUISetup] BUILD SUCCEEDED");
    }
    public static void Inspect()
    {
        string[] names = { "BeginnerZoneScene", "TownScene" };
        for (int i = 0; i < names.Length; ++i)
        {
            EditorSceneManager.OpenScene(Root + "Scenes/" + names[i] + ".unity", OpenSceneMode.Single);
            ZonePortal[] portals = UnityEngine.Object.FindObjectsOfType<ZonePortal>(true);
            for (int j = 0; j < portals.Length; ++j)
            {
                Debug.Log("[WorldInspect] " + names[i] + " portal=" + portals[j].name + " position=" + portals[j].transform.position + " data=" + JsonUtility.ToJson(portals[j]));
            }
            ZoneSceneInfo info = UnityEngine.Object.FindObjectOfType<ZoneSceneInfo>();
            Debug.Log("[WorldInspect] " + names[i] + " " + JsonUtility.ToJson(info));
            Vector3[] probes = { new Vector3(50, 0, 20), new Vector3(50.5f, 0, 50.5f), new Vector3(15, 0, 18) };
            for (int j = 0; j < probes.Length; ++j)
            {
                UnityEngine.AI.NavMeshHit hit;
                bool success = UnityEngine.AI.NavMesh.SamplePosition(probes[j], out hit, 10, UnityEngine.AI.NavMesh.AllAreas);
                Debug.Log("[WorldInspect] " + names[i] + " probe=" + probes[j] + " nav=" + success + " hit=" + hit.position);
            }
        }
    }
}
