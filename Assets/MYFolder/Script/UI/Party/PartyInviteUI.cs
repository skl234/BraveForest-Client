using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyInviteUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_message;
	[SerializeField] Button m_accept;
	[SerializeField] Button m_decline;
	PartyUI m_owner;
	NoticePanelUI m_notice;
	ulong m_inviteNum;

	void Awake()
	{
		m_notice = FindObjectOfType<NoticePanelUI>(true);
		m_accept.onClick.AddListener(Accept);
		m_decline.onClick.AddListener(Decline);
	}

	public void Bind(PartyUI _owner)
	{
		m_owner = _owner;
		PartyInfo info = _owner.GetInfo();
		bool visible = info != null && info.inviteNum != 0;
		if (LoadingPanelUI.IsVisible() || _owner.GetConfirm().gameObject.activeSelf) visible = false;
		if (m_notice != null && m_notice.gameObject.activeInHierarchy) visible = false;
		gameObject.SetActive(visible);
		if (!visible) return;
		m_inviteNum = info.inviteNum;
		m_message.text = info.inviterName + " 님이\n파티에 초대했습니다.";
		bool ready = _owner.GetNetwork() != null && _owner.GetNetwork().CanRequest() && !info.IsPending();
		m_accept.interactable = ready;
		m_decline.interactable = ready;
	}

	void Accept() { m_owner.GetNetwork().Answer(m_inviteNum, true); }
	void Decline() { m_owner.GetNetwork().Answer(m_inviteNum, false); }
}
