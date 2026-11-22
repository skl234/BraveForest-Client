using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
	[SerializeField] Image m_icon;
	[SerializeField] Image m_cooldownMask;
	[SerializeField] GameObject m_cooldownDisc;
	[SerializeField] TMP_Text m_seconds;
	[SerializeField] TMP_Text m_skillName;
	[SerializeField] Sprite m_warriorIcon;
	[SerializeField] Sprite m_archerIcon;
	[SerializeField] string m_warriorName;
	[SerializeField] string m_archerName;
	public void SetClass(ePlayerClass _class)
	{
		if (_class == ePlayerClass.Archer)
		{
			m_icon.sprite = m_archerIcon;
			m_skillName.text = m_archerName;
		}
		else
		{
			m_icon.sprite = m_warriorIcon;
			m_skillName.text = m_warriorName;
		}
	}
	public void SetCooldown(float _remaining, float _duration)
	{
		bool cooling = _remaining > 0;
		m_cooldownMask.gameObject.SetActive(cooling);
		if (m_cooldownDisc != null) m_cooldownDisc.SetActive(cooling);
		m_seconds.gameObject.SetActive(cooling);
		m_cooldownMask.fillAmount = 0;
		if (_duration > 0) m_cooldownMask.fillAmount = Mathf.Clamp01(_remaining / _duration);
		m_seconds.text = "";
		if (cooling) m_seconds.text = Mathf.CeilToInt(_remaining).ToString();
	}
}
