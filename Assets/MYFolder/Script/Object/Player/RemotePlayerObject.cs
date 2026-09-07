using UnityEngine;
using System.Collections;

[RequireComponent(typeof(PlayerAttackEffects))]
public class RemotePlayerObject : PlayerObject
{
    [SerializeField] float m_moveSpeed = 5.0f;
    Vector3 m_destination;
    bool m_moving;
    string m_attackClip;
    Transform m_attackTarget;
    float m_attackUntil;
	byte m_attackType;
	int m_attackLevel;
	float m_attackStarted;
	float m_attackDuration;
	float m_impactRatio;
	Coroutine m_pendingArea;
	PlayerAttackEffects m_effects;
	[SerializeField] float m_areaRadius = 5.0f;

	protected override void Awake()
	{
		base.Awake();
		m_effects = GetComponent<PlayerAttackEffects>();
	}

	public void BeginServerAttack(byte _attackType, GameObject _target)
	{
		CancelArea();
		if (_attackType == 0)
		{
			m_attackType = 0;
			m_attackTarget = null;
			m_attackUntil = 0;
			if (m_state == ePlayerObjectState.Attack || m_state == ePlayerObjectState.Skill) SetState(ePlayerObjectState.Idle);
			return;
		}
		if (m_state == ePlayerObjectState.Dead) return;
		m_attackType = _attackType;
		m_attackStarted = Time.time;
		PlayerInfo info = GetComponent<PlayerInfo>();
		m_attackLevel = 1;
		if (info != null) m_attackLevel = info.GetLevel();
		string clip = "NormalAttack01_SwordShield";
		if (m_playerClass == ePlayerClass.Archer) clip = "Attack01_Bow";
		m_impactRatio = .38f;
		ePlayerObjectState state = ePlayerObjectState.Attack;
		if (_attackType == 2)
		{
			state = ePlayerObjectState.Skill;
			m_impactRatio = .45f;
			if (m_playerClass == ePlayerClass.Warrior) { clip = "NormalAttack02_SwordShield"; m_impactRatio = .34f; }
		}
		if (_attackType == 3) { state = ePlayerObjectState.Skill; clip = "JumpAttack_SwordShield"; m_impactRatio = .65f; }
		if (_attackType == 4) { state = ePlayerObjectState.Skill; clip = "Attack02RepeatFire_Bow"; m_impactRatio = 0; }
		m_attackDuration = 1;
		if (m_animator != null && m_animator.runtimeAnimatorController != null)
		{
			AnimationClip[] clips = m_animator.runtimeAnimatorController.animationClips;
			for (int i = 0; i < clips.Length; ++i) if (clips[i].name == clip) { m_attackDuration = clips[i].length; break; }
		}
		PlayServerAttack(state, clip);
		SetAttackTarget(_target, _attackType);
		// 비대상 범위기는 맞은 몬스터가 없어도 보임. 대상마다 불꽃을 중복 생성하지 않음
		if (_attackType == 3 && m_playerClass == ePlayerClass.Warrior) m_pendingArea = StartCoroutine(PlayAreaAfter(m_attackDuration * m_impactRatio));
	}

	IEnumerator PlayAreaAfter(float _delay)
	{
		yield return new WaitForSeconds(_delay);
		m_pendingArea = null;
		if (m_state != ePlayerObjectState.Dead) m_effects.PlayArea(m_areaRadius);
	}

	void CancelArea()
	{
		if (m_pendingArea != null) StopCoroutine(m_pendingArea);
		m_pendingArea = null;
	}

	void OnDisable() { StopAllCoroutines(); m_pendingArea = null; }

	public void PlayServerHit(GameObject _target, int _serverDamage)
	{
		if (_target == null) return;
		CombatHitFeedback feedback = _target.GetComponent<CombatHitFeedback>();
		if (feedback == null) return;
		if (m_attackType == 0) { feedback.Play(_serverDamage); return; }
		eCombatAttackType attack = eCombatAttackType.Basic;
		if (m_attackType == 2) attack = eCombatAttackType.SkillA;
		if (m_attackType == 3) attack = eCombatAttackType.SkillS;
		if (m_attackType == 4) attack = eCombatAttackType.RapidShot;
		int damage = 0;
		if (_serverDamage > 0) damage = CombatDamageCalculator.Calculate(m_playerClass, m_attackLevel, attack);
		float delay = Mathf.Max(0, m_attackStarted + m_attackDuration * m_impactRatio - Time.time);
		if (m_playerClass == ePlayerClass.Archer)
		{
			// 결과를 받은 직후라도 모션의 타격 시점 이전이면 기다림. HP와 숫자 연출은 분리
			StartCoroutine(PlayArrowHitAfter(_target, feedback, m_attackType, damage, delay));
			MonsterCombatStatus status = _target.GetComponent<MonsterCombatStatus>();
			if (m_attackType == 4 && status != null && status.GetHP() == 0) m_attackUntil = Time.time + delay + .15f;
		}
		else if (m_attackType == 2)
		{
			CombatDamageCalculator.SplitWarriorSkillA(damage, out int first, out int second);
			feedback.PlayDelayed(first, delay);
			feedback.PlayDelayed(second, Mathf.Max(0, m_attackStarted + m_attackDuration * .68f - Time.time));
		}
		else feedback.PlayDelayed(damage, delay);
	}

