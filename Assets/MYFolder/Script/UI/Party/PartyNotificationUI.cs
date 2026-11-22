using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyNotificationUI : MonoBehaviour
{
	[SerializeField] Button m_badge;
	[SerializeField] TMP_Text m_count;
	[SerializeField] Button m_toast;
	[SerializeField] TMP_Text m_message;
	[SerializeField] PartyApplicationUI m_window;
	[SerializeField] RectTransform m_partyPanel;
	PartyUI m_owner;
	float m_hideTime;
	ulong m_requestNum;
	void Awake() { m_badge.onClick.AddListener(Open); m_toast.onClick.AddListener(OpenToast); }
	public void Close() { m_toast.gameObject.SetActive(false); m_badge.gameObject.SetActive(false); m_window.Close(); }
	public void Bind(PartyUI _owner)
	{
		m_owner = _owner;
		if (!_owner.IsLeader()) { Close(); m_requestNum = 0; m_hideTime = 0; return; }
		var list = _owner.GetInfo().lobby.GetApplications();
		PartyApplicationInfo application = _owner.GetInfo().lobby.TakeNewApplication();
		if (application != null)
		{
			m_requestNum = application.requestNum;
			m_message.text = application.name + "님이\n파티 가입을 신청했습니다.";
			if (list.Count > 1) m_message.text += "  (" + list.Count + ")";
			m_hideTime = Time.unscaledTime + 5;
		}
		m_badge.gameObject.SetActive(list.Count > 0);
		if (m_partyPanel != null)
		{
			RectTransform toast = (RectTransform)m_toast.transform;
			toast.anchoredPosition = m_partyPanel.anchoredPosition - new Vector2(0, m_partyPanel.rect.height + 14);
		}
		m_count.text = list.Count.ToString();
		bool exists = _owner.GetInfo().lobby.FindApplication(m_requestNum) != null;
		m_toast.gameObject.SetActive(exists && Time.unscaledTime < m_hideTime && !LoadingPanelUI.IsVisible());
	}
	void Open()
	{
		if (m_owner == null || !m_owner.IsLeader() || UIInputBlocker.IsBlocked()) return;
		m_hideTime = 0;
		m_toast.gameObject.SetActive(false);
		m_window.Open(m_owner);
	}
	void OpenToast()
	{
		if (m_owner == null || !m_owner.IsLeader() || UIInputBlocker.IsBlocked()) return;
		m_hideTime = 0;
		m_toast.gameObject.SetActive(false);
		m_window.Open(m_owner, m_requestNum);
	}
}
