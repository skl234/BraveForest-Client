using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MonsterWorldUI : MonoBehaviour
{
    [SerializeField] TMP_Text m_nameText;
    [SerializeField] TMP_Text m_levelText;
    [SerializeField] Image m_hpFill;
    MonsterCombatStatus m_status;

    public void Bind(MonsterCombatStatus _status)
    {
        m_status = _status;
        Refresh();
    }

    public void Refresh()
    {
        if (m_status == null) return;
        if (m_nameText != null) m_nameText.text = m_status.GetName();
        if (m_levelText != null) m_levelText.text = "Lv. " + m_status.GetLevel();
        if (m_hpFill != null)
        {
            float amount = 0.0f;
            if (m_status.GetMaxHP() > 0) amount = (float)m_status.GetHP() / m_status.GetMaxHP();
            m_hpFill.fillAmount = amount;
        }
    }

    void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        transform.rotation = camera.transform.rotation;
    }
}
