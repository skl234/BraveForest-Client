using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyApplicationUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_message;
	[SerializeField] TMP_Text m_result;
	[SerializeField] TMP_Text m_page;
	[SerializeField] Button m_previous;
	[SerializeField] Button m_next;
	[SerializeField] Button m_accept;
	[SerializeField] Button m_decline;
	[SerializeField] Button m_close;
	PartyUI m_owner;
	ulong m_selected;
	int m_index;
	void Awake()
	{
		m_previous.onClick.AddListener(Previous); m_next.onClick.AddListener(Next);
		m_accept.onClick.AddListener(Accept); m_decline.onClick.AddListener(Decline); m_close.onClick.AddListener(Close);
	}
	public void Open(PartyUI _owner, ulong _requestNum = 0)
	{
		m_owner = _owner;
		m_selected = _requestNum;
		m_index = 0;
		gameObject.SetActive(true);
		Refresh();
	}
	public void Close() { gameObject.SetActive(false); }
	void Previous() { if (!m_previous.interactable) return; --m_index; m_selected = 0; Refresh(); }
	void Next()
	{
		if (!m_next.interactable) return;
		if (m_selected != 0 && m_owner.GetInfo().lobby.FindApplication(m_selected) == null) m_index = 0;
		else ++m_index;
		m_selected = 0;
		Refresh();
	}
	void Accept() { Answer(true); }
	void Decline() { Answer(false); }
	void Answer(bool _accept)
	{
		if (!CanRequest() || m_selected == 0) return;
		PartyInfo info = m_owner.GetInfo();
		if (info.lobby.FindApplication(m_selected) == null) { Refresh(); return; }
		if (_accept && info.GetMembers().Count >= PartyInfo.MaxMembers) return;
		if (!m_owner.GetLobbyNetwork().Answer(m_selected, _accept)) info.lobby.Complete("신청을 처리하지 못했습니다. 잠시 후 다시 시도해주세요.", m_selected);
		Refresh();
	}
	bool CanRequest()
	{
		if (m_owner == null || !m_owner.IsLeader() || LoadingPanelUI.IsVisible()) return false;
		PartyInfo info = m_owner.GetInfo();
		return m_owner.GetNetwork() != null && m_owner.GetNetwork().CanRequest() && !info.IsPending() && m_owner.GetLobbyNetwork() != null;
	}
	void Update() { Refresh(); }
	void Refresh()
	{
		if (m_owner == null || !m_owner.IsLeader()) { Close(); return; }
		PartyInfo info = m_owner.GetInfo();
		PartyLobbyInfo lobby = info.lobby;
		bool pending = info.IsPending();
		var list = lobby.GetApplications();
		bool missing = m_selected != 0 && lobby.FindApplication(m_selected) == null;
		for (int i = 0; i < list.Count; ++i) if (list[i].requestNum == m_selected) m_index = i;
		m_index = Mathf.Clamp(m_index, 0, Mathf.Max(0, list.Count - 1));
		if (!missing && list.Count > 0) m_selected = list[m_index].requestNum;
		m_previous.interactable = m_index > 0 && !pending && !missing;
		m_next.interactable = m_index + 1 < list.Count && !pending;
		if (missing) m_next.interactable = list.Count > 0 && !pending;
		m_page.text = "0 / 0";
		m_message.text = "대기 중인 가입 신청이 없습니다.";
		// 다른 신청의 처리 결과가 새 신청에 남지 않게 함.
		m_result.text = "";
		if (m_selected != 0 && lobby.messageRequestNum == m_selected) m_result.text = lobby.message;
		if (info.pendingRequest == ePacketType.C2S_AnswerPartyApplication && info.pendingNumber == m_selected) m_result.text = "처리 중...";
		m_accept.interactable = m_decline.interactable = list.Count > 0 && !missing && CanRequest();
		if (list.Count == 0) return;
		if (missing)
		{
			m_message.text = "이미 처리되었거나 취소된 가입 신청입니다.";
			m_page.text = "- / " + list.Count;
			return;
		}
		PartyApplicationInfo entry = list[m_index];
		m_selected = entry.requestNum;
		string job = "전사";
		if (entry.jobClass == 1) job = "궁수";
		m_page.text = (m_index + 1) + " / " + list.Count;
		m_message.text = entry.name + " 님의 가입 신청\nLv." + entry.level + "  ·  " + job;
		if (m_owner.GetInfo().GetMembers().Count >= PartyInfo.MaxMembers) m_accept.interactable = false;
	}
}
