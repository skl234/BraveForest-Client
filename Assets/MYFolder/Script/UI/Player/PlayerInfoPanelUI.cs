using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoPanelUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_name;
	[SerializeField] TMP_Text m_class;
	[SerializeField] TMP_Text m_level;
	[SerializeField] TMP_Text m_hp;
	[SerializeField] TMP_Text m_experience;
	[SerializeField] Image m_hpFill;
	[SerializeField] Image m_experienceFill;
	[SerializeField] Button m_close;
	PlayerInfo m_info;

	void Awake() { m_close.onClick.AddListener(Close); }
	public void Bind(PlayerInfo _info, ePlayerClass _class)
	{
		m_info = _info;
		m_class.text = "전사";
		if (_class == ePlayerClass.Archer) m_class.text = "궁수";
		Refresh();
	}
	public void Open() { Refresh(); gameObject.SetActive(true); }
	public void Close() { gameObject.SetActive(false); }
	void LateUpdate() { Refresh(); }
	void Refresh()
	{
		if (m_info == null) return;
		m_name.text = m_info.GetName();
		m_level.text = "Lv. " + m_info.GetLevel();
		m_hp.text = m_info.GetHP() + " / " + m_info.GetMaxHP();
		m_hpFill.fillAmount = (float)m_info.GetHP() / Mathf.Max(1, m_info.GetMaxHP());
		float experience = m_info.GetExperienceRatio();
		m_experienceFill.fillAmount = experience;
		m_experience.text = Mathf.FloorToInt(experience * 100.0f) + "%";
	}
}
