using UnityEngine;

// 씬에 배치한 위치를 ZoneServer의 몬스터 생성 위치로 사용합니다.
public class MonsterSpawnPoint : MonoBehaviour
{
    [SerializeField] eMonsterType m_monsterType;
    [SerializeField] uint m_level = 1;
    [SerializeField] uint m_maxHP = 100;
    [SerializeField] uint m_experience = 10;
	[SerializeField] uint m_attackPower = 7;
	[SerializeField] bool m_aggressive;
	[SerializeField] bool m_canWander = true;
	[SerializeField] bool m_canChase = true;
	[SerializeField] bool m_canAttack = true;
	[SerializeField] float m_idleMinTime = 4.0f;
	[SerializeField] float m_idleMaxTime = 8.0f;
	[SerializeField] float m_wanderMinTime = 6.0f;
	[SerializeField] float m_wanderMaxTime = 10.0f;
	[SerializeField] float m_detectionRange = 5.0f;
	[SerializeField] float m_attackRange = 1.5f;
	[SerializeField] float m_moveSpeed = 3.0f;
	[SerializeField] uint m_attackIntervalMs = 2000;
    [SerializeField] float m_hitboxRadius = 0.65f;
    [SerializeField] float m_wanderRadius = 4.0f;
    [SerializeField] float m_maxAggroRange = 30.0f;
    [SerializeField] float m_respawnRadius = 1.5f;
    [SerializeField] float m_respawnDelay = 5.0f;
    [SerializeField] bool m_invincible = true;

    public eMonsterType GetMonsterType() { return m_monsterType; }
    public uint GetLevel() { return m_level; }
    public uint GetMaxHP() { return m_maxHP; }
    public uint GetExperience() { return m_experience; }
	public uint GetAttackPower() { return m_attackPower; }
	public bool IsAggressive() { return m_aggressive; }
	public bool CanWander() { return m_canWander; }
	public bool CanChase() { return m_canChase; }
	public bool CanAttack() { return m_canAttack; }
	public float GetIdleMinTime() { return m_idleMinTime; }
	public float GetIdleMaxTime() { return m_idleMaxTime; }
	public float GetWanderMinTime() { return m_wanderMinTime; }
	public float GetWanderMaxTime() { return m_wanderMaxTime; }
	public float GetDetectionRange() { return m_detectionRange; }
	public float GetAttackRange() { return m_attackRange; }
	public float GetMoveSpeed() { return m_moveSpeed; }
	public uint GetAttackIntervalMs() { return m_attackIntervalMs; }
    public float GetHitboxRadius() { return m_hitboxRadius; }
    public float GetWanderRadius() { return m_wanderRadius; }
    public float GetMaxAggroRange() { return m_maxAggroRange; }
    public float GetRespawnRadius() { return m_respawnRadius; }
    public float GetRespawnDelay() { return m_respawnDelay; }
    public bool IsInvincible() { return m_invincible; }

    public bool IsValid()
    {
        if (m_monsterType > eMonsterType.Rat) return false;
        if (m_level == 0 || m_maxHP == 0 || m_attackPower == 0) return false;
        if (m_hitboxRadius <= 0.0f) return false;
		if (m_detectionRange < 0.0f || m_attackRange < 0.0f || m_moveSpeed <= 0.0f) return false;
		if (m_attackIntervalMs == 0) return false;
		if (m_idleMinTime < 0.0f || m_idleMaxTime < m_idleMinTime) return false;
		if (m_wanderMinTime < 0.0f || m_wanderMaxTime < m_wanderMinTime) return false;
		if (m_canWander && m_wanderRadius <= 0.0f) return false;
		if (m_canAttack && m_attackRange <= 0.0f) return false;
        if (m_wanderRadius < 0.0f || m_maxAggroRange < 0.0f) return false;
        if (m_respawnRadius < 0.0f || m_respawnDelay < 0.0f) return false;
        if (m_wanderRadius > m_maxAggroRange) return false;
        if (m_respawnRadius > m_maxAggroRange) return false;
        return true;
    }

#if UNITY_EDITOR
    public void SetEditorValues(eMonsterType _type, uint _level, uint _maxHP, uint _experience, uint _attackPower,
        float _hitboxRadius, float _wanderRadius, float _maxAggroRange,
        float _respawnRadius, float _respawnDelay)
    {
        m_monsterType = _type;
        m_level = _level;
        m_maxHP = _maxHP;
        m_experience = _experience;
		m_attackPower = _attackPower;
        m_hitboxRadius = _hitboxRadius;
        m_wanderRadius = _wanderRadius;
        m_maxAggroRange = _maxAggroRange;
        m_respawnRadius = _respawnRadius;
        m_respawnDelay = _respawnDelay;
        m_invincible = true;
    }

	public void SetCombatEditorValues(bool _aggressive, float _detectionRange,
		float _attackRange, float _moveSpeed, uint _attackIntervalMs)
	{
		m_aggressive = _aggressive;
		m_detectionRange = _detectionRange;
		m_attackRange = _attackRange;
		m_moveSpeed = _moveSpeed;
		m_attackIntervalMs = _attackIntervalMs;
		m_invincible = false;
	}

	public void SetBehaviourEditorValues(bool _canWander, bool _canChase, bool _canAttack,
		float _idleMinTime, float _idleMaxTime, float _wanderMinTime, float _wanderMaxTime)
	{
		m_canWander = _canWander;
		m_canChase = _canChase;
		m_canAttack = _canAttack;
		m_idleMinTime = _idleMinTime;
		m_idleMaxTime = _idleMaxTime;
		m_wanderMinTime = _wanderMinTime;
		m_wanderMaxTime = _wanderMaxTime;
	}
#endif
}
