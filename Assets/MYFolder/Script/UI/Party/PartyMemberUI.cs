using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_name;
	[SerializeField] TMP_Text m_level;
	[SerializeField] Image m_classIcon;
	[SerializeField] Sprite m_warriorIcon;
	[SerializeField] Sprite m_archerIcon;
	[SerializeField] TMP_Text m_location;
	[SerializeField] TMP_Text m_hpText;
	[SerializeField] Image m_hpFill;
	[SerializeField] GameObject m_leaderMark;
	[SerializeField] GameObject m_selectedMark;
	[SerializeField] Button m_button;
	[SerializeField] bool m_compact;
	PartyMemberInfo m_member;
	Action<PartyMemberInfo> m_select;

	public PartyMemberInfo GetMember() { return m_member; }

	void Awake()
	{
		if (m_button != null) m_button.onClick.AddListener(Select);
	}

	void Select()
	{
		if (m_member != null && m_select != null) m_select(m_member);
	}

	public void Bind(PartyMemberInfo _member, bool _leader, bool _selected, Action<PartyMemberInfo> _select = null)
	{
		m_member = _member;
		m_select = _select;
		gameObject.SetActive(_member != null);
		if (_member == null) return;
		string job = "전사";
		if (_member.jobClass == 1) job = "궁수";
		m_name.text = "Lv." + _member.level + " " + _member.name + " · " + job;
		if (m_compact && m_level != null)
		{
			m_name.text = _member.name + " · " + job;
			m_level.text = "Lv." + _member.level;
		}
		if (m_classIcon != null)
		{
			m_classIcon.sprite = m_warriorIcon;
			if (_member.jobClass == 1) m_classIcon.sprite = m_archerIcon;
			m_classIcon.gameObject.SetActive(!_leader && m_compact);
		}
		m_leaderMark.SetActive(_leader);
		m_selectedMark.SetActive(_selected);
		if (m_button != null) m_button.interactable = _select != null;
		string zone = "지역 " + _member.zoneId;
		if (_member.zoneId == 1) zone = "마을";
		if (_member.zoneId == 2) zone = "비기너존";
		m_location.text = zone + " · 채널 " + _member.channelId;
		m_location.gameObject.SetActive(!m_compact);
		m_hpText.gameObject.SetActive(!m_compact);
		if (_member.state == ePartyMemberState.Moving) m_location.text = "이동 중";
		else if (_member.state == ePartyMemberState.Dead) m_location.text = "사망";
		if (_member.state == ePartyMemberState.Offline)
		{
			m_location.text = "접속 종료";
			m_name.text += " (비활성)";
			m_hpText.text = "비활성";
			m_hpFill.fillAmount = 1;
			m_hpFill.color = new Color(.3f, .36f, .33f);
		}
		else
		{
			m_hpText.text = _member.hp + " / " + _member.maxHP;
			m_hpFill.fillAmount = 0;
			if (_member.maxHP != 0) m_hpFill.fillAmount = Mathf.Clamp01((float)_member.hp / _member.maxHP);
			else m_hpText.text = "동기화 중";
			m_hpFill.color = new Color(0f, .9f, .65f);
		}
	}
}
