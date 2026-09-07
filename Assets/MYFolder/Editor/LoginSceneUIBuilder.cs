using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LoginSceneUIBuilder
{
    const string ScenePath = "Assets/MyFolder/Scenes/LoginScene.unity";

    [MenuItem("MMORPG/Login Scene/Rebuild Static UI")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform canvas = Find(scene, "Canvas");
        Transform loginPanel = Find(scene, "Login_Panel");
        Transform characterPanel = Find(scene, "Character_Panel");
        LoginScene loginScene = Object.FindObjectOfType<LoginScene>(true);
        TMP_Text textStyle = loginPanel.GetComponentInChildren<TMP_Text>(true);
        Button buttonStyle = loginPanel.GetComponentInChildren<Button>(true);
        Image panelStyle = loginPanel.GetComponent<Image>();

        DeleteChild(loginPanel, "Character_Showcase");
        DeleteChild(characterPanel, "Character_Selection");
        DeleteChild(canvas, "Notice_Panel");

        CharacterShowcase showcase = BuildShowcase(loginPanel);
        CharacterSelectionPanel selection = BuildCharacterSelection(characterPanel, textStyle, buttonStyle);
        NoticePanelUI notice = BuildNotice(canvas, textStyle, buttonStyle, panelStyle);

        SerializedObject loginObject = new SerializedObject(loginScene);
        loginObject.FindProperty("m_noticePanel").objectReferenceValue = notice;
        loginObject.FindProperty("m_characterSelectionPanel").objectReferenceValue = selection;
        loginObject.FindProperty("m_characterShowcase").objectReferenceValue = showcase;
        loginObject.ApplyModifiedPropertiesWithoutUndo();

        notice.gameObject.SetActive(false);
        selection.transform.Find("Create_Character_Dialog").gameObject.SetActive(false);
        UnifiedUISetup.ApplyLogin(loginScene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("LoginScene static UI build completed.");
    }

    public static void Validate()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        LoginScene loginScene = Find(scene, "LoginScene").GetComponent<LoginScene>();
        CharacterSelectionPanel panel = Find(scene, "Character_Selection").GetComponent<CharacterSelectionPanel>();
        NoticePanelUI notice = Find(scene, "Notice_Panel").GetComponent<NoticePanelUI>();
        CharacterShowcase showcase = Find(scene, "Character_Showcase").GetComponent<CharacterShowcase>();
        if (loginScene == null || panel == null || notice == null || showcase == null)
        {
            throw new System.InvalidOperationException("LoginScene static UI component reference is missing.");
        }

        SerializedObject loginObject = new SerializedObject(loginScene);
        GameObject warriorPrefab = loginObject.FindProperty("m_warriorPrefab").objectReferenceValue as GameObject;
        GameObject archerPrefab = loginObject.FindProperty("m_archerPrefab").objectReferenceValue as GameObject;
        panel.Initialize(warriorPrefab, archerPrefab, null, null);

        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        System.Reflection.MethodInfo openMethod = typeof(CharacterSelectionPanelBase).GetMethod("OpenCreateDialog", flags);
        System.Reflection.MethodInfo selectMethod = typeof(CharacterSelectionPanelBase).GetMethod("SelectJob", flags);
        System.Reflection.FieldInfo nicknameField = typeof(CharacterSelectionPanelBase).GetField("m_nicknameField", flags);
        System.Reflection.FieldInfo confirmField = typeof(CharacterSelectionPanelBase).GetField("m_confirmButton", flags);
        System.Reflection.FieldInfo warriorField = typeof(CharacterSelectionPanelBase).GetField("m_warriorButton", flags);
        openMethod.Invoke(panel, new object[] { 0 });
        TMP_InputField nickname = nicknameField.GetValue(panel) as TMP_InputField;
        Button confirm = confirmField.GetValue(panel) as Button;
        Button warrior = warriorField.GetValue(panel) as Button;
        if (confirm.interactable) throw new System.InvalidOperationException("Create button must be disabled before input.");
        selectMethod.Invoke(panel, new object[] { 0 });
        if (confirm.interactable) throw new System.InvalidOperationException("Create button must remain disabled without nickname.");
        nickname.text = "검증캐릭터";
        if (!confirm.interactable) throw new System.InvalidOperationException("Create button was not enabled after valid input.");
        if (warrior.image.color == Color.white) throw new System.InvalidOperationException("Selected job visual was not applied.");
        nickname.text = "가나다라마바사아자차카타파하가나다";
        if (confirm.interactable) throw new System.InvalidOperationException("Create button must be disabled for a nickname over 30 UTF-8 bytes.");

        Transform canvas = Find(scene, "Canvas");
        if (canvas.Find("Notice_Panel/Dialog/Message_Text") == null
            || canvas.Find("Character_Panel/Character_Selection/Create_Character_Dialog/Window/Job_Selection_Group/Warrior_Button") == null)
        {
            throw new System.InvalidOperationException("LoginScene UI parent-child hierarchy is invalid.");
        }
        Debug.Log("LoginScene static UI validation completed.");
    }

    static CharacterShowcase BuildShowcase(Transform _parent)
    {
        GameObject root = UI("Character_Showcase", _parent);
        Stretch(root.GetComponent<RectTransform>());
        root.transform.SetAsFirstSibling();
        CharacterShowcase showcase = root.AddComponent<CharacterShowcase>();
        RawImage warrior = Raw("Warrior_Preview_Image", root.transform, new Vector2(0.2f, 0.5f), new Vector2(400.0f, 600.0f));
        RawImage archer = Raw("Archer_Preview_Image", root.transform, new Vector2(0.8f, 0.5f), new Vector2(400.0f, 600.0f));

        SerializedObject serializedObject = new SerializedObject(showcase);
        serializedObject.FindProperty("m_warriorImage").objectReferenceValue = warrior;
        serializedObject.FindProperty("m_archerImage").objectReferenceValue = archer;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return showcase;
    }

    static CharacterSelectionPanel BuildCharacterSelection(Transform _parent, TMP_Text _textStyle, Button _buttonStyle)
    {
        GameObject root = UI("Character_Selection", _parent);
        Stretch(root.GetComponent<RectTransform>());
        CharacterSelectionPanel panel = root.AddComponent<CharacterSelectionPanel>();

        GameObject titleBackground = Background("Title_Background", root.transform, new Vector2(0.5f, 0.91f), new Vector2(900.0f, 90.0f), new Color(0, 0, 0, 0.58f));
        Text("Title_Text", "캐릭터 선택", titleBackground.transform, Vector2.zero, Vector2.one, 46.0f, _textStyle, Color.white);

        GameObject slotsRoot = UI("Character_Slots", root.transform);
        Stretch(slotsRoot.GetComponent<RectTransform>());
        Object[,] slots = new Object[2, 5];
        for (int i = 0; i < 2; ++i)
        {
            float x = i == 0 ? -400.0f : 400.0f;
            GameObject slot = UI("Character_Slot_" + (i + 1), slotsRoot.transform);
            SetRect(slot.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(430.0f, 760.0f), new Vector2(x, -10.0f));
            TMP_Text name = Text("Character_Name", "", slot.transform, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.84f), 34.0f, _textStyle, Color.white);
            RawImage preview = Raw("Character_Preview", slot.transform, new Vector2(0.5f, 0.5f), new Vector2(350.0f, 520.0f));
            preview.transform.SetAsFirstSibling();
            GameObject infoBackground = Background("Character_Info_Background", slot.transform, new Vector2(0.5f, 0.25f), new Vector2(344.0f, 76.0f), new Color(0, 0, 0, 0.55f));
            TMP_Text info = Text("Character_Info", "", infoBackground.transform, Vector2.zero, Vector2.one, 25.0f, _textStyle, Color.white);
            Button action = Button("Slot_Action_Button", "접속", slot.transform, new Vector2(0.5f, 0.095f), new Vector2(280.0f, 72.0f), _textStyle, _buttonStyle);
            slots[i, 0] = name;
            slots[i, 1] = info;
            slots[i, 2] = preview;
            slots[i, 3] = action;
            slots[i, 4] = action.GetComponentInChildren<TMP_Text>(true);
        }

        GameObject statusBackground = Background("Status_Background", root.transform, new Vector2(0.5f, 0.08f), new Vector2(900.0f, 55.0f), new Color(0, 0, 0, 0.62f));
        TMP_Text status = Text("Status_Text", "캐릭터 목록을 불러오는 중입니다.", statusBackground.transform, Vector2.zero, Vector2.one, 26.0f, _textStyle, Color.white);

        GameObject dialog = UI("Create_Character_Dialog", root.transform);
        Stretch(dialog.GetComponent<RectTransform>());
        Image overlay = dialog.AddComponent<Image>();
        overlay.color = new Color(0, 0, 0, 0.72f);
        GameObject window = Background("Window", dialog.transform, new Vector2(0.5f, 0.5f), new Vector2(720.0f, 570.0f), new Color(0.32f, 0.32f, 0.32f, 0.99f));
        Outline outline = window.AddComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.08f, 0.08f, 1.0f);
        outline.effectDistance = new Vector2(3.0f, -3.0f);
        Text("Title", "캐릭터 생성", window.transform, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.94f), 38.0f, _textStyle, Color.white);
        Text("Job_Label", "직업 선택", window.transform, new Vector2(0.15f, 0.64f), new Vector2(0.85f, 0.74f), 25.0f, _textStyle, Color.white);

        GameObject jobGroup = UI("Job_Selection_Group", window.transform);
        SetRect(jobGroup.GetComponent<RectTransform>(), new Vector2(0.5f, 0.57f), new Vector2(500.0f, 72.0f), Vector2.zero);
        Button warrior = Button("Warrior_Button", "전사", jobGroup.transform, new Vector2(0.2f, 0.5f), new Vector2(190.0f, 70.0f), _textStyle, _buttonStyle);
        Button archer = Button("Archer_Button", "궁수", jobGroup.transform, new Vector2(0.8f, 0.5f), new Vector2(190.0f, 70.0f), _textStyle, _buttonStyle);

        TMP_InputField nickname = InputField(window.transform, _textStyle);
        TMP_Text validation = Text("Validation_Text", "", window.transform, new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.31f), 21.0f, _textStyle, new Color(1.0f, 0.65f, 0.55f, 1.0f));
        GameObject buttonGroup = UI("Button_Group", window.transform);
        SetRect(buttonGroup.GetComponent<RectTransform>(), new Vector2(0.5f, 0.12f), new Vector2(520.0f, 72.0f), Vector2.zero);
        Button confirm = Button("Confirm_Button", "생성", buttonGroup.transform, new Vector2(0.24f, 0.5f), new Vector2(210.0f, 70.0f), _textStyle, _buttonStyle);
        Button cancel = Button("Cancel_Button", "취소", buttonGroup.transform, new Vector2(0.76f, 0.5f), new Vector2(210.0f, 70.0f), _textStyle, _buttonStyle);
        confirm.interactable = false;

        SerializedObject serializedObject = new SerializedObject(panel);
        SerializedProperty slotProperty = serializedObject.FindProperty("m_slots");
        slotProperty.arraySize = 2;
        for (int i = 0; i < 2; ++i)
        {
            SerializedProperty element = slotProperty.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("m_nameText").objectReferenceValue = slots[i, 0];
            element.FindPropertyRelative("m_infoText").objectReferenceValue = slots[i, 1];
            element.FindPropertyRelative("m_previewImage").objectReferenceValue = slots[i, 2];
            element.FindPropertyRelative("m_connectButton").objectReferenceValue = slots[i, 3];
            element.FindPropertyRelative("m_buttonText").objectReferenceValue = slots[i, 4];
        }
        serializedObject.FindProperty("m_statusText").objectReferenceValue = status;
        serializedObject.FindProperty("m_createDialog").objectReferenceValue = dialog;
        serializedObject.FindProperty("m_nicknameField").objectReferenceValue = nickname;
        serializedObject.FindProperty("m_warriorButton").objectReferenceValue = warrior;
        serializedObject.FindProperty("m_archerButton").objectReferenceValue = archer;
        serializedObject.FindProperty("m_warriorButtonText").objectReferenceValue = warrior.GetComponentInChildren<TMP_Text>(true);
        serializedObject.FindProperty("m_archerButtonText").objectReferenceValue = archer.GetComponentInChildren<TMP_Text>(true);
        serializedObject.FindProperty("m_validationText").objectReferenceValue = validation;
        serializedObject.FindProperty("m_confirmButton").objectReferenceValue = confirm;
        serializedObject.FindProperty("m_cancelButton").objectReferenceValue = cancel;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    static NoticePanelUI BuildNotice(Transform _parent, TMP_Text _textStyle, Button _buttonStyle, Image _panelStyle)
    {
        return UnifiedUISetup.AttachNotice(_parent);
    }

    static TMP_InputField InputField(Transform _parent, TMP_Text _style)
    {
        GameObject fieldObject = Background("Nickname_Field", _parent, new Vector2(0.5f, 0.39f), new Vector2(500.0f, 65.0f), new Color(0.92f, 0.92f, 0.92f, 1.0f));
        TMP_InputField field = fieldObject.AddComponent<TMP_InputField>();
        field.characterLimit = 30;
        GameObject area = UI("Text_Area", fieldObject.transform);
        RectTransform areaRect = area.GetComponent<RectTransform>();
        Stretch(areaRect);
        areaRect.offsetMin = new Vector2(18.0f, 8.0f);
        areaRect.offsetMax = new Vector2(-18.0f, -8.0f);
        area.AddComponent<RectMask2D>();
        TMP_Text placeholder = Text("Placeholder", "닉네임을 입력하세요", area.transform, Vector2.zero, Vector2.one, 25.0f, _style, new Color(0.25f, 0.25f, 0.25f, 0.55f));
        TMP_Text input = Text("Text", "", area.transform, Vector2.zero, Vector2.one, 25.0f, _style, Color.black);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        input.alignment = TextAlignmentOptions.MidlineLeft;
        field.textViewport = areaRect;
        field.textComponent = input;
        field.placeholder = placeholder;
        return field;
    }

    static Button Button(string _name, string _label, Transform _parent, Vector2 _anchor, Vector2 _size, TMP_Text _style, Button _source)
    {
        GameObject gameObject = UI(_name, _parent);
        SetRect(gameObject.GetComponent<RectTransform>(), _anchor, _size, Vector2.zero);
        Image image = gameObject.AddComponent<Image>();
        if (_source != null)
        {
            Image sourceImage = _source.GetComponent<Image>();
            if (sourceImage != null)
            {
                image.sprite = sourceImage.sprite;
                image.type = sourceImage.type;
                image.color = sourceImage.color;
            }
        }
        Button button = gameObject.AddComponent<Button>();
        if (_source != null)
        {
            button.transition = _source.transition;
            button.colors = _source.colors;
        }
        Text("Text", _label, gameObject.transform, Vector2.zero, Vector2.one, 27.0f, _style, Color.black);
        return button;
    }

    static RawImage Raw(string _name, Transform _parent, Vector2 _anchor, Vector2 _size)
    {
        GameObject gameObject = UI(_name, _parent);
        SetRect(gameObject.GetComponent<RectTransform>(), _anchor, _size, Vector2.zero);
        RawImage image = gameObject.AddComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text Text(string _name, string _value, Transform _parent, Vector2 _min, Vector2 _max, float _size, TMP_Text _style, Color _color)
    {
        GameObject gameObject = UI(_name, _parent);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = _min;
        rect.anchorMax = _max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TMP_Text text = gameObject.AddComponent<TextMeshProUGUI>();
        if (_style != null) text.font = _style.font;
        text.text = _value;
        text.fontSize = _size;
        text.color = _color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        return text;
    }

    static GameObject Background(string _name, Transform _parent, Vector2 _anchor, Vector2 _size, Color _color)
    {
        GameObject gameObject = UI(_name, _parent);
        SetRect(gameObject.GetComponent<RectTransform>(), _anchor, _size, Vector2.zero);
        Image image = gameObject.AddComponent<Image>();
        image.color = _color;
        return gameObject;
    }

    static GameObject UI(string _name, Transform _parent)
    {
        GameObject gameObject = new GameObject(_name, typeof(RectTransform), typeof(CanvasRenderer));
        gameObject.transform.SetParent(_parent, false);
        return gameObject;
    }

    static void SetRect(RectTransform _rect, Vector2 _anchor, Vector2 _size, Vector2 _position)
    {
        _rect.anchorMin = _anchor;
        _rect.anchorMax = _anchor;
        _rect.sizeDelta = _size;
        _rect.anchoredPosition = _position;
    }

    static void Stretch(RectTransform _rect)
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }

    static Transform Find(Scene _scene, string _name)
    {
        GameObject[] roots = _scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; ++i)
        {
            Transform found = Find(roots[i].transform, _name);
            if (found != null) return found;
        }
        throw new System.InvalidOperationException(_name + " object was not found.");
    }

    static Transform Find(Transform _root, string _name)
    {
        if (_root.name == _name) return _root;
        for (int i = 0; i < _root.childCount; ++i)
        {
            Transform found = Find(_root.GetChild(i), _name);
            if (found != null) return found;
        }
        return null;
    }

    static void DeleteChild(Transform _parent, string _name)
    {
        Transform child = _parent.Find(_name);
        if (child != null) Object.DestroyImmediate(child.gameObject);
    }
}
