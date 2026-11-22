using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyDirectoryUI : MonoBehaviour
{
	[SerializeField] PartyEntryUI[] m_rows;
	[SerializeField] TMP_Text m_detail;
	[SerializeField] TMP_Text m_message;
	[SerializeField] TMP_Text m_pageText;
	[SerializeField] Button m_previous;
	[SerializeField] Button m_next;
	[SerializeField] Button m_refresh;
	[SerializeField] Button m_join;
	[SerializeField] Button m_cancel;
	[SerializeField] Button m_create;
	[SerializeField] Button m_close;
	[SerializeField] PartyCreateUI m_createWindow;
	PartyUI m_owner;
	ulong m_selectedParty;
	ulong m_selectedCreateNum;

	void Awake()
	{
		m_close.onClick.AddListener(Close);
		m_previous.onClick.AddListener(Previous);
		m_next.onClick.AddListener(Next);
		m_refresh.onClick.AddListener(Refresh);
		m_join.onClick.AddListener(Join);
		m_cancel.onClick.AddListener(Cancel);
		m_create.onClick.AddListener(Create);
	}
	public void Open(PartyUI _owner) { m_selectedParty = 0; Bind(_owner); gameObject.SetActive(true); if (_owner.GetLobbyNetwork() != null) _owner.GetLobbyNetwork().Refresh(0); Bind(_owner); }
	public void Close() { gameObject.SetActive(false); if (m_createWindow != null) m_createWindow.Close(); }
	void Previous() { if (!m_previous.interactable) return; m_selectedParty = 0; m_owner.GetLobbyNetwork().Refresh(GetLobby().page - 1); Bind(m_owner); }
	void Next() { if (!m_next.interactable) return; m_selectedParty = 0; m_owner.GetLobbyNetwork().Refresh(GetLobby().page + 1); Bind(m_owner); }
	void Select(PartyListingInfo _party) { m_selectedParty = _party.partyNum; m_selectedCreateNum = _party.createNum; Bind(m_owner); }
	void Refresh() { if (!m_refresh.interactable) return; m_owner.GetLobbyNetwork().Refresh(GetLobby().page); Bind(m_owner); }
	void Join()
	{
		Bind(m_owner);
		if (!m_join.interactable) return;
		m_owner.GetLobbyNetwork().Join(m_selectedParty);
		Bind(m_owner);
	}
	void Cancel() { if (!m_cancel.interactable) return; m_owner.GetLobbyNetwork().Cancel(GetLobby().requestedParty); Bind(m_owner); }
	void Create() { m_createWindow.Open(m_owner); }
	PartyLobbyInfo GetLobby() { if (m_owner == null || m_owner.GetInfo() == null) return null; return m_owner.GetInfo().lobby; }
	public void Bind(PartyUI _owner)
	{
		m_owner = _owner;
		PartyLobbyInfo lobby = GetLobby();
		bool connected = _owner != null && _owner.GetInfo() != null && _owner.GetNetwork() != null && _owner.GetNetwork().CanRequest() && !LoadingPanelUI.IsVisible();
		bool ready = connected && _owner.GetInfo().partyNum == 0 && !_owner.GetInfo().IsPending() && _owner.GetLobbyNetwork() != null && !lobby.listPending;
		m_refresh.interactable = ready;
		m_create.interactable = connected && !_owner.GetInfo().IsPending() && _owner.GetLobbyNetwork() != null;
		m_join.interactable = false;
		m_cancel.gameObject.SetActive(lobby != null && lobby.requestedParty != 0);
		m_cancel.interactable = ready;
		m_message.text = "서버에 연결되면 파티를 확인할 수 있습니다.";
		m_detail.text = "파티를 선택하면\n파티원 정보를 볼 수 있습니다.";
		int count = 0;
		if (lobby != null) count = lobby.GetParties().Count;
		ulong page = 0;
		ulong pages = 1;
		if (lobby != null)
		{
			page = lobby.page;
			if (lobby.totalCount > 0) pages = (lobby.totalCount - 1) / 5 + 1;
		}
		m_pageText.text = (page + 1) + " / " + pages;
		m_previous.interactable = page > 0 && ready;
		m_next.interactable = page + 1 < pages && ready;
		PartyListingInfo selected = null;
		if (lobby != null)
		{
			m_message.text = lobby.message;
			if (count == 0 && !lobby.listPending && string.IsNullOrEmpty(m_message.text)) m_message.text = "아직 개설된 파티가 없습니다.";
			if (lobby.requestedParty != 0 && !_owner.GetInfo().IsPending()) m_message.text = "가입 신청 중입니다. 파티장의 응답을 기다려주세요.";
		}
		for (int i = 0; i < m_rows.Length; ++i)
		{
			PartyListingInfo entry = null;
			if (i < count) entry = lobby.GetParties()[i];
			bool isSelected = entry != null && entry.partyNum == m_selectedParty && entry.createNum == m_selectedCreateNum;
			if (isSelected) selected = entry;
			m_rows[i].Bind(entry, isSelected, Select);
		}
		if (selected == null) { m_selectedParty = 0; return; }
		StringBuilder detail = new StringBuilder(selected.name + "\n\n");
		for (int i = 0; i < selected.members.Count; ++i)
		{
			PartyMemberInfo member = selected.members[i];
			string job = "전사";
			if (member.jobClass == 1) job = "궁수";
			detail.Append(member.name).Append("\nLv.").Append(member.level).Append("  ·  ").Append(job);
			if (member.state == ePartyMemberState.Offline) detail.Append(" · 비활성");
			if (i + 1 < selected.members.Count) detail.Append("\n\n");
		}
		m_detail.text = detail.ToString();
		m_join.interactable = ready && lobby.requestedParty == 0 && selected.members.Count < PartyInfo.MaxMembers;
	}
}
