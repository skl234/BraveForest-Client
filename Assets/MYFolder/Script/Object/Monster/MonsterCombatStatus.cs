using UnityEngine;

public class MonsterCombatStatus : MonoBehaviour
{
	[SerializeField] MonsterWorldUI m_worldUI;
	[SerializeField] GameObject m_hitEffectPrefab;
	[SerializeField] DamageNumberEffect m_damageNumberPrefab;

	string m_name;
	uint m_level;
	uint m_maxHP;
	uint m_hp;

	void Awake()
	{
		MonsterSpawnPoint spawnPoint = GetComponent<MonsterSpawnPoint>();
		if (spawnPoint == null) return;
		m_name = GetMonsterName(spawnPoint.GetMonsterType());
		m_level = spawnPoint.GetLevel();
		m_maxHP = spawnPoint.GetMaxHP();
		m_hp = m_maxHP;
		if (m_worldUI != null) m_worldUI.Bind(this);
	}

	public string GetName() { return m_name; }
	public uint GetLevel() { return m_level; }
	public uint GetHP() { return m_hp; }
	public uint GetMaxHP() { return m_maxHP; }

	public void InitializeServerData(eMonsterType _type, uint _level, uint _maxHP, uint _hp)
	{
		m_name = GetMonsterName(_type);
		m_level = _level;
		m_maxHP = _maxHP;
		if (m_maxHP == 0) m_maxHP = 1;
		m_hp = _hp;
		if (m_hp > m_maxHP) m_hp = m_maxHP;
		if (m_worldUI != null) m_worldUI.Bind(this);
	}

	public void PlayPredictedHit(int _damage)
	{
		CombatHitFeedback feedback = GetComponent<CombatHitFeedback>();
		if (feedback != null) { feedback.Play(_damage); return; }
		Vector3 position = transform.position + Vector3.up * 1.1f;
		if (m_hitEffectPrefab != null)
		{
			GameObject effect = Instantiate(m_hitEffectPrefab, position, Quaternion.identity);
			effect.transform.localScale *= 1.35f;
			Destroy(effect, 3.0f);
		}
		if (m_damageNumberPrefab != null)
		{
			DamageNumberEffect number = Instantiate(m_damageNumberPrefab, transform.position + Vector3.up * 2.2f, Quaternion.identity);
			number.Play(_damage);
		}
	}

	// 서버의 전투 결과 패킷을 받은 뒤에만 호출합니다.
	public void ApplyServerHP(uint _hp)
	{
		m_hp = _hp;
		if (m_hp > m_maxHP) m_hp = m_maxHP;
		if (m_worldUI != null) m_worldUI.Refresh();
	}

	string GetMonsterName(eMonsterType _type)
	{
		if (_type == eMonsterType.Ghost) return "숲의 망령";
		if (_type == eMonsterType.BlueMushroom) return "파랑버섯";
		if (_type == eMonsterType.Knight) return "검은 갑옷 기사";
		if (_type == eMonsterType.Slime) return "슬라임";
		return "몬스터";
	}
}
