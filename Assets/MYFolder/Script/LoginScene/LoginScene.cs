using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.UI;

public class LoginScene : MonoBehaviour
{
    [SerializeField] GameObject m_loginPanel;
    [SerializeField] GameObject m_characterPanel;
    [SerializeField] GameObject m_warriorPrefab;
    [SerializeField] GameObject m_archerPrefab;
    [SerializeField] NoticePanelUI m_noticePanel;
    [SerializeField] CharacterSelectionPanel m_characterSelectionPanel;
    [SerializeField] CharacterShowcase m_characterShowcase;
    [SerializeField] LoginSceneNetwork m_network;

    void Start()
    {
        m_characterShowcase.Initialize(m_warriorPrefab, m_archerPrefab);
        m_noticePanel.Initialize();
        m_characterSelectionPanel.Initialize(m_warriorPrefab, m_archerPrefab, m_network.RequestCreateCharacter, m_network.RequestSelectCharacter);
        m_loginPanel.SetActive(true);
        m_characterPanel.SetActive(false);

        m_network.Initialize(this);
    }

    // 씬은 화면 변경만 하고, 패킷 처리는 LoginSceneNetwork에서 함
    public void ShowLoginResult(bool _result)
    {
        if (!_result)
        {
            ShowNotice("ID 및 PW를 확인하세요");
            return;
        }

        m_loginPanel.SetActive(false);
        m_characterPanel.SetActive(true);
        m_characterSelectionPanel.ShowWaiting();
    }
    public void ShowAccountResult(bool _result)
    {
        if (_result) ShowNotice("회원가입이 완료되었습니다.");
        else ShowNotice("회원가입에 실패했습니다. 가입 정보를 확인하세요.");
    }
    public void ShowCharacters(List<CharacterListInfo> _characters, int _count)
    {
        m_characterSelectionPanel.SetCharacters(_characters, _count);
    }
    public void ShowCreateResult(CharacterListInfo _character)
    {
        if (_character == null)
        {
            m_characterSelectionPanel.ShowCreateFailed();
            ShowNotice("캐릭터 생성에 실패했습니다.");
            return;
        }

        m_characterSelectionPanel.AddCharacter(_character);
        ShowNotice("캐릭터가 생성되었습니다.");
    }
    public void ShowNotice(string _message)
    {
        m_noticePanel.Show(_message, "확인", null);
    }
    public void ShowDisconnected()
    {
        m_loginPanel.SetActive(true);
        m_characterPanel.SetActive(false);
        m_noticePanel.Show("서버와 연결이 되지않았습니다.", "확인", m_network.QuitClient);
    }
}

public class CharacterListInfo
{
    public readonly ulong characterIndex;
    public readonly string name;
    public readonly ulong jobClass;
    public readonly ulong level;

    public CharacterListInfo(ulong _characterIndex, string _name, ulong _jobClass, ulong _level)
    {
        characterIndex = _characterIndex;
        name = _name;
        jobClass = _jobClass;
        level = _level;
    }
}

public class CharacterSelectionPanelBase : MonoBehaviour
{
    const int SlotCount = 2;

    [SerializeField]
    CharacterSlotUI[] m_slots = new CharacterSlotUI[SlotCount];
    [SerializeField]
    TMP_Text m_statusText;
    [SerializeField]
    GameObject m_createDialog;
    [SerializeField]
    TMP_InputField m_nicknameField;
    [SerializeField]
    Button m_warriorButton;
    [SerializeField]
    Button m_archerButton;
    [SerializeField]
    TMP_Text m_warriorButtonText;
    [SerializeField]
    TMP_Text m_archerButtonText;
    [SerializeField]
    TMP_Text m_validationText;
    [SerializeField]
    Button m_confirmButton;
    [SerializeField]
    Button m_cancelButton;
    int m_selectedJob = -1;
    Action<ulong, string> m_createCharacterAction;
	Action<ulong> m_selectCharacterAction;
    List<CharacterListInfo> m_characterList = new List<CharacterListInfo>();
    GameObject m_warriorPrefab;
    GameObject m_archerPrefab;
    GameObject m_previewStage;
    GameObject[] m_slotPreviewRoots = new GameObject[SlotCount];
    RenderTexture[] m_slotPreviewTextures = new RenderTexture[SlotCount];

