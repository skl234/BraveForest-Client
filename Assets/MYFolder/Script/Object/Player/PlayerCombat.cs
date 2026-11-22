using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LocalPlayerObject))]
[RequireComponent(typeof(PlayerAttackEffects))]
public class PlayerCombat : MonoBehaviour
{
	enum eAttack { None, Basic, Heavy, Area, Rapid }

	// 사거리는 캐릭터 중심이 아닌 두 히트박스 표면 사이의 거리입니다.
	[SerializeField] float m_attackRange = 1.0f;
	[SerializeField] float m_basicInterval = 1.1f;
	[SerializeField] float m_heavyCooldown = 5.0f;
	[SerializeField] float m_areaCooldown = 10.0f;
	[SerializeField] float m_potionCooldown = 20.0f;
	[SerializeField] float m_areaRadius = 5.0f;
	[SerializeField] float m_archerRange = 10.0f;
	[SerializeField] float m_rapidDuration = 3.0f;
	[SerializeField] float m_rapidInterval = 0.15f;
	[SerializeField] LayerMask m_clickLayers = (1 << 0) | (1 << 3) | (1 << 7);

	LocalPlayerObject m_player;
	PlayerAttackEffects m_effects;
	MonsterHitbox m_target;
	MonsterHitbox m_attackTarget;
	bool m_autoAttack;
	bool m_approach;
	eAttack m_attack;
	float m_attackStart;
	float m_attackDuration;
	float m_impactTime;
	float m_secondImpactTime;
	bool m_impacted;
	bool m_secondImpacted;
	bool m_firstPredictedHit;
	int m_attackLevel = 1;
	float m_nextBasic;
	float m_nextHeavy;
	float m_nextArea;
	float m_nextPath;
	float m_nextRapid;
	float m_nextPotion;
	byte m_rapidHitIndex;
#if UNITY_EDITOR
	public int PathRefreshCount { get; private set; }
#endif
	public string LastMessage { get; private set; }
	bool IsArcher() { return m_player.GetPlayerClass() == ePlayerClass.Archer; }
	public float GetAttackRange() { if (IsArcher()) return m_archerRange; return m_attackRange; }
	public float GetHeavyDuration() { if (IsArcher()) return 3.0f; return m_heavyCooldown; }
	public float GetAreaDuration() { if (IsArcher()) return 9.0f; return m_areaCooldown; }
	public float GetRapidRemaining() { if (m_attack == eAttack.Rapid) return Mathf.Max(0, m_rapidDuration - (Time.time - m_attackStart)); return 0; }
	public float GetBasicCooldown() { return Mathf.Max(0, m_nextBasic - Time.time); }
	public float GetBasicDuration() { return m_basicInterval; }

	void Awake() { m_player = GetComponent<LocalPlayerObject>(); m_effects = GetComponent<PlayerAttackEffects>(); }

	public bool IsAttacking() { return m_attack != eAttack.None; }
	public MonsterHitbox GetTarget() { return m_target; }
	public float GetHeavyCooldown() { return Mathf.Max(0.0f, m_nextHeavy - Time.time); }
	public float GetAreaCooldown() { return Mathf.Max(0.0f, m_nextArea - Time.time); }
	public float GetPotionCooldown() { return Mathf.Max(0.0f, m_nextPotion - Time.time); }
	public float GetPotionDuration() { return m_potionCooldown; }
	public bool TryPotion()
	{
		if (Time.time < m_nextPotion) return false;
		PlayerInfo info = GetComponent<PlayerInfo>();
		if (info == null || info.GetHP() <= 0 || info.GetHP() >= info.GetMaxHP()) return false;
		if (ZoneSceneNetwork.Instance != null)
		{
			if (!ZoneSceneNetwork.Instance.SendPotion()) return false;
		}
		else if (info.RecoverHalfHP() == 0) return false;
		m_nextPotion = Time.time + m_potionCooldown;
		LastMessage = "체력을 절반 회복했습니다.";
		return true;
	}

