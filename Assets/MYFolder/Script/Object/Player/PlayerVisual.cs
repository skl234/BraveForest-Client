using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
	[SerializeField] Animator m_bodyAnimator;
	[SerializeField] Animator m_bowAnimator;
	[SerializeField] Animator m_arrowAnimator;
	[SerializeField] PlayerAnimationData m_animationData;
	[SerializeField] Transform m_arrowFirePoint;
	[SerializeField] Transform m_hitPoint;
	[SerializeField] Renderer[] m_loadedArrow;
	PlayerAnimationData.Attack m_attack;

	public Animator GetAnimator() { return m_bodyAnimator; }
	public PlayerAnimationData GetAnimationData() { return m_animationData; }
	public Transform GetArrowFirePoint() { return m_arrowFirePoint; }
	public Transform GetHitPoint() { return m_hitPoint; }

	void Awake()
	{
		m_bodyAnimator.applyRootMotion = false;
		if (m_bowAnimator != null) m_bowAnimator.applyRootMotion = false;
		if (m_arrowAnimator != null) m_arrowAnimator.applyRootMotion = false;
	}

	public void Play(string _state, float _time = 0)
	{
		m_attack = m_animationData.FindState(_state);
		if (m_attack != null && m_attack.repeat && _time == 0) _time = m_attack.impact;
		PlayAnimator(m_bodyAnimator, _state, _time);
		PlayAnimator(m_bowAnimator, _state, _time);
		PlayAnimator(m_arrowAnimator, _state, _time);
		SetArrowVisible(_state != m_animationData.GetDeathState());
	}

	static void PlayAnimator(Animator _animator, string _state, float _time)
	{
		if (_animator == null) return;
		_animator.speed = 1;
		_animator.Play(_state, 0, _time);
	}

	public void Freeze()
	{
		m_bodyAnimator.Update(0);
		m_bodyAnimator.speed = 0;
		if (m_bowAnimator != null) { m_bowAnimator.Update(0); m_bowAnimator.speed = 0; }
		if (m_arrowAnimator != null) { m_arrowAnimator.Update(0); m_arrowAnimator.speed = 0; }
		SetArrowVisible(false);
	}

	void LateUpdate()
	{
		if (m_attack == null || m_loadedArrow == null || m_loadedArrow.Length == 0) return;
		AnimatorStateInfo state = m_bodyAnimator.GetCurrentAnimatorStateInfo(0);
		if (!state.IsName(m_attack.state)) return;
		float time = state.normalizedTime;
		if (m_attack.repeat) time -= Mathf.Floor(time);
		SetArrowVisible(time < m_attack.impact || (m_attack.repeat && time > .85f));
	}

	void SetArrowVisible(bool _visible)
	{
		if (m_loadedArrow == null) return;
		for (int i = 0; i < m_loadedArrow.Length; ++i)
			if (m_loadedArrow[i] != null) m_loadedArrow[i].enabled = _visible;
	}
}
