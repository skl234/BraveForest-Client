using UnityEngine;
using System.Collections;

public enum ePlayerClass : byte
{
	Warrior = 0,
	Archer,
}

public enum ePlayerObjectState : byte
{
	Idle = 0,
	Move,
	Attack,
	Skill,
	Dead,
}

public class PlayerObject : MMO.Object
{
	[SerializeField] protected ePlayerClass m_playerClass = ePlayerClass.Warrior;
	[SerializeField] protected ePlayerObjectState m_state = ePlayerObjectState.Idle;
	[SerializeField] GameObject m_warriorPrefab;
	[SerializeField] GameObject m_archerPrefab;
	[SerializeField] RuntimeAnimatorController m_gameplayController;

	protected GameObject m_character;
	PlayerVisual m_visual;
	bool m_deathAnimationPending;

	protected override void Awake()
	{
		CreateCharacter();
		base.Awake();
		if (m_visual != null) m_animator = m_visual.GetAnimator();
		else if (m_animator != null && m_gameplayController != null)
			m_animator.runtimeAnimatorController = m_gameplayController;
		ApplyState();
	}

	void CreateCharacter()
	{
		GameObject prefab = m_warriorPrefab;
		if (m_playerClass != ePlayerClass.Warrior) prefab = m_archerPrefab;
		if (prefab == null)
		{
			Debug.LogError("PlayerObject 캐릭터 프리팹이 연결되지 않았습니다: " + m_playerClass);
			return;
		}

		m_character = Instantiate(prefab, transform);
		m_character.name = m_playerClass.ToString();
		m_character.transform.localPosition = Vector3.zero;
		m_character.transform.localRotation = Quaternion.identity;
		m_visual = m_character.GetComponent<PlayerVisual>();
		PlayerAttackEffects effects = GetComponent<PlayerAttackEffects>();
		if (effects != null) effects.Bind(m_visual);
		CombatHitFeedback feedback = GetComponent<CombatHitFeedback>();
		if (feedback != null)
		{
			Transform point = null;
			if (m_visual != null) point = m_visual.GetHitPoint();
			feedback.BindHitPoint(point);
		}
	}

	public void SetPlayerClass(ePlayerClass _playerClass)
	{
		if (m_playerClass == _playerClass && m_character != null) return;

		m_playerClass = _playerClass;
		if (m_character != null)
		{
			m_character.SetActive(false);
			Destroy(m_character);
		}

		CreateCharacter();
		m_animator = m_character.GetComponentInChildren<Animator>();
		if (m_visual != null) m_animator = m_visual.GetAnimator();
		if (m_animator != null) m_animator.applyRootMotion = false;
		ApplyState();
	}

	public void SetState(ePlayerObjectState _state)
	{
		if (m_state == ePlayerObjectState.Dead) return;
		if (_state == ePlayerObjectState.Dead) { ApplyDeath(); return; }
		if (m_state == _state) return;

		m_state = _state;
		ApplyState();
	}

	public ePlayerClass GetPlayerClass()
	{
		return m_playerClass;
	}

	public ePlayerObjectState GetState()
	{
		return m_state;
	}

	string GetMovementAnimation()
	{
		if (m_visual != null)
		{
			if (m_state == ePlayerObjectState.Move) return m_visual.GetAnimationData().GetMoveState();
			return m_visual.GetAnimationData().GetIdleState();
		}
		if (m_state == ePlayerObjectState.Move)
		{
			if (m_playerClass == ePlayerClass.Warrior) return "Run_SwordShield";
			return "Run_Bow";
		}
		if (m_playerClass == ePlayerClass.Warrior) return "Idle_SwordShield";
		return "Idle_Bow";
	}

	protected void ApplyState()
	{
		if (m_state == ePlayerObjectState.Attack || m_state == ePlayerObjectState.Skill) return;
		if (m_animator == null) return;

		string stateName = GetMovementAnimation();

		PlayVisual(stateName);
	}

	protected void MaintainStateAnimation()
	{
		if (m_state == ePlayerObjectState.Dead)
		{
			if (m_deathAnimationPending) return;
			if (m_animator == null || m_animator.speed == 0.0f) return;
			AnimatorStateInfo state = m_animator.GetCurrentAnimatorStateInfo(0);
			if (state.IsName(GetDeathAnimation()) && state.normalizedTime >= 1.0f)
			{
				// 한 번 쓰러진 다음 마지막 자세를 유지
				PlayVisual(GetDeathAnimation(), 1);
				FreezeVisual();
			}
			return;
		}
		if (m_state == ePlayerObjectState.Attack || m_state == ePlayerObjectState.Skill) return;
		if (m_animator == null) return;

		string stateName = GetMovementAnimation();

		if (!m_animator.IsInTransition(0) && m_animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)) return;
		PlayVisual(stateName);
	}

	public void PlayAttack(ePlayerObjectState _state, string _clip)
	{
		if (m_state == ePlayerObjectState.Dead) return;
		m_state = _state;
		PlayVisual(_clip);
	}

	public PlayerAnimationData.Attack GetAttackAnimation(byte _type)
	{
		if (m_visual == null) return null;
		return m_visual.GetAnimationData().GetAttack(_type);
	}

	void PlayVisual(string _state, float _time = 0)
	{
		if (m_visual != null) m_visual.Play(_state, _time);
		else if (m_animator != null) m_animator.Play(_state, 0, _time);
	}

	void FreezeVisual()
	{
		if (m_visual != null) m_visual.Freeze();
		else if (m_animator != null) { m_animator.Update(0); m_animator.speed = 0; }
	}

	public void ApplyDeath(bool _finished = false)
	{
		if (m_state == ePlayerObjectState.Dead) return;
		m_state = ePlayerObjectState.Dead;
		PlayerCombat combat = GetComponent<PlayerCombat>();
		if (combat != null) combat.StopCombat();
		LocalPlayerObject local = this as LocalPlayerObject;
		if (local != null) local.StopMove();
		CombatHitFeedback feedback = GetComponent<CombatHitFeedback>();
		float delay = 0;
		if (feedback != null && !_finished) delay = feedback.GetPendingHitDelay();
		if (delay > 0)
		{
			// 사망 판정/입력 차단은 즉시, 쓰러지는 모습은 치명타가 닿을 때
			m_deathAnimationPending = true;
			StartCoroutine(PlayDeathAfterHit(delay));
			return;
		}
		PlayDeathAnimation(_finished);
	}

	IEnumerator PlayDeathAfterHit(float _delay)
	{
		yield return new WaitForSeconds(_delay);
		m_deathAnimationPending = false;
		PlayDeathAnimation(false);
	}

	void PlayDeathAnimation(bool _finished)
	{
		if (m_animator == null) return;
		float time = 0.0f;
		if (_finished) time = 1.0f;
		m_animator.speed = 1.0f;
		PlayVisual(GetDeathAnimation(), time);
		if (_finished)
		{
			FreezeVisual();
		}
	}

	string GetDeathAnimation()
	{
		if (m_visual != null) return m_visual.GetAnimationData().GetDeathState();
		if (m_playerClass == ePlayerClass.Warrior) return "Die_SwordShield";
		return "Die_Bow";
	}
}
