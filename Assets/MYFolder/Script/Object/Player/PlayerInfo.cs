using UnityEngine;

// HUD는 이 데이터를 읽습니다. 서버 연결 후 수신값을 적용할 위치입니다.
public class PlayerInfo : MonoBehaviour
{
    [SerializeField] string m_characterName = "새싹 모험가";
    [SerializeField] int m_level = 10;
    [SerializeField] int m_maxHP = 1000;
    [SerializeField] int m_hp = 1000;
	[SerializeField] ulong m_experience;
	[SerializeField] ulong m_requiredExperience = 10000;
    public string GetName() { return m_characterName; }
    public int GetLevel() { return m_level; }
    public int GetHP() { return m_hp; }
    public int GetMaxHP() { return m_maxHP; }
	public ulong GetExperience() { return m_experience; }
	public ulong GetRequiredExperience() { return m_requiredExperience; }
	public void Initialize(string _name, int _level, int _maxHP, int _hp, ulong _experience = 0, ulong _requiredExperience = 0)
	{
		m_characterName = _name;
		m_level = _level;
		m_maxHP = Mathf.Max(1, _maxHP);
		m_hp = Mathf.Clamp(_hp, 0, m_maxHP);
		m_experience = _experience;
		m_requiredExperience = _requiredExperience;
		PlayerWorldUI worldUI = GetComponentInChildren<PlayerWorldUI>(true);
		if (worldUI != null) worldUI.Bind(this);
	}
	public void AddExperience(ulong _experience)
	{
		if (m_level >= 20) return;
		m_experience += _experience;
		if (m_requiredExperience > 0 && m_experience > m_requiredExperience) m_experience = m_requiredExperience;
	}
	public void ApplyLevelUp(int _level, ulong _experience, ulong _requiredExperience, int _maxHP, int _hp)
	{
		m_level = _level;
		m_experience = _experience;
		m_requiredExperience = _requiredExperience;
		m_maxHP = Mathf.Max(1, _maxHP);
		m_hp = Mathf.Clamp(_hp, 0, m_maxHP);
		PlayerWorldUI worldUI = GetComponentInChildren<PlayerWorldUI>(true);
		if (worldUI != null) worldUI.Refresh();
	}
	public int RecoverHalfHP()
	{
		if (m_hp <= 0 || m_hp >= m_maxHP) return 0;
		int recovery = m_maxHP / 2;
		if (recovery > m_maxHP - m_hp) recovery = m_maxHP - m_hp;
		m_hp += recovery;
		return recovery;
	}
	public void ApplyServerHP(int _hp)
	{
		m_hp = Mathf.Clamp(_hp, 0, m_maxHP);
		PlayerWorldUI worldUI = GetComponentInChildren<PlayerWorldUI>(true);
		if (worldUI != null) worldUI.Refresh();
	}
}
