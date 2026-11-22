using UnityEngine;

// 클릭과 전투 거리 판정의 유일한 몬스터 Collider입니다.
[RequireComponent(typeof(SphereCollider))]
public class MonsterHitbox : MonoBehaviour
{
	[SerializeField] GameObject m_targetRing;
	int m_hitCount;
	bool m_alive = true;
	string m_lastHit = "-";
	SphereCollider m_collider;
	MonsterObject m_monster;
	MonsterCombatStatus m_combatStatus;

	void Awake()
	{
		m_collider = GetComponent<SphereCollider>();
		m_monster = GetComponentInParent<MonsterObject>();
		m_combatStatus = GetComponentInParent<MonsterCombatStatus>();
		SetSelected(false);
	}

	void Start()
	{
		// 기존 외형 프리팹의 물리 판정과 전용 Hitbox가 중복되지 않도록 합니다.
		Collider[] colliders = m_monster.GetComponentsInChildren<Collider>(true);
		for (int i = 0; i < colliders.Length; ++i)
			if (colliders[i] != m_collider) colliders[i].enabled = false;
	}

	public Vector3 GetCenter() { return transform.TransformPoint(GetComponent<SphereCollider>().center); }
	public float GetRadius()
	{
		Vector3 scale = transform.lossyScale;
		return GetComponent<SphereCollider>().radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
	}
	public int GetHitCount() { return m_hitCount; }
	public string GetLastHit() { return m_lastHit; }
	public bool IsAlive() { return m_alive && gameObject.activeInHierarchy && (m_combatStatus == null || m_combatStatus.GetHP() > 0); }
	public void SetSelected(bool _selected)
	{
		if (m_targetRing != null) m_targetRing.SetActive(_selected && IsAlive());
	}
	// 오프라인에서는 피해량/방어력/HP 계산 없이 타격 사실만 확인합니다.
	public void ReceiveHit(string _skill)
	{
		ReceivePredictedHit(0, _skill);
	}

	public void ReceivePredictedHit(int _damage, string _skill)
	{
		if (!IsAlive()) return;
		++m_hitCount;
		m_lastHit = _skill;
		if (m_combatStatus != null) m_combatStatus.PlayPredictedHit(_damage);
		Debug.Log("[TestHit] " + transform.parent.name + " " + _skill + " count=" + m_hitCount);
	}

	public void SetDead()
	{
		m_alive = false;
		SetSelected(false);
		m_collider.enabled = false;
		if (m_monster != null) m_monster.SetState(eMonsterState.Dead);
	}

	public void ResetTest()
	{
		m_alive = true;
		m_hitCount = 0;
		m_lastHit = "-";
		m_collider.enabled = true;
		SetSelected(false);
		if (m_monster != null) m_monster.SetState(eMonsterState.Idle);
	}
}
