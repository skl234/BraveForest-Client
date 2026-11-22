using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyWindowUI : MonoBehaviour
{
	[SerializeField] PartyMemberUI[] m_memberList;
	[SerializeField] TMP_Text m_title;
	[SerializeField] TMP_Text m_result;
	[SerializeField] TMP_InputField m_nameInput;
	[SerializeField] Button m_create;
	[SerializeField] Button m_invite;
	[SerializeField] Button m_leave;
	[SerializeField] Button m_kick;
	[SerializeField] Button m_leader;
	[SerializeField] Button m_disband;
	[SerializeField] Button m_close;
	PartyUI m_owner;
	ulong m_selectedSession;
	ulong m_selectedCharacter;

	void Awake()
	{
		m_create.onClick.AddListener(Create);
		m_invite.onClick.AddListener(Invite);
		m_leave.onClick.AddListener(Leave);
		m_kick.onClick.AddListener(Kick);
		m_leader.onClick.AddListener(ChangeLeader);
		m_disband.onClick.AddListener(Disband);
		m_close.onClick.AddListener(Close);
		m_nameInput.onValueChanged.AddListener(LimitName);
	}

	void LimitName(string _value)
	{
		if (Input.compositionString.Length > 0) return;
		string name = ChatInputState.LimitInput("/" + _value).Substring(1);
		if (_value != name) m_nameInput.SetTextWithoutNotify(name);
	}

	public void Bind(PartyUI _owner)
	{
		m_owner = _owner;
		PartyInfo info = _owner.GetInfo();
		if (info == null) return;
		PartyMemberInfo target = info.Find(m_selectedSession, m_selectedCharacter);
		if (target == null) { m_selectedSession = 0; m_selectedCharacter = 0; }
		m_title.text = info.partyName + "  " + info.GetMembers().Count + " / " + PartyInfo.MaxMembers;
		m_result.text = info.resultText;
		PartyUI.BindRows(m_memberList, info, _owner.GetCharacterIndex(), m_selectedSession, m_selectedCharacter, Select, 8);
		bool ready = _owner.GetNetwork() != null && _owner.GetNetwork().CanRequest() && !info.IsPending();
		bool leader = _owner.IsLeader();
		m_invite.gameObject.SetActive(leader);
		m_nameInput.gameObject.SetActive(leader);
		m_kick.gameObject.SetActive(leader);
		m_leader.gameObject.SetActive(leader);
		m_disband.gameObject.SetActive(leader);
		m_create.gameObject.SetActive(info.partyNum == 0);
		m_create.interactable = ready;
		m_invite.interactable = ready && leader && info.GetMembers().Count < PartyInfo.MaxMembers;
		m_nameInput.interactable = ready && leader;
		m_leave.interactable = ready && info.partyNum != 0;
		m_disband.interactable = ready && leader;
		bool other = target != null && target.characterIndex != _owner.GetCharacterIndex();
		m_kick.interactable = ready && leader && other;
		m_leader.interactable = ready && leader && other && target.state != ePartyMemberState.Offline;
	}

	void Select(PartyMemberInfo _member)
	{
		m_selectedSession = _member.sessionId;
		m_selectedCharacter = _member.characterIndex;
		Bind(m_owner);
	}

	void Create() { if (m_owner != null) { Close(); m_owner.Toggle(); } }
	void Invite()
	{
		if (m_owner == null || Input.compositionString.Length > 0) return;
		if (!m_owner.GetNetwork().Invite(m_nameInput.text)) m_result.text = "초대할 닉네임과 접속 상태를 확인해주세요.";
	}
	void Leave() { Confirm(ePacketType.C2S_LeaveParty, "파티 탈퇴", "파티에서 탈퇴할까요?"); }
	void Disband() { Confirm(ePacketType.C2S_DisbandParty, "파티 해산", "파티를 해산할까요?"); }
	void Kick() { ConfirmTarget(ePacketType.C2S_KickParty, "파티원 추방", " 님을 추방할까요?"); }
	void ChangeLeader() { ConfirmTarget(ePacketType.C2S_ChangePartyLeader, "파티장 위임", " 님에게 파티장을 위임할까요?"); }
	void ConfirmTarget(ePacketType _type, string _title, string _suffix)
	{
		PartyMemberInfo target = m_owner.GetInfo().Find(m_selectedSession, m_selectedCharacter);
		if (target != null) Confirm(_type, _title, target.name + _suffix);
	}
	void Confirm(ePacketType _type, string _title, string _message)
	{
		// 확인창을 열 때의 대상을 고정. 목록 순서가 바뀌어도 다른 사람에게 적용하지 않음
		ulong party = m_owner.GetInfo().partyNum;
		ulong session = m_selectedSession;
		ulong character = m_selectedCharacter;
		m_owner.GetConfirm().Show(_title, _message, () => m_owner.GetNetwork().Manage(_type, party, session, character));
	}
	public void Close() { gameObject.SetActive(false); }
}
