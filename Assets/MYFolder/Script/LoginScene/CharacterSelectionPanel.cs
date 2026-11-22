using UnityEngine;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class CharacterSelectionPanel : MonoBehaviour
{
	const int SlotCount = 2;

	[SerializeField]
	CharacterSlotUI[] m_slots = new CharacterSlotUI[SlotCount];
	[SerializeField]
	TMP_Text m_statusText;
	[SerializeField] CharacterCreatePanelUI m_createPanel;
	Action<ulong, string> m_createCharacterAction;
	Action<ulong> m_selectCharacterAction;
	List<CharacterListInfo> m_characterList = new List<CharacterListInfo>();
	GameObject m_warriorPrefab;
	GameObject m_archerPrefab;
	GameObject m_previewStage;
	[SerializeField] CharacterPreview m_previewPrefab;
	CharacterPreview[] m_previews = new CharacterPreview[SlotCount];

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
		m_createPanel.Initialize(RequestCreate);
		for (int i = 0; i < SlotCount; ++i)
		{
			m_slots[i].Initialize();
		}
		ShowWaiting();
	}
	public void ShowWaiting()
	{
		m_statusText.text = "캐릭터 목록을 불러오는 중입니다.";
		m_createPanel.Close();

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
			if (_serverCharacterCount > SlotCount) m_statusText.text = "표시 가능한 캐릭터 슬롯은 2개입니다.";
			else m_statusText.text = "접속할 캐릭터를 선택하세요.";
		}
	}
	public void AddCharacter(CharacterListInfo _character)
	{
		if (m_characterList.Count >= SlotCount) return;

		m_characterList.Add(_character);
		SetCharacters(m_characterList, m_characterList.Count);
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
		GameObject prefab = m_warriorPrefab;
		if (_jobClass != 0) prefab = m_archerPrefab;
		if (prefab == null || m_previewPrefab == null) return null;
		CharacterPreview preview = Instantiate(m_previewPrefab, m_previewStage.transform);
		preview.name = "Character_Slot_" + (_slotIndex + 1) + "_Preview";
		preview.transform.position = new Vector3(-25.0f + 50.0f * _slotIndex, -2000.0f, 0.0f);
		m_previews[_slotIndex] = preview;
		return preview.Initialize(prefab, _jobClass, _slotIndex);
	}
	void ClearSlotPreview(int _slotIndex)
	{
		if (m_previews[_slotIndex] == null) return;
		m_previews[_slotIndex].gameObject.SetActive(false);
		Destroy(m_previews[_slotIndex].gameObject);
		m_previews[_slotIndex] = null;
	}
	void OpenCreateDialog(int _slotIndex) { m_createPanel.Show(); }
	void RequestCreate(ulong _job, string _name)
	{
		m_createCharacterAction?.Invoke(_job, _name);
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
			string jobName = "전사";
			if (_character.jobClass != 0) jobName = "궁수";
			m_infoText.text = jobName + "  |  Lv. " + _character.level;
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
