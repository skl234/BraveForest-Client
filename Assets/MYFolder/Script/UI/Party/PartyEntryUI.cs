using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyEntryUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_title;
	[SerializeField] TMP_Text m_detail;
	[SerializeField] Button m_button;
	PartyListingInfo m_info;
	Action<PartyListingInfo> m_select;
	void Awake() { m_button.onClick.AddListener(Select); }
	void Select() { if (m_info != null) m_select?.Invoke(m_info); }
	public void Bind(PartyListingInfo _info, bool _selected, Action<PartyListingInfo> _select)
	{
		m_info = _info; m_select = _select;
		gameObject.SetActive(_info != null);
		if (_info == null) return;
		m_title.text = _info.name;
		m_detail.text = _info.leaderName + "   " + _info.members.Count + " / 4";
		UIButtonFeedback feedback = m_button.GetComponent<UIButtonFeedback>();
		if (feedback != null) feedback.SetSelected(_selected);
	}
}
