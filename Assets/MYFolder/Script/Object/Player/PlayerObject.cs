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
	bool m_deathAnimationPending;

    protected override void Awake()
    {
        CreateCharacter();
        base.Awake();
        if (m_animator != null && m_gameplayController != null)
            m_animator.runtimeAnimatorController = m_gameplayController;
        ApplyState();
    }

    void CreateCharacter()
    {
        GameObject prefab = m_playerClass == ePlayerClass.Warrior ? m_warriorPrefab : m_archerPrefab;
        if (prefab == null)
        {
            Debug.LogError("PlayerObject 캐릭터 프리팹이 연결되지 않았습니다: " + m_playerClass);
            return;
        }

        m_character = Instantiate(prefab, transform);
        m_character.name = m_playerClass.ToString();
        m_character.transform.localPosition = Vector3.zero;
        m_character.transform.localRotation = Quaternion.identity;
    }

    public void SetPlayerClass(ePlayerClass _playerClass)
    {
        if (m_playerClass == _playerClass && m_character != null) return;

        m_playerClass = _playerClass;
        if (m_character != null) Destroy(m_character);

        CreateCharacter();
        m_animator = GetComponentInChildren<Animator>();
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

    protected void ApplyState()
    {
        if (m_state == ePlayerObjectState.Attack || m_state == ePlayerObjectState.Skill) return;
        if (m_animator == null) return;

        string stateName;
        if (m_state == ePlayerObjectState.Move)
            stateName = m_playerClass == ePlayerClass.Warrior ? "Run_SwordShield" : "Run_Bow";
        else
            stateName = m_playerClass == ePlayerClass.Warrior ? "Idle_SwordShield" : "Idle_Bow";

        m_animator.Play(stateName, 0, 0.0f);
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
				m_animator.Play(GetDeathAnimation(), 0, 1.0f);
				m_animator.Update(0.0f);
				m_animator.speed = 0.0f;
			}
			return;
		}
        if (m_state == ePlayerObjectState.Attack || m_state == ePlayerObjectState.Skill) return;
        if (m_animator == null) return;

        string stateName;
        if (m_state == ePlayerObjectState.Move)
            stateName = m_playerClass == ePlayerClass.Warrior ? "Run_SwordShield" : "Run_Bow";
        else
            stateName = m_playerClass == ePlayerClass.Warrior ? "Idle_SwordShield" : "Idle_Bow";

        if (!m_animator.IsInTransition(0) && m_animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)) return;
        m_animator.Play(stateName, 0, 0.0f);
    }

    public void PlayAttack(ePlayerObjectState _state, string _clip)
    {
		if (m_state == ePlayerObjectState.Dead) return;
        m_state = _state;
        if (m_animator != null) m_animator.Play(_clip, 0, 0.0f);
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
		m_animator.Play(GetDeathAnimation(), 0, time);
		if (_finished)
		{
			m_animator.Update(0.0f);
			m_animator.speed = 0.0f;
		}
	}

	string GetDeathAnimation()
	{
		if (m_playerClass == ePlayerClass.Warrior) return "Die_SwordShield";
		return "Die_Bow";
	}
}