    public void Initialize(
        GameObject _warriorPrefab,
        GameObject _archerPrefab,
		Action<ulong, string> _createCharacterAction,
		Action<ulong> _selectCharacterAction)
    {
        m_warriorPrefab = _warriorPrefab;
        m_archerPrefab = _archerPrefab;
        m_createCharacterAction = _createCharacterAction;
		m_selectCharacterAction = _selectCharacterAction;
        m_previewStage = new GameObject("CharacterSlotPreviewStage");
        m_warriorButton.onClick.RemoveAllListeners();
        m_archerButton.onClick.RemoveAllListeners();
        m_confirmButton.onClick.RemoveAllListeners();
        m_cancelButton.onClick.RemoveAllListeners();
        m_nicknameField.onValueChanged.RemoveAllListeners();
        m_warriorButton.onClick.AddListener(() => SelectJob(0));
        m_archerButton.onClick.AddListener(() => SelectJob(1));
        m_confirmButton.onClick.AddListener(ConfirmCreate);
        m_cancelButton.onClick.AddListener(CloseCreateDialog);
        m_nicknameField.onValueChanged.AddListener(OnNicknameChanged);
        for (int i = 0; i < SlotCount; ++i)
        {
            m_slots[i].Initialize();
        }
        ShowWaiting();
    }
    public void ShowWaiting()
    {
        m_statusText.text = "캐릭터 목록을 불러오는 중입니다.";
        m_createDialog.SetActive(false);

        for (int i = 0; i < SlotCount; ++i)
        {
            ClearSlotPreview(i);
            m_slots[i].SetWaiting();
        }
    }
    public void SetCharacters(List<CharacterListInfo> _characterList, int _serverCharacterCount)
    {
        m_characterList = new List<CharacterListInfo>(_characterList);

        for (int i = 0; i < SlotCount; ++i)
        {
            ClearSlotPreview(i);

            if (i < _characterList.Count)
            {
                CharacterListInfo character = _characterList[i];
                RenderTexture previewTexture = CreateSlotPreview(i, character.jobClass);
                m_slots[i].SetOccupied(i, character, previewTexture, OnConnectClicked);
            }
            else
            {
                m_slots[i].SetEmpty(i, OpenCreateDialog);
            }
        }

        if (_serverCharacterCount == 0)
        {
            m_statusText.text = "생성된 캐릭터가 없습니다.";
        }
        else
        {
            m_statusText.text = _serverCharacterCount > SlotCount
                ? "표시 가능한 캐릭터 슬롯은 2개입니다."
                : "접속할 캐릭터를 선택하세요.";
        }
    }
    public void AddCharacter(CharacterListInfo _character)
    {
        if (m_characterList.Count >= SlotCount) return;

        List<CharacterListInfo> characterList = new List<CharacterListInfo>(m_characterList);
        characterList.Add(_character);
        SetCharacters(characterList, characterList.Count);
    }
    public void ShowCreateFailed()
    {
        if (m_characterList.Count == 0)
        {
            m_statusText.text = "생성된 캐릭터가 없습니다.";
        }
        else
        {
            m_statusText.text = "접속할 캐릭터를 선택하세요.";
        }
    }
    void OnEnable()
    {
        if (m_previewStage != null) m_previewStage.SetActive(true);
    }
    void OnDisable()
    {
        if (m_previewStage != null) m_previewStage.SetActive(false);
    }
    void OnDestroy()
    {
        for (int i = 0; i < SlotCount; ++i)
        {
            ClearSlotPreview(i);
        }

        if (m_previewStage != null) Destroy(m_previewStage);
    }
    RenderTexture CreateSlotPreview(int _slotIndex, ulong _jobClass)
    {
        GameObject prefab = _jobClass == 0 ? m_warriorPrefab : m_archerPrefab;
        string idleState = _jobClass == 0 ? "Idle_SwordShield" : "Idle_Bow";
        if (prefab == null)
        {
            Debug.LogError("캐릭터 슬롯 프리팹이 연결되지 않았습니다. 직업: " + _jobClass);
            return null;
        }

        Vector3 previewPosition = new Vector3(-25.0f + (50.0f * _slotIndex), -2000.0f, 0.0f);
        GameObject previewRoot = new GameObject("Character_Slot_" + (_slotIndex + 1) + "_Preview");
        previewRoot.transform.SetParent(m_previewStage.transform, false);
        previewRoot.transform.position = previewPosition;
        m_slotPreviewRoots[_slotIndex] = previewRoot;

        GameObject character = Instantiate(prefab, previewRoot.transform);
        character.name = prefab.name;
        character.transform.localPosition = Vector3.zero;
        character.transform.localRotation = Quaternion.identity;

        Animator animator = character.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            PlayIdleOnly(animator, idleState);
        }

        Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogError("캐릭터 프리팹에서 Renderer를 찾지 못했습니다: " + prefab.name);
            Destroy(previewRoot);
            m_slotPreviewRoots[_slotIndex] = null;
            return null;
        }

        int activeRendererCount = 0;
        for (int i = 0; i < renderers.Length; ++i)
        {
            if (!renderers[i].gameObject.activeInHierarchy) continue;

            renderers[i].enabled = true;
            ++activeRendererCount;

            SkinnedMeshRenderer skinnedMeshRenderer = renderers[i] as SkinnedMeshRenderer;
            if (skinnedMeshRenderer != null) skinnedMeshRenderer.updateWhenOffscreen = true;
        }

        Debug.Log("캐릭터 슬롯 프리뷰 생성: " + prefab.name
            + ", 전체 Renderer: " + renderers.Length
            + ", 활성 Renderer: " + activeRendererCount);

        RenderTexture texture = new RenderTexture(512, 768, 24, RenderTextureFormat.ARGB32);
        texture.name = "Character_Slot_" + (_slotIndex + 1) + "_Texture";
        texture.antiAliasing = 4;
        texture.Create();
        m_slotPreviewTextures[_slotIndex] = texture;

        GameObject cameraObject = new GameObject("Preview_Camera", typeof(Camera));
        cameraObject.transform.SetParent(previewRoot.transform, false);
        cameraObject.transform.localPosition = new Vector3(0.0f, 1.15f, 4.3f);
        cameraObject.transform.LookAt(previewPosition + new Vector3(0.0f, 1.05f, 0.0f));

        Camera previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        previewCamera.fieldOfView = 30.0f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 10.0f;
        previewCamera.targetTexture = texture;
        previewCamera.enabled = true;

        GameObject lightObject = new GameObject("Preview_Light", typeof(Light));
        lightObject.transform.SetParent(previewRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(-2.0f, 3.0f, 3.0f);
        lightObject.transform.LookAt(previewPosition + Vector3.up);

        Light previewLight = lightObject.GetComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;
        previewLight.shadows = LightShadows.Soft;
        previewCamera.Render();
        return texture;
    }
    static void PlayIdleOnly(Animator _animator, string _idleState)
    {
        RuntimeAnimatorController controller = _animator.runtimeAnimatorController;
        if (controller == null) return;

        AnimationClip idleClip = null;
        AnimationClip[] clips = controller.animationClips;
        for (int i = 0; i < clips.Length; ++i)
        {
            if (clips[i].name != _idleState) continue;

            idleClip = clips[i];
            break;
        }

        _animator.Play(_idleState, 0, 0.0f);
        _animator.Update(0.0f);

        if (idleClip == null)
        {
            AnimatorClipInfo[] currentClips = _animator.GetCurrentAnimatorClipInfo(0);
            if (currentClips.Length > 0) idleClip = currentClips[0].clip;
        }

        if (idleClip == null)
        {
            Debug.LogError("Idle 애니메이션 클립을 찾지 못했습니다: " + _idleState);
            return;
        }

        AnimatorOverrideController idleController = new AnimatorOverrideController(controller);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        idleController.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; ++i)
        {
            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, idleClip);
        }

        idleController.ApplyOverrides(overrides);
        _animator.runtimeAnimatorController = idleController;
        _animator.Play(_idleState, 0, 0.0f);
        _animator.Update(0.0f);
    }
    void ClearSlotPreview(int _slotIndex)
    {
        if (m_slotPreviewRoots[_slotIndex] != null)
        {
            Destroy(m_slotPreviewRoots[_slotIndex]);
            m_slotPreviewRoots[_slotIndex] = null;
        }

        if (m_slotPreviewTextures[_slotIndex] != null)
        {
            m_slotPreviewTextures[_slotIndex].Release();
            Destroy(m_slotPreviewTextures[_slotIndex]);
            m_slotPreviewTextures[_slotIndex] = null;
        }
    }
    void OpenCreateDialog(int _slotIndex)
    {
        m_selectedJob = -1;
        m_nicknameField.text = string.Empty;
        m_validationText.text = string.Empty;
        UpdateJobSelection();
        UpdateCreateButtonState();
        m_createDialog.SetActive(true);
        m_createDialog.transform.SetAsLastSibling();
        m_nicknameField.Select();
    }
    void CloseCreateDialog()
    {
        m_createDialog.SetActive(false);
    }
    void SelectJob(int _job)
    {
        m_selectedJob = _job;
        m_validationText.text = string.Empty;
        UpdateJobSelection();
        UpdateCreateButtonState();
    }
    void UpdateJobSelection()
    {
        m_warriorButton.image.color = m_selectedJob == 0 ? new Color(0.55f, 0.75f, 0.95f, 1.0f) : Color.white;
        m_archerButton.image.color = m_selectedJob == 1 ? new Color(0.55f, 0.75f, 0.95f, 1.0f) : Color.white;
        m_warriorButtonText.text = m_selectedJob == 0 ? "✓  전사" : "전사";
        m_archerButtonText.text = m_selectedJob == 1 ? "✓  궁수" : "궁수";
        m_warriorButtonText.color = Color.black;
        m_archerButtonText.color = Color.black;
    }
    void OnNicknameChanged(string _nickname)
    {
        m_validationText.text = string.Empty;
        UpdateCreateButtonState();
    }
    void UpdateCreateButtonState()
    {
        string nickname = m_nicknameField.text.Trim();
        m_confirmButton.interactable = m_selectedJob >= 0
            && !string.IsNullOrEmpty(nickname)
            && Encoding.UTF8.GetByteCount(nickname) <= 30;
    }
    void ConfirmCreate()
    {
        string nickname = m_nicknameField.text.Trim();
        if (m_selectedJob < 0)
        {
            m_validationText.text = "전사 또는 궁수를 선택하세요.";
            return;
        }
        if (string.IsNullOrEmpty(nickname))
        {
            m_validationText.text = "닉네임을 입력하세요.";
            m_nicknameField.Select();
            return;
        }

        if (Encoding.UTF8.GetByteCount(nickname) > 30)
        {
            m_validationText.text = "닉네임은 UTF-8 기준 30바이트 이하여야 합니다.";
            m_nicknameField.Select();
            return;
        }

        string jobName = m_selectedJob == 0 ? "전사" : "궁수";
        Debug.Log("캐릭터 생성 요청: " + jobName + ", " + nickname);
        m_createCharacterAction?.Invoke((ulong)m_selectedJob, nickname);
        CloseCreateDialog();
        m_statusText.text = "캐릭터를 생성하는 중입니다.";
    }
    void OnConnectClicked(int _slotIndex)
    {
		if (_slotIndex < 0 || _slotIndex >= m_characterList.Count) return;
		m_statusText.text = "Zone에 입장하는 중입니다.";
		m_selectCharacterAction?.Invoke(m_characterList[_slotIndex].characterIndex);
    }
    [Serializable]
    class CharacterSlotUI
    {
        [SerializeField]
        TMP_Text m_nameText;
        [SerializeField]
        TMP_Text m_infoText;
        [SerializeField]
        RawImage m_previewImage;
        [SerializeField]
        Button m_connectButton;
        [SerializeField]
        TMP_Text m_buttonText;
        int m_slotIndex;
        Action<int> m_connectAction;

        public void Initialize()
        {
            m_connectButton.onClick.RemoveAllListeners();
            m_connectButton.onClick.AddListener(OnConnect);
        }
        public void SetWaiting()
        {
            m_nameText.text = string.Empty;
            m_infoText.text = string.Empty;
            m_previewImage.texture = null;
            m_previewImage.gameObject.SetActive(false);
            m_connectButton.gameObject.SetActive(false);
            m_connectAction = null;
        }
        public void SetEmpty(int _slotIndex, Action<int> _createAction)
        {
            m_slotIndex = _slotIndex;
            m_nameText.text = "빈 슬롯";
            m_infoText.text = "캐릭터 없음";
            m_previewImage.texture = null;
            m_previewImage.gameObject.SetActive(false);
            m_buttonText.text = "생성";
            m_connectButton.gameObject.SetActive(true);
            m_connectAction = _createAction;
        }
        public void SetOccupied(int _slotIndex, CharacterListInfo _character, RenderTexture _previewTexture, Action<int> _connectAction)
        {
            m_slotIndex = _slotIndex;
            m_connectAction = _connectAction;
            m_nameText.text = _character.name;
            m_infoText.text = (_character.jobClass == 0 ? "전사" : "궁수") + "  |  Lv. " + _character.level;
            m_previewImage.texture = _previewTexture;
            m_previewImage.gameObject.SetActive(_previewTexture != null);
            m_buttonText.text = "접속";
            m_connectButton.gameObject.SetActive(true);
        }
        void OnConnect()
        {
            m_connectAction?.Invoke(m_slotIndex);
        }
    }
}