	public void Tick()
	{
		if (m_player.GetState() == ePlayerObjectState.Dead) return;
		UpdateAttack();
		if (IsAttacking()) return;
		if (m_target == null || !m_target.IsAlive())
		{
			if (m_approach) m_player.StopMove();
			SelectTarget(null);
			m_approach = false;
			m_autoAttack = false;
			return;
		}

		float range = 0.0f;
		if (m_autoAttack) range = GetAttackRange();
		if (!m_approach && !m_autoAttack) return;
		if (InRange(m_target, range))
		{
			m_player.StopMove();
			m_approach = false;
			Face(m_target);
			if (m_autoAttack && Time.time >= m_nextBasic)
			{
				m_nextBasic = Time.time + m_basicInterval;
				if (IsArcher()) BeginAttack(eAttack.Basic, "Attack01_Bow", 0.8f, 0.38f);
				else BeginAttack(eAttack.Basic, "NormalAttack01_SwordShield", 0.8f, 0.38f);
			}
			return;
		}

		// 움직이는 타겟도 추적할 수 있도록 경로만 주기적으로 다시 계산합니다.
		if (Time.time < m_nextPath) return;
		m_nextPath = Time.time + 0.2f;
#if UNITY_EDITOR
		++PathRefreshCount;
#endif
		Vector3 direction = transform.position - m_target.GetCenter();
		direction.y = 0.0f;
		if (direction.sqrMagnitude < 0.0001f) direction = Vector3.back;
		float stopDistance = m_player.GetHitboxRadius() + m_target.GetRadius() + Mathf.Max(0.0f, range - 0.03f);
		Vector3 destination = m_target.GetCenter() + direction.normalized * stopDistance;
		destination.y = transform.position.y;
		if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 0.75f, NavMesh.AllAreas))
			m_player.MoveTo(hit.position);
	}

	public void CommandTarget(MonsterHitbox _target, bool _attack)
	{
		if (m_player.GetState() == ePlayerObjectState.Dead) return;
		if (_target == null || !_target.IsAlive()) return;
		if (m_target == _target && m_autoAttack && _attack) return;
		// 선택 대상은 공격 중에도 바뀝니다. 이미 시작한 단발 공격은 기존 타겟을 유지합니다.
		// 속사 중 다른 타겟을 선택하면 기존 속사를 끊고 새 타겟에 평타로 전환합니다.
		if (m_attack == eAttack.Rapid && _target != m_attackTarget) CancelAttack();
		if (!_attack) CancelAttack();
		if (!IsAttacking()) m_player.StopMove();
		SelectTarget(_target);
		m_autoAttack = _attack;
		m_approach = true;
		m_nextPath = 0.0f;
#if UNITY_EDITOR
		PathRefreshCount = 0;
#endif
	}

	public void CommandMove(Vector3 _position)
	{
		TryCommandMove(_position, out Vector3 destination);
	}

	public bool TryCommandMove(Vector3 _position, out Vector3 _destination)
	{
		CancelAttack();
		m_autoAttack = false;
		m_approach = false;
		if (m_player.MoveToResolved(_position, out _destination)) return true;
		m_player.StopMove();
		return false;
	}

	public void ClearTarget()
	{
		bool chasing = m_approach || m_autoAttack;
		CancelAttack();
		m_autoAttack = false;
		m_approach = false;
		SelectTarget(null);
		if (chasing) m_player.StopMove();
		else if (m_player.GetPath().Count > 0) m_player.SetState(ePlayerObjectState.Move);
	}

	void SelectTarget(MonsterHitbox _target)
	{
		if (m_target == _target) return;
		if (m_target != null) m_target.SetSelected(false);
		m_target = _target;
		if (m_target != null) m_target.SetSelected(true);
	}

	public bool InRange(MonsterHitbox _target, float _range)
	{
		if (_target == null || !_target.IsAlive()) return false;
		Vector3 delta = _target.GetCenter() - transform.position;
		delta.y = 0.0f;
		// 접근 정지/평타/강타/타격 시점에 동일한 부동소수점 허용 오차를 사용합니다.
		float distance = m_player.GetHitboxRadius() + _target.GetRadius() + _range + 0.025f;
		if (delta.sqrMagnitude > distance * distance) return false;
		// 가까워도 이동 불가 지형을 사이에 둔 타겟은 공격하지 않습니다.
		Vector3 end = _target.transform.position;
		return !NavMesh.Raycast(transform.position, end, out NavMeshHit hit, NavMesh.AllAreas);
	}

	void Face(MonsterHitbox _target)
	{
		Vector3 direction = _target.GetCenter() - transform.position;
		direction.y = 0.0f;
		if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
	}

	public bool TryHeavy()
	{
		if (m_player.GetState() == ePlayerObjectState.Dead) return false;
		if ((IsAttacking() && m_attack != eAttack.Basic) || Time.time < m_nextHeavy) return false;
		if (!InRange(m_target, GetAttackRange())) { LastMessage = "사거리 안의 타겟이 필요합니다."; return false; }
		m_nextHeavy = Time.time + GetHeavyDuration();
		m_autoAttack = true;
		Face(m_target);
		if (IsArcher()) BeginAttack(eAttack.Heavy, "Attack01_Bow", 1.0f, 0.45f);
		else BeginAttack(eAttack.Heavy, "NormalAttack02_SwordShield", 1.0f, 0.45f);
		return true;
	}

	public bool TryArea()
	{
		if (m_player.GetState() == ePlayerObjectState.Dead) return false;
		if ((IsAttacking() && m_attack != eAttack.Basic) || Time.time < m_nextArea) return false;
		if (IsArcher() && !InRange(m_target, GetAttackRange())) { LastMessage = "사거리 안의 타겟이 필요합니다."; return false; }
		m_nextArea = Time.time + GetAreaDuration();
		m_autoAttack = m_target != null && m_target.IsAlive();
		if (IsArcher()) BeginAttack(eAttack.Rapid, "Attack02RepeatFire_Bow", 1.4f, 0.65f);
		else BeginAttack(eAttack.Area, "JumpAttack_SwordShield", 1.4f, 0.65f);
		if (IsArcher())
		{
			m_attackDuration = m_rapidDuration;
			m_nextRapid = Time.time;
		}
		return true;
	}

	void BeginAttack(eAttack _attack, string _animation, float _duration, float _impact)
	{
		if (IsAttacking()) CancelAttack();
		m_player.StopMove();
		m_approach = false;
		m_attack = _attack;
		PlayerInfo attackInfo = GetComponent<PlayerInfo>();
		m_attackLevel = 1;
		if (attackInfo != null) m_attackLevel = attackInfo.GetLevel();
		m_attackTarget = m_target;
		m_attackStart = Time.time;
		LastMessage = "";
		// 실제 클립 길이를 사용해서 모션이 끝나기 전에 다른 모션으로 바뀌지 않게 합니다.
		m_attackDuration = _duration;
		Animator animator = m_player.GetAnimator();
		if (animator != null && animator.runtimeAnimatorController != null)
		{
			AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
			for (int i = 0; i < clips.Length; ++i)
				if (clips[i].name == _animation) { m_attackDuration = clips[i].length; break; }
		}
		m_impactTime = m_attackDuration * _impact;
		m_secondImpactTime = m_attackDuration * .68f;
		byte attackType = 1;
		if (_attack == eAttack.Heavy) attackType = 2;
		if (_attack == eAttack.Area) attackType = 3;
		if (_attack == eAttack.Rapid) attackType = 4;
		PlayerAnimationData.Attack animation = m_player.GetAttackAnimation(attackType);
		if (animation != null)
		{
			_animation = animation.state;
			m_attackDuration = animation.duration;
			m_impactTime = animation.duration * animation.impact;
			m_secondImpactTime = animation.duration * animation.secondImpact;
		}
		else if (_attack == eAttack.Heavy && !IsArcher()) m_impactTime = m_attackDuration * .34f;
		m_impacted = false;
		m_secondImpacted = false;
		m_firstPredictedHit = false;
		m_rapidHitIndex = 0;
		if (_attack == eAttack.Basic) m_player.PlayAttack(ePlayerObjectState.Attack, _animation);
		else m_player.PlayAttack(ePlayerObjectState.Skill, _animation);
		if (ZoneSceneNetwork.Instance != null)
		{
			ZoneSceneNetwork.Instance.SendAttackStart(attackType, m_attackTarget);
		}
	}

	void UpdateAttack()
	{
		if (!IsAttacking()) return;
		// 공격 도중 대상이 움직여도 현재 위치를 계속 바라봅니다.
		if (m_attackTarget != null && m_attackTarget.IsAlive()) Face(m_attackTarget);
		float elapsed = Time.time - m_attackStart;
		if (m_attack == eAttack.Rapid)
		{
			// 도중에 죽거나 비활성화된 타겟으로는 다음 화살을 발사하지 않습니다.
			if (m_attackTarget == null || !m_attackTarget.IsAlive() || !InRange(m_attackTarget, GetAttackRange()))
			{
				CancelAttack();
				m_autoAttack = m_target != null && m_target.IsAlive();
				m_approach = m_autoAttack;
				LastMessage = "속사 중단";
				return;
			}
			if (Time.time >= m_nextRapid && elapsed < m_rapidDuration && m_rapidHitIndex < 20)
			{
				if (ZoneSceneNetwork.Instance != null) ZoneSceneNetwork.Instance.SendAttackExecute(m_rapidHitIndex);
				++m_rapidHitIndex;
				m_nextRapid = m_attackStart + m_rapidHitIndex * m_rapidInterval;
				if (m_player.GetAttackAnimation(4) == null) m_player.GetAnimator().Play("Attack02RepeatFire_Bow", 0, 0);
				Shoot(m_attackTarget, 4, "속사", GetDamage(eCombatAttackType.RapidShot));
			}
			if (elapsed >= m_rapidDuration) CancelAttack();
			return;
		}
		if (!IsArcher() && m_attack == eAttack.Heavy)
		{
			int totalDamage = GetDamage(eCombatAttackType.SkillA);
			CombatDamageCalculator.SplitWarriorSkillA(totalDamage, out int firstDamage, out int secondDamage);
			if (!m_impacted && elapsed >= m_impactTime)
			{
				m_impacted = true;
				if (ZoneSceneNetwork.Instance != null) ZoneSceneNetwork.Instance.SendAttackExecute();
				if (InRange(m_attackTarget, GetAttackRange()))
				{
					m_firstPredictedHit = true;
					m_attackTarget.ReceivePredictedHit(firstDamage, "강타 1타");
				}
			}
			if (!m_secondImpacted && elapsed >= m_secondImpactTime)
			{
				m_secondImpacted = true;
				// 첫 타 때 서버가 총 피해를 확정해도 남은 절반의 숫자 연출은 마무리
				if (m_firstPredictedHit && m_attackTarget != null && m_attackTarget.gameObject.activeInHierarchy)
				{
					MonsterCombatStatus status = m_attackTarget.GetComponentInParent<MonsterCombatStatus>();
					if (status != null) status.PlayPredictedHit(secondDamage);
				}
			}
		}
		else if (!m_impacted && elapsed >= m_impactTime)
		{
			m_impacted = true;
			if (ZoneSceneNetwork.Instance != null) ZoneSceneNetwork.Instance.SendAttackExecute();
			if (m_attack == eAttack.Area) ApplyArea();
			else if (InRange(m_attackTarget, GetAttackRange()))
			{
				if (IsArcher())
				{
					eCombatAttackType attackType = eCombatAttackType.SkillA;
					if (m_attack == eAttack.Basic) attackType = eCombatAttackType.Basic;
					if (m_attack == eAttack.Basic) Shoot(m_attackTarget, 1, "일반 화살", GetDamage(attackType));
					else Shoot(m_attackTarget, 2, "강화 화살", GetDamage(attackType));
				}
				else m_attackTarget.ReceivePredictedHit(GetDamage(eCombatAttackType.Basic), "일반 공격");
			}
		}
		if (elapsed < m_attackDuration) return;
		CancelAttack();
	}

	void ApplyArea()
	{
		m_effects.PlayArea(m_areaRadius);
		// 고정 크기 배열을 사용하지 않으므로 범위 안의 몬스터 수가 늘어도 누락하지 않습니다.
		Collider[] colliders = Physics.OverlapSphere(transform.position, m_areaRadius, m_clickLayers, QueryTriggerInteraction.Collide);
		HashSet<MonsterHitbox> damaged = new HashSet<MonsterHitbox>();
		for (int i = 0; i < colliders.Length; ++i)
		{
			MonsterHitbox monster = colliders[i].GetComponent<MonsterHitbox>();
			if (monster == null || !monster.IsAlive() || !damaged.Add(monster)) continue;
			monster.ReceivePredictedHit(GetDamage(eCombatAttackType.SkillS), "화염 강타");
		}
	}

	void OnDestroy() { if (m_target != null) m_target.SetSelected(false); }

	void Shoot(MonsterHitbox _target, byte _attackType, string _skill, int _damage)
	{
		// 발사 순간 타격. 아래 이펙트는 연출일 뿐 충돌/도착 판정이 없습니다.
		_target.ReceivePredictedHit(_damage, _skill);
		m_effects.PlayArrow(_attackType, _target.GetCenter());
	}

	int GetDamage(eCombatAttackType _attack)
	{
		return CombatDamageCalculator.Calculate(m_player.GetPlayerClass(), m_attackLevel, _attack);
	}

	void CancelAttack()
	{
		if (m_attack == eAttack.None) return;
		if (ZoneSceneNetwork.Instance != null)
		{
			ZoneSceneNetwork.Instance.SendAttackCancel();
		}
		m_attack = eAttack.None;
		m_player.SetState(ePlayerObjectState.Idle);
	}

	public void StopCombat()
	{
		CancelAttack();
		m_autoAttack = false;
		m_approach = false;
		SelectTarget(null);
		m_player.StopMove();
	}
}
