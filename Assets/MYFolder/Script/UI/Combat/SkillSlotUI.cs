using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotUI : MonoBehaviour
{
    [SerializeField] Image m_icon;
    [SerializeField] Image m_cooldownMask;
    [SerializeField] TMP_Text m_seconds;
    [SerializeField] TMP_Text m_skillName;
    [SerializeField] Sprite m_warriorIcon;
    [SerializeField] Sprite m_archerIcon;
    [SerializeField] string m_warriorName;
    [SerializeField] string m_archerName;
    public void SetClass(ePlayerClass _class)
    {
        bool archer = _class == ePlayerClass.Archer;
        m_icon.sprite = archer ? m_archerIcon : m_warriorIcon;
        m_skillName.text = archer ? m_archerName : m_warriorName;
    }
    public void SetCooldown(float _remaining, float _duration)
    {
        bool cooling = _remaining > 0;
        m_cooldownMask.gameObject.SetActive(cooling);
        m_seconds.gameObject.SetActive(cooling);
        m_cooldownMask.fillAmount = _duration > 0 ? Mathf.Clamp01(_remaining / _duration) : 0;
        m_seconds.text = cooling ? Mathf.CeilToInt(_remaining).ToString() : "";
    }
}