public class CharacterShowcaseBase : MonoBehaviour
{
    const int PreviewWidth = 512;
    const int PreviewHeight = 768;

    GameObject m_stage;
    RenderTexture m_warriorTexture;
    RenderTexture m_archerTexture;
    [SerializeField]
    RawImage m_warriorImage;
    [SerializeField]
    RawImage m_archerImage;

    public void Initialize(GameObject _warriorPrefab, GameObject _archerPrefab)
    {
        if (_warriorPrefab == null || _archerPrefab == null)
        {
            Debug.LogError("전사 또는 궁수 프리팹이 LoginScene에 연결되지 않았습니다.");
            return;
        }

        m_stage = new GameObject("LoginCharacterPreviewStage");

        m_warriorTexture = CreatePreviewTexture("Warrior_Preview_Texture");
        m_archerTexture = CreatePreviewTexture("Archer_Preview_Texture");

        CreatePreview(
            "Warrior_Preview",
            _warriorPrefab,
            new Vector3(-20.0f, -1000.0f, 0.0f),
            m_warriorTexture,
            "Idle_SwordShield",
            m_warriorImage);

        CreatePreview(
            "Archer_Preview",
            _archerPrefab,
            new Vector3(20.0f, -1000.0f, 0.0f),
            m_archerTexture,
            "Idle_Bow",
            m_archerImage);
    }
    void OnEnable()
    {
        if (m_stage != null) m_stage.SetActive(true);
    }
    void OnDisable()
    {
        if (m_stage != null) m_stage.SetActive(false);
    }
    void OnDestroy()
    {
        if (m_stage != null) Destroy(m_stage);

        ReleaseTexture(m_warriorTexture);
        ReleaseTexture(m_archerTexture);
    }
    void CreatePreview(
        string _name,
        GameObject _prefab,
        Vector3 _position,
        RenderTexture _texture,
        string _idleState,
        RawImage _previewImage)
    {
        GameObject previewRoot = new GameObject(_name);
        previewRoot.transform.SetParent(m_stage.transform, false);
        previewRoot.transform.position = _position;

        GameObject character = Instantiate(_prefab, previewRoot.transform);
        character.name = _prefab.name;
        character.transform.localPosition = Vector3.zero;
        character.transform.localRotation = Quaternion.identity;

        Animator animator = character.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.applyRootMotion = false;
            animator.Play(_idleState, 0, 0.0f);
            animator.Update(0.0f);
        }

