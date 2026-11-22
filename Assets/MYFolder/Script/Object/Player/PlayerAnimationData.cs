using System;
using UnityEngine;

[CreateAssetMenu(menuName = "MMO/Player Animation")]
public class PlayerAnimationData : ScriptableObject
{
	[Serializable]
	public class Attack
	{
		public byte type;
		public string state;
		public AnimationClip clip;
		public float duration;
		[Range(0, 1)] public float impact;
		[Range(0, 1)] public float secondImpact;
		public bool repeat;
	}

	[SerializeField] string m_idleState;
	[SerializeField] string m_moveState;
	[SerializeField] string m_deathState;
	[SerializeField] Attack[] m_attacks;

	public string GetIdleState() { return m_idleState; }
	public string GetMoveState() { return m_moveState; }
	public string GetDeathState() { return m_deathState; }

	public Attack GetAttack(byte _type)
	{
		for (int i = 0; i < m_attacks.Length; ++i)
			if (m_attacks[i].type == _type) return m_attacks[i];
		return null;
	}

	public Attack FindState(string _state)
	{
		for (int i = 0; i < m_attacks.Length; ++i)
			if (m_attacks[i].state == _state) return m_attacks[i];
		return null;
	}
}