	IEnumerator PlayArrowHitAfter(GameObject _target, CombatHitFeedback _feedback, byte _attackType, int _damage, float _delay)
	{
		if (_delay > 0) yield return new WaitForSeconds(_delay);
		if (_target == null || !_target.activeInHierarchy) yield break;
		MonsterHitbox hitbox = _target.GetComponentInChildren<MonsterHitbox>();
		Vector3 end = _target.transform.position + Vector3.up;
		if (hitbox != null) end = hitbox.GetCenter();
		m_effects.PlayArrow(_attackType, end);
		_feedback.Play(_damage);
	}

    public void SetAttackTarget(GameObject _target, byte _attackType)
    {
        m_attackTarget = null;
        if (_target != null) m_attackTarget = _target.transform;
        m_attackUntil = 0;
        if (_attackType == 4) m_attackUntil = Time.time + 3;
    }

    public void PlayServerAttack(ePlayerObjectState _state, string _clip)
    {
		if (m_state == ePlayerObjectState.Dead) return;
        m_moving = false;
        m_attackClip = _clip;
        PlayAttack(_state, _clip);
    }

    public void ApplyMove(Vector3 _position, Vector3 _destination, float _rotationY)
    {
		if (m_state == ePlayerObjectState.Dead) return;
        // 0.2초 위치 보고마다 되감지 않고 현재 위치에서 같은 속도로 목적지까지 이동
        Vector3 reportedMove = _destination - _position; reportedMove.y = 0;
        if (reportedMove.sqrMagnitude < 0.0001f && (m_state == ePlayerObjectState.Attack || m_state == ePlayerObjectState.Skill)) return;
        m_destination = _destination;
        m_destination.y = _position.y;
        Vector3 remaining = m_destination - transform.position; remaining.y = 0;
        m_moving = remaining.sqrMagnitude > 0.0001f;
        if (!m_moving && m_state != ePlayerObjectState.Attack && m_state != ePlayerObjectState.Skill)
            transform.rotation = Quaternion.Euler(0, _rotationY, 0);
        if (m_moving) { CancelArea(); SetState(ePlayerObjectState.Move); }
        else if (m_state == ePlayerObjectState.Move) SetState(ePlayerObjectState.Idle);
    }

    void Update()
    {
		if (m_state == ePlayerObjectState.Dead) { CancelArea(); MaintainStateAnimation(); return; }
		if (m_attackType == 4 && Time.time >= m_attackUntil && m_state == ePlayerObjectState.Skill) SetState(ePlayerObjectState.Idle);
        if (m_moving && m_state == ePlayerObjectState.Move)
        {
            Vector3 direction = m_destination - transform.position; direction.y = 0;
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
            Vector3 position = Vector3.MoveTowards(transform.position, m_destination, m_moveSpeed * Time.deltaTime);
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null) position.y = terrain.SampleHeight(position) + terrain.transform.position.y;
            transform.position = position;
            Vector3 remaining = m_destination - position;
            remaining.y = 0.0f;
            if (remaining.sqrMagnitude < 0.0001f)
            {
                m_moving = false;
                SetState(ePlayerObjectState.Idle);
            }
        }
        if ((m_state == ePlayerObjectState.Attack || m_state == ePlayerObjectState.Skill) && m_animator != null)
        {
            if (m_attackTarget != null)
            {
                Vector3 direction = m_attackTarget.position - transform.position; direction.y = 0;
                if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
            }
            AnimatorStateInfo state = m_animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(m_attackClip) && state.normalizedTime >= 1.0f)
            {
                if (Time.time < m_attackUntil) m_animator.Play(m_attackClip, 0, 0);
                else SetState(ePlayerObjectState.Idle);
            }
        }
        MaintainStateAnimation();
    }
}
