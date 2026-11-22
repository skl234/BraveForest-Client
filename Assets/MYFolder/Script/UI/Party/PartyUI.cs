using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PartyUI : MonoBehaviour
{
	[SerializeField] PartyMemberUI[] m_memberList;
	[SerializeField] RectTransform m_panel;
	[SerializeField] TMP_Text m_title;
	[SerializeField] TMP_Text m_memberCount;
	[SerializeField] Button m_openButton;
	[SerializeField] PartyWindowUI m_window;
	[SerializeField] PartyInviteUI m_invite;
	[SerializeField] ConfirmPanelUI m_confirm;
	[SerializeField] PartyDirectoryUI m_directory;
	[SerializeField] PartyNotificationUI m_notification;
	NetClient m_client;
	AccountInfo m_account;
	PartyInfo m_info;
	IPartyUIActions m_actions;
	IPartyLobbyRequests m_lobbyActions;
	ulong m_characterIndex;
	uint m_revision = uint.MaxValue;
	uint m_lobbyRevision = uint.MaxValue;
	bool m_ready;

	public PartyInfo GetInfo() { return m_info; }
	public IPartyUIActions GetNetwork() { return m_actions; }
	public IPartyLobbyRequests GetLobbyNetwork() { return m_lobbyActions; }
	public ConfirmPanelUI GetConfirm() { return m_confirm; }
	public ulong GetCharacterIndex() { return m_characterIndex; }
	public void Bind(PartyInfo _info, ulong _characterIndex, IPartyUIActions _actions, IPartyLobbyRequests _lobbyActions)
	{
		m_info = _info;
		m_characterIndex = _characterIndex;
		m_actions = _actions;
		m_lobbyActions = _lobbyActions;
		m_revision = uint.MaxValue;
	}
	public bool IsLeader()
	{
		PartyInfo info = GetInfo();
		if (info == null || info.partyNum == 0) return false;
		PartyMemberInfo self = info.FindCharacter(GetCharacterIndex());
		return self != null && self.characterIndex == info.leaderCharacterIndex;
	}

	void Awake() { m_openButton.onClick.AddListener(Toggle); }
	void OnEnable() { m_revision = uint.MaxValue; }
	void OnDisable()
	{
		m_window.gameObject.SetActive(false);
		m_invite.gameObject.SetActive(false);
		m_confirm.Close();
		if (m_directory != null) m_directory.Close();
		if (m_notification != null) m_notification.Close();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.P) && Input.compositionString.Length == 0)
		{
			GameObject selected = null;
			if (EventSystem.current != null) selected = EventSystem.current.currentSelectedGameObject;
			TMP_InputField input = null;
			if (selected != null) input = selected.GetComponent<TMP_InputField>();
			if (input == null || !input.isFocused) Toggle();
		}
	}

	void LateUpdate()
	{
		if (m_client == null) m_client = FindObjectOfType<NetClient>();
		if (m_account == null) m_account = FindObjectOfType<AccountInfo>();
		if (m_client != null)
		{
			m_info = m_client.GetPartyInfo();
			m_actions = m_client.GetPartyNetwork();
			m_lobbyActions = m_client.GetPartyNetwork().GetLobbyNetwork();
			if (m_account != null) m_characterIndex = m_account.GetCharacterIndex();
		}
		PartyInfo info = GetInfo();
		if (info == null) { m_panel.gameObject.SetActive(false); return; }
		bool ready = GetNetwork() != null && GetNetwork().CanRequest();
		m_invite.Bind(this);
		if (m_revision == info.revision && m_lobbyRevision == info.lobby.revision && m_ready == ready)
		{
			if (m_notification != null) m_notification.Bind(this);
			return;
		}
		m_revision = info.revision;
		m_lobbyRevision = info.lobby.revision;
		m_ready = ready;
		m_openButton.interactable = ready;
		m_title.text = info.partyName;
		m_memberCount.text = info.GetMembers().Count + " / " + PartyInfo.MaxMembers;
		m_panel.gameObject.SetActive(info.partyNum != 0 && info.GetMembers().Count > 0);
		float height = 38;
		if (info.GetMembers().Count > 0) height = 38 + info.GetMembers().Count * 55;
		m_panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		if (m_notification != null) m_notification.Bind(this);
		BindRows(m_memberList, info, GetCharacterIndex(), 0, 0, null);
		m_window.Bind(this);
		if (m_directory != null) m_directory.Bind(this);
		if (info.partyNum == 0 && m_window.gameObject.activeSelf)
		{
			m_window.Close();
			if (m_directory != null) m_directory.Open(this);
		}
		else if (info.partyNum != 0 && m_directory != null && m_directory.gameObject.activeSelf)
		{
			m_directory.Close();
			m_window.gameObject.SetActive(true);
		}
	}

	public void Toggle()
	{
		if (m_confirm.gameObject.activeSelf) return;
		if (m_window.gameObject.activeSelf) { m_window.Close(); return; }
		if (m_directory != null && m_directory.gameObject.activeSelf) { m_directory.Close(); return; }
		if (UIInputBlocker.IsBlocked()) return;
		ZoneMapUI map = FindObjectOfType<ZoneMapUI>();
		if (map != null) map.Collapse();
		if (GetInfo() == null || GetInfo().partyNum == 0)
		{
			if (m_directory != null) m_directory.Open(this);
			return;
		}
		m_window.Bind(this);
		m_window.gameObject.SetActive(true);
	}

	public static void BindRows(PartyMemberUI[] _rows, PartyInfo _info, ulong _selfCharacter, ulong _selectedSession, ulong _selectedCharacter, System.Action<PartyMemberInfo> _select, float _spacing = 4)
	{
		// 기존 행을 같은 캐릭터에 다시 연결. 자신만 맨 위로 배치
		bool[] used = new bool[_rows.Length];
		int[] matchingRows = new int[_info.GetMembers().Count];
		for (int i = 0; i < matchingRows.Length; ++i)
		{
			matchingRows[i] = -1;
			PartyMemberInfo member = _info.GetMembers()[i];
			for (int j = 0; j < _rows.Length; ++j)
			{
				PartyMemberInfo previous = _rows[j].GetMember();
				if (previous == null || previous.characterIndex != member.characterIndex) continue;
				matchingRows[i] = j;
				used[j] = true;
				break;
			}
		}
		int order = 0;
		for (int pass = 0; pass < 2; ++pass)
		{
			for (int i = 0; i < _info.GetMembers().Count; ++i)
			{
				PartyMemberInfo member = _info.GetMembers()[i];
				bool self = member.characterIndex == _selfCharacter;
				if ((pass == 0 && !self) || (pass == 1 && self)) continue;
				int row = matchingRows[i];
				if (row < 0)
				{
					for (int j = 0; j < _rows.Length; ++j)
					{
						if (!used[j]) { row = j; break; }
					}
				}
				if (row < 0) break;
				used[row] = true;
				_rows[row].Bind(member, member.characterIndex == _info.leaderCharacterIndex, member.sessionId == _selectedSession && member.characterIndex == _selectedCharacter, _select);
				RectTransform rect = (RectTransform)_rows[row].transform;
				rect.anchoredPosition = new Vector2(0, -order * (rect.sizeDelta.y + _spacing));
				++order;
			}
		}
		for (int i = 0; i < _rows.Length; ++i)
		{
			if (!used[i]) _rows[i].Bind(null, false, false);
		}
	}
}
