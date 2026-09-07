using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CombatHUD : MonoBehaviour
{
    [SerializeField] TMP_Text m_name;
    [SerializeField] TMP_Text m_level;
    [SerializeField] TMP_Text m_hp;
    [SerializeField] Image m_hpFill;
	[SerializeField] TMP_Text m_experience;
	[SerializeField] Image m_experienceFill;
    [SerializeField] TMP_Text m_target;
    [SerializeField] TMP_Text m_status;
    [SerializeField] SkillSlotUI[] m_slots;
    [SerializeField] GameObject m_channelPanel;
    [SerializeField] Image m_channelFill;
    [SerializeField] TMP_Text m_channelText;
    PlayerInfo m_info;
    PlayerCombat m_combat;
    public void Bind(LocalPlayerObject _player)
    {
        m_info = _player.GetComponent<PlayerInfo>();
        m_combat = _player.GetComponent<PlayerCombat>();
        for (int i = 0; i < m_slots.Length; ++i) m_slots[i].SetClass(_player.GetPlayerClass());
        m_name.text = m_info.GetName() + (_player.GetPlayerClass() == ePlayerClass.Warrior ? " · 전사" : " · 궁수");
        m_level.text = "Lv. " + m_info.GetLevel();
    }
    void LateUpdate()
    {
        if (m_info == null || m_combat == null) return;
        m_level.text = "Lv. " + m_info.GetLevel();
        m_hp.text = m_info.GetHP() + " / " + m_info.GetMaxHP();
        m_hpFill.fillAmount = m_info.GetMaxHP() > 0 ? (float)m_info.GetHP() / m_info.GetMaxHP() : 0;
		if (m_info.GetRequiredExperience() == 0) m_experienceFill.fillAmount = 1.0f;
		else m_experienceFill.fillAmount = (float)m_info.GetExperience() / m_info.GetRequiredExperience();
		if (m_info.GetRequiredExperience() == 0) m_experience.text = "MAX LEVEL";
		else m_experience.text = "EXP  " + m_info.GetExperience() + " / " + m_info.GetRequiredExperience();
        m_slots[0].SetCooldown(m_combat.GetBasicCooldown(), m_combat.GetBasicDuration());
        m_slots[1].SetCooldown(m_combat.GetHeavyCooldown(), m_combat.GetHeavyDuration());
        m_slots[2].SetCooldown(m_combat.GetAreaCooldown(), m_combat.GetAreaDuration());
		if (m_slots.Length > 3) m_slots[3].SetCooldown(m_combat.GetPotionCooldown(), m_combat.GetPotionDuration());
        MonsterHitbox target = m_combat.GetTarget();
        m_target.text = "선택한 대상 없음";
        if (target != null)
        {
            MonsterCombatStatus status = target.GetComponentInParent<MonsterCombatStatus>();
            if (status != null) m_target.text = status.GetName() + "  Lv. " + status.GetLevel() + "   " + status.GetHP() + " / " + status.GetMaxHP();
        }
        m_status.text = m_combat.LastMessage;
        float channel = m_combat.GetRapidRemaining();
        m_channelPanel.SetActive(channel > 0);
        m_channelFill.fillAmount = channel / 3.0f;
        m_channelText.text = "속사 중  " + channel.ToString("F1") + "초";
    }
}