        GameObject cameraObject = new GameObject("Preview_Camera", typeof(Camera));
        cameraObject.transform.SetParent(previewRoot.transform, false);
        cameraObject.transform.localPosition = new Vector3(0.0f, 1.15f, 4.3f);
        cameraObject.transform.LookAt(previewRoot.transform.position + new Vector3(0.0f, 1.05f, 0.0f));

        Camera previewCamera = cameraObject.GetComponent<Camera>();
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        previewCamera.fieldOfView = 30.0f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 10.0f;
        previewCamera.targetTexture = _texture;

        GameObject lightObject = new GameObject("Preview_Light", typeof(Light));
        lightObject.transform.SetParent(previewRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(-2.0f, 3.0f, 3.0f);
        lightObject.transform.LookAt(previewRoot.transform.position + Vector3.up);

        Light previewLight = lightObject.GetComponent<Light>();
        previewLight.type = LightType.Directional;
        previewLight.intensity = 1.2f;
        previewLight.shadows = LightShadows.Soft;

        _previewImage.texture = _texture;
    }
    static RenderTexture CreatePreviewTexture(string _name)
    {
        RenderTexture texture = new RenderTexture(PreviewWidth, PreviewHeight, 24, RenderTextureFormat.ARGB32);
        texture.name = _name;
        texture.antiAliasing = 4;
        texture.Create();
        return texture;
    }
    static void ReleaseTexture(RenderTexture _texture)
    {
        if (_texture == null) return;

        _texture.Release();
        Destroy(_texture);
    }
}

