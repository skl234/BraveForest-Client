using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 실행 시 생성이 아니라 에디터에서 씬/프리팹을 미리 구성하는 도구입니다.
public static class CombatHUDSetup
{
    const string Root = "Assets/MYFolder/";
    static TMP_FontAsset font;
    static Color ink = new Color(0.06f, 0.09f, 0.12f, 0.96f);
    static Color gold = new Color(0.84f, 0.72f, 0.48f, 1);
    static Color mint = new Color(0.47f, 0.91f, 0.76f, 1);
    static Sprite panelSprite;
    public static void Run()
    {
        string scenePath = Root + "Scenes/BeginnerZoneScene.unity";
        Directory.CreateDirectory("Build/Validation/Backups");
        File.Copy(scenePath, "Build/Validation/Backups/BeforeHUD_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
        Directory.CreateDirectory(Root + "Prefab/UI");
        AssetDatabase.Refresh();
        string[] icons = { "WarriorBasic", "WarriorHeavy", "WarriorFire", "ArcherBasic", "ArcherPower", "ArcherRapid", "Potion" };
        for (int i = 0; i < icons.Length; ++i)
        {
            TextureImporter importer = AssetImporter.GetAtPath(Root + "Image/Skills/" + icons[i] + ".png") as TextureImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 512;
            importer.SaveAndReimport();
        }
        OfflineCombatSetup.CreateEffect();
        RuntimeAnimatorController bow = BowController();
        ArrowShotEffect basic = Arrow("ArrowBasic", new Color(0.9f, 1, 0.7f), 0.045f, 0.16f);
        ArrowShotEffect power = Arrow("ArrowPower", new Color(1, 0.8f, 0.15f), 0.28f, 0.24f);
        ArrowShotEffect rapid = Arrow("ArrowRapid", new Color(0.15f, 1, 0.88f), 0.1f, 0.17f);
        string warriorPath = Root + "Prefab/Object/Player/LocalPlayerObject_Warrior.prefab";
        string archerPath = Root + "Prefab/Object/Player/LocalPlayerObject_Archer.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(archerPath) == null) AssetDatabase.CopyAsset(warriorPath, archerPath);
        string[] players = { warriorPath, archerPath };
        for (int i = 0; i < players.Length; ++i)
        {
            GameObject prefab = PrefabUtility.LoadPrefabContents(players[i]);
            if (prefab.GetComponent<PlayerInfo>() == null) prefab.AddComponent<PlayerInfo>();
            PlayerAttackEffects effects = prefab.GetComponent<PlayerAttackEffects>();
            if (effects == null) effects = prefab.AddComponent<PlayerAttackEffects>();
            Ref(effects, "m_arrowBasic", basic); Ref(effects, "m_arrowPower", power); Ref(effects, "m_arrowRapid", rapid);
            if (i == 1)
            {
                Int(prefab.GetComponent<LocalPlayerObject>(), "m_playerClass", 1);
                Ref(prefab.GetComponent<LocalPlayerObject>(), "m_gameplayController", bow);
            }
            PrefabUtility.SaveAsPrefabAsset(prefab, players[i]);
            PrefabUtility.UnloadPrefabContents(prefab);
        }
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        LocalPlayerObject warrior = null;
        LocalPlayerObject[] localPlayers = UnityEngine.Object.FindObjectsOfType<LocalPlayerObject>(true);
        for (int i = 0; i < localPlayers.Length; ++i) if (localPlayers[i].GetPlayerClass() == ePlayerClass.Warrior) warrior = localPlayers[i];
        if (warrior == null) throw new Exception("Warrior missing");
        Transform parent = warrior.transform.parent;
        Transform oldArcher = parent.Find("LocalPlayerObject_Archer");
        GameObject archer = oldArcher != null ? oldArcher.gameObject : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(archerPath), parent);
        archer.name = "LocalPlayerObject_Archer";
        if (!UnityEngine.AI.NavMesh.SamplePosition(new Vector3(45, .5f, 40), out UnityEngine.AI.NavMeshHit spawn, 4, UnityEngine.AI.NavMesh.AllAreas)) throw new Exception("Test clearing missing");
        warrior.transform.position = spawn.position;
        archer.transform.position = spawn.position;
        MonsterObject[] allMonsters = UnityEngine.Object.FindObjectsOfType<MonsterObject>(true);
        for (int i = 0; i < allMonsters.Length; ++i) allMonsters[i].gameObject.SetActive(false);
        MonsterHitbox[] monsters = new MonsterHitbox[3];
        Vector3[] offsets = { new Vector3(6,0,0), new Vector3(7,0,3), new Vector3(4,0,5) };
        for (int i = 0; i < 3; ++i)
        {
            monsters[i] = parent.Find("CombatTest_Rat_" + (i + 1)).GetComponentInChildren<MonsterHitbox>(true);
            monsters[i].transform.parent.name = "CombatTest_Rat_" + (i + 1);
            if (!UnityEngine.AI.NavMesh.SamplePosition(spawn.position + offsets[i], out UnityEngine.AI.NavMeshHit position, 3, UnityEngine.AI.NavMesh.AllAreas)) throw new Exception("Monster clearing missing");
            monsters[i].transform.parent.position = position.position;
        }
        warrior.gameObject.SetActive(false); archer.SetActive(false);
        GameObject previous = GameObject.Find("CombatCanvas");
        if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Maplestory Bold SDF.asset");
        panelSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        GameObject canvasObject = new GameObject("CombatCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 0.5f;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        OfflineCombatSession session = canvasObject.AddComponent<OfflineCombatSession>();
        GameObject selection = Selection(canvasObject.transform, session);
        CombatHUD hud = HUD(canvasObject.transform, session);
        Ref(session, "m_selectionPanel", selection); Ref(session, "m_hud", hud);
        // 외부 씬 참조 없이 UI 전체를 프리팹으로 먼저 저장합니다.
        canvas.worldCamera = null;
        UnifiedUISetup.ApplyZone(canvasObject);
        GameObject uiPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(canvasObject, Root + "Prefab/UI/CombatInterface.prefab", InteractionMode.AutomatedAction);
        canvas.worldCamera = Camera.main;
        Ref(session, "m_warrior", warrior); Ref(session, "m_archer", archer.GetComponent<LocalPlayerObject>());
        Ref(session, "m_camera", Camera.main.GetComponent<PlayerFollowCamera>());
        Array(session, "m_monsters", monsters);
        hud.gameObject.SetActive(false);
        if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EditorSceneManager.MarkSceneDirty(warrior.gameObject.scene);
        EditorSceneManager.SaveScene(warrior.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[HUDSetup] COMPLETE: saved static selection, HUD, archer, three monsters");
    }

    static RuntimeAnimatorController BowController()
    {
        string path = Root + "Animation/Combat/ArcherCombat.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        string[] names = { "Idle_Bow", "Run_Bow", "Attack01_Bow", "Attack02RepeatFire_Bow" };
        for (int i = 0; i < names.Length; ++i)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/ModularRPGHeroesPolyArt/Animations/Bow/" + names[i] + ".fbx");
            AnimationClip clip = null;
            for (int j = 0; j < assets.Length; ++j) if (assets[j] is AnimationClip && !assets[j].name.StartsWith("__")) { clip = (AnimationClip)assets[j]; break; }
            if (clip == null) throw new Exception(names[i] + " missing");
            AnimatorState state = controller.layers[0].stateMachine.AddState(names[i]);
            state.motion = clip;
            if (i == 0) controller.layers[0].stateMachine.defaultState = state;
            Debug.Log("[HUDSetup] " + names[i] + " actual=" + clip.name + " length=" + clip.length);
        }
        return controller;
    }

    static ArrowShotEffect Arrow(string name, Color color, float width, float duration)
    {
        string path = Root + "Prefab/Object/Effect/" + name + ".prefab";
        GameObject root = new GameObject(name);
        ArrowShotEffect shot = root.AddComponent<ArrowShotEffect>();
        LineRenderer line = root.AddComponent<LineRenderer>();
        string materialPath = Root + "Prefab/Object/Effect/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(Shader.Find("Sprites/Default")); AssetDatabase.CreateAsset(material, materialPath); }
        material.color = color;
        line.sharedMaterial = material; line.useWorldSpace = true; line.positionCount = 2;
        line.startWidth = width * 0.3f; line.endWidth = width; line.numCapVertices = 4;
        Ref(shot, "m_trail", line); Float(shot, "m_duration", duration);
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab.GetComponent<ArrowShotEffect>();
    }

    static GameObject Selection(Transform parent, OfflineCombatSession session)
    {
        RectTransform veil = Box("ClassSelection", parent, Vector2.zero, Vector2.zero, new Color(0.035f, 0.055f, 0.075f, 0.86f));
        Stretch(veil);
        RectTransform panel = Box("SelectionCard", veil, Vector2.zero, new Vector2(740, 460), ink);
        Frame(panel, gold);
        Text("Eyebrow", panel, "BEGINNER GROVE  /  OFFLINE PLAYGROUND", new Vector2(0, 181), new Vector2(680, 30), 14, mint);
        Text("Title", panel, "오늘은 어떤 모험가가 될까요?", new Vector2(0, 138), new Vector2(690, 60), 30, Color.white);
        Text("Subtitle", panel, "직업을 선택하면 세 마리의 연습 몬스터와 함께 시작해요.", new Vector2(0, 91), new Vector2(690, 40), 16, new Color(.76f,.82f,.83f));
        Button warrior = Button("ChooseWarrior", panel, "", new Vector2(-169, -38), new Vector2(290, 205), new Color(.2f,.18f,.18f), session.ChooseWarrior);
        Button archer = Button("ChooseArcher", panel, "", new Vector2(169, -38), new Vector2(290, 205), new Color(.12f,.22f,.22f), session.ChooseArcher);
        ClassCard(warrior.transform, "WarriorHeavy", "전사", "검과 방패 · 강타 · 화염 강타", gold);
        ClassCard(archer.transform, "ArcherPower", "궁수", "원거리 사격 · 강화 화살 · 속사", mint);
        Text("Help", panel, "피해량 계산 없이 타격 · 타겟 전환 · 스킬 동작을 테스트합니다.", new Vector2(0, -188), new Vector2(690, 36), 14, new Color(.76f,.82f,.83f));
        return veil.gameObject;
    }
    static void ClassCard(Transform parent, string icon, string name, string help, Color accent)
    {
        Image image = Box("ClassIcon", parent, new Vector2(0, 32), new Vector2(92,92), Color.white).GetComponent<Image>();
        image.sprite = Icon(icon); image.raycastTarget = false;
        Text("Name", parent, name, new Vector2(0,-34), new Vector2(270,35), 26, accent);
        Text("Description", parent, help, new Vector2(0,-70), new Vector2(280,35), 13, Color.white);
    }
    static CombatHUD HUD(Transform parent, OfflineCombatSession session)
    {
        RectTransform root = new GameObject("HUD", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false); Stretch(root);
        CombatHUD hud = root.gameObject.AddComponent<CombatHUD>();
        RectTransform bar = Box("ActionBar", root, new Vector2(0, 85), new Vector2(760,146), ink);
        bar.anchorMin = bar.anchorMax = new Vector2(.5f, 0); Frame(bar, gold);
        RectTransform vitals = Box("PlayerVitals", bar, new Vector2(-204, 0), new Vector2(304,118), new Color(.1f,.15f,.17f,1));
        TMP_Text level = Text("Level", vitals, "Lv. 10", new Vector2(-105,36), new Vector2(84,32), 18, gold);
        TMP_Text name = Text("Name", vitals, "새싹 모험가 · 전사", new Vector2(30,36), new Vector2(208,32), 16, Color.white);
        RectTransform hpBack = Box("HealthTrack", vitals, new Vector2(0,1), new Vector2(270,20), new Color(.04f,.06f,.07f));
        Image fill = Box("HealthFill", hpBack, Vector2.zero, new Vector2(264,12), new Color(.24f,.78f,.53f)).GetComponent<Image>();
        // 낮은 높이에 9-sliced 기본 스프라이트를 사용하면 내부가 두꺼운 타원처럼 보입니다.
        // 게이지 안쪽은 모서리가 단정한 직선형 막대로 표시합니다.
        fill.sprite = null; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal;
        TMP_Text hp = Text("HealthText", hpBack, "1000 / 1000", Vector2.zero, new Vector2(264,24), 15, Color.white);
		RectTransform experienceBack = Box("ExperienceTrack", vitals, new Vector2(0,-32), new Vector2(270,15), new Color(.04f,.06f,.07f));
		Image experienceFill = Box("ExperienceFill", experienceBack, Vector2.zero, new Vector2(264,9), new Color(.32f,.56f,.95f)).GetComponent<Image>();
		experienceFill.sprite = null; experienceFill.type = Image.Type.Filled; experienceFill.fillMethod = Image.FillMethod.Horizontal;
		TMP_Text experience = Text("ExperienceText", experienceBack, "EXP  0 / 10000", Vector2.zero, new Vector2(264,20), 11, Color.white);
        Ref(hud,"m_name",name); Ref(hud,"m_level",level); Ref(hud,"m_hp",hp); Ref(hud,"m_hpFill",fill);
		Ref(hud,"m_experience",experience); Ref(hud,"m_experienceFill",experienceFill);
		SkillSlotUI[] slots = new SkillSlotUI[4];
        slots[0] = Slot(bar, 24, "LMB", "WarriorBasic", "ArcherBasic", "일반 공격", "일반 화살", session.BasicAttack);
        slots[1] = Slot(bar, 148, "A", "WarriorHeavy", "ArcherPower", "강타", "강화 화살", session.SkillA);
        slots[2] = Slot(bar, 272, "S", "WarriorFire", "ArcherRapid", "화염 강타", "속사", session.SkillS);
		slots[3] = Slot(bar, 396, "D", "Potion", "Potion", "회복 물약 ∞", "회복 물약 ∞", session.UsePotion);
        Array(hud,"m_slots",slots);
        RectTransform targetPanel = Box("TargetPanel", root, new Vector2(0,-44), new Vector2(570,48), ink);
        targetPanel.anchorMin = targetPanel.anchorMax = new Vector2(.5f,1); Frame(targetPanel,mint);
        Ref(hud,"m_target",Text("TargetText",targetPanel,"선택한 대상 없음",Vector2.zero,new Vector2(545,42),16,Color.white));
        RectTransform status = new GameObject("Status",typeof(RectTransform)).GetComponent<RectTransform>();
        status.SetParent(root,false); status.anchorMin = status.anchorMax = new Vector2(.5f,0);
        status.anchoredPosition = new Vector2(0,177); status.sizeDelta = new Vector2(600,30);
        Ref(hud,"m_status",Text("StatusText",status,"",Vector2.zero,new Vector2(600,30),17,gold));
		Chat(root);
        RectTransform channel = Box("ChannelPanel",root,new Vector2(0,222),new Vector2(310,28),ink);
        channel.anchorMin = channel.anchorMax = new Vector2(.5f,0);
        Image channelFill=Box("ChannelFill",channel,Vector2.zero,new Vector2(300,12),new Color(.12f,.67f,.58f)).GetComponent<Image>();
        channelFill.sprite=null; channelFill.type=Image.Type.Filled; channelFill.fillMethod=Image.FillMethod.Horizontal;
        Ref(hud,"m_channelPanel",channel.gameObject); Ref(hud,"m_channelFill",channelFill);
        Ref(hud,"m_channelText",Text("ChannelTime",channel,"속사 중",Vector2.zero,new Vector2(300,30),16,Color.white));
        channel.gameObject.SetActive(false);
        RectTransform tools=Box("TestTools",root,new Vector2(-112,-119),new Vector2(194,202),ink);
        tools.anchorMin=tools.anchorMax=new Vector2(1,1); Frame(tools,gold);
        Text("Label",tools,"TEST CONTROLS",new Vector2(0,77),new Vector2(182,28),13,gold);
        Button movement=Button("MonsterMovement",tools,"몬스터 이동",new Vector2(0,40),new Vector2(166,30),new Color(.24f,.32f,.22f),session.ToggleMonsterMovement);
        Ref(session,"m_monsterMovementLabel",movement.transform.Find("Label").GetComponent<TMP_Text>());
        Button("KillTarget",tools,"선택 대상 사망",new Vector2(0,2),new Vector2(166,30),new Color(.29f,.22f,.24f),session.KillTarget);
        Button("ResetMonsters",tools,"몬스터 초기화",new Vector2(0,-36),new Vector2(166,30),new Color(.18f,.3f,.28f),session.ResetMonsters);
        Button("ChangeClass",tools,"직업 다시 선택",new Vector2(0,-74),new Vector2(166,30),new Color(.24f,.26f,.3f),session.OpenSelection);
        RectTransform guide=Box("InputGuide",root,new Vector2(156,-74),new Vector2(284,112),ink);
        guide.anchorMin=guide.anchorMax=new Vector2(0,1);
        Text("Controls",guide,"BEGINNER GROVE\n우클릭 이동 · 좌클릭 타겟 공격\nA / S 스킬 · 휠 줌",Vector2.zero,new Vector2(270,100),15,new Color(.8f,.88f,.88f));
        return hud;
    }

	static void Chat(Transform parent)
	{
		RectTransform panel = Box("ZoneChat", parent, new Vector2(218, 145), new Vector2(410, 240), new Color(.025f,.04f,.055f,.82f));
		panel.anchorMin = panel.anchorMax = new Vector2(0, 0);
		Frame(panel, new Color(.28f,.42f,.46f,1));
		ZoneChatUI chat = panel.gameObject.AddComponent<ZoneChatUI>();
		RectTransform viewport = Box("Viewport", panel, new Vector2(0, 18), new Vector2(386, 176), new Color(.01f,.02f,.025f,.45f));
		Mask mask = viewport.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
		ScrollRect scroll = panel.gameObject.AddComponent<ScrollRect>();
		scroll.viewport = viewport; scroll.horizontal = false; scroll.vertical = true;
		TMP_Text history = Text("ChatHistory", viewport, "", Vector2.zero, new Vector2(374, 176), 14, Color.white);
		history.alignment = TextAlignmentOptions.BottomLeft; history.enableWordWrapping = true;
		RectTransform historyRect = history.rectTransform; historyRect.anchorMin = new Vector2(0,0); historyRect.anchorMax = new Vector2(1,1);
		historyRect.offsetMin = new Vector2(6,4); historyRect.offsetMax = new Vector2(-6,-4); scroll.content = historyRect;
		RectTransform inputBack = Box("Input", panel, new Vector2(0,-91), new Vector2(386,36), new Color(.08f,.11f,.13f,.98f));
		inputBack.GetComponent<Image>().raycastTarget = true;
		TMP_InputField input = inputBack.gameObject.AddComponent<TMP_InputField>();
		TMP_Text inputText = Text("Text", inputBack, "", Vector2.zero, new Vector2(366,30), 14, Color.white);
		inputText.alignment = TextAlignmentOptions.MidlineLeft;
		TMP_Text placeholder = Text("Placeholder", inputBack, "Enter로 채팅 입력 (최대 30바이트)", Vector2.zero, new Vector2(366,30), 13, new Color(.55f,.62f,.64f));
		placeholder.alignment = TextAlignmentOptions.MidlineLeft;
		input.textViewport = inputBack; input.textComponent = inputText; input.placeholder = placeholder;
		Ref(chat,"m_chatText",history); Ref(chat,"m_inputField",input); Ref(chat,"m_scrollRect",scroll);
	}

    static SkillSlotUI Slot(Transform parent,float x,string key,string warrior,string archer,string wName,string aName,UnityAction action)
    {
        Button button=Button("Skill_"+key,parent,"",new Vector2(x,8),new Vector2(86,86),gold,action);
        SkillSlotUI slot=button.gameObject.AddComponent<SkillSlotUI>();
        Image icon=Box("Icon",button.transform,Vector2.zero,new Vector2(80,80),Color.white).GetComponent<Image>(); icon.sprite=Icon(warrior); icon.raycastTarget=false;
        Image mask=Box("CooldownMask",button.transform,Vector2.zero,new Vector2(80,80),new Color(.2f,.23f,.27f,.86f)).GetComponent<Image>();
        mask.sprite=TextureSprite(); mask.type=Image.Type.Filled; mask.fillMethod=Image.FillMethod.Radial360; mask.fillOrigin=2; mask.fillClockwise=true; mask.raycastTarget=false;
        TMP_Text seconds=Text("Seconds",button.transform,"",Vector2.zero,new Vector2(78,72),34,Color.white);
        RectTransform keyBack=Box("Key",button.transform,new Vector2(-29,31),new Vector2(key=="LMB"?37:25,22),ink);
        Text("KeyText",keyBack,key,Vector2.zero,new Vector2(37,22),12,gold);
        TMP_Text label=Text("SkillName",button.transform,wName,new Vector2(0,-57),new Vector2(120,28),14,Color.white);
        Ref(slot,"m_icon",icon); Ref(slot,"m_cooldownMask",mask); Ref(slot,"m_seconds",seconds); Ref(slot,"m_skillName",label);
        Ref(slot,"m_warriorIcon",Icon(warrior)); Ref(slot,"m_archerIcon",Icon(archer)); Str(slot,"m_warriorName",wName); Str(slot,"m_archerName",aName);
        return slot;
    }
    static Sprite TextureSprite() { return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"); }
    static Sprite Icon(string name) { return AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Image/Skills/"+name+".png"); }
    static RectTransform Box(string name,Transform parent,Vector2 position,Vector2 size,Color color)
    {
        GameObject obj=new GameObject(name,typeof(RectTransform),typeof(Image));
        RectTransform rect=obj.GetComponent<RectTransform>(); rect.SetParent(parent,false); rect.anchoredPosition=position; rect.sizeDelta=size;
        Image image=obj.GetComponent<Image>(); image.color=color; image.sprite=panelSprite; image.type=Image.Type.Sliced; image.raycastTarget=false;
        return rect;
    }
    static void Frame(RectTransform rect,Color color) { Outline outline=rect.gameObject.AddComponent<Outline>(); outline.effectColor=color; outline.effectDistance=new Vector2(1,-1); }
    static TMP_Text Text(string name,Transform parent,string value,Vector2 position,Vector2 size,int fontSize,Color color)
    {
        GameObject obj=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); RectTransform rect=obj.GetComponent<RectTransform>(); rect.SetParent(parent,false); rect.anchoredPosition=position; rect.sizeDelta=size;
        TMP_Text text=obj.GetComponent<TMP_Text>(); text.font=font; text.text=value; text.fontSize=fontSize; text.color=color; text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false;
        return text;
    }
    static Button Button(string name,Transform parent,string title,Vector2 position,Vector2 size,Color color,UnityAction action)
    {
        RectTransform rect=Box(name,parent,position,size,color); rect.GetComponent<Image>().raycastTarget=true;
        Button button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>();
        ColorBlock colors=button.colors; colors.highlightedColor=new Color(1.2f,1.2f,1.2f); colors.pressedColor=new Color(.65f,.75f,.7f); button.colors=colors;
        UnityEventTools.AddPersistentListener(button.onClick,action);
        if (title.Length>0) Text("Label",rect,title,Vector2.zero,size,14,Color.white);
        return button;
    }
    static void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=Vector2.zero; rect.offsetMax=Vector2.zero; }
    static void Ref(UnityEngine.Object obj,string field,UnityEngine.Object value) { SerializedObject so=new SerializedObject(obj); so.FindProperty(field).objectReferenceValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Int(UnityEngine.Object obj,string field,int value) { SerializedObject so=new SerializedObject(obj); so.FindProperty(field).intValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Float(UnityEngine.Object obj,string field,float value) { SerializedObject so=new SerializedObject(obj); so.FindProperty(field).floatValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Str(UnityEngine.Object obj,string field,string value) { SerializedObject so=new SerializedObject(obj); so.FindProperty(field).stringValue=value; so.ApplyModifiedPropertiesWithoutUndo(); }
    static void Array(UnityEngine.Object obj,string field,UnityEngine.Object[] values) { SerializedObject so=new SerializedObject(obj); SerializedProperty p=so.FindProperty(field); p.arraySize=values.Length; for(int i=0;i<values.Length;++i)p.GetArrayElementAtIndex(i).objectReferenceValue=values[i]; so.ApplyModifiedPropertiesWithoutUndo(); }
}
