using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterCreateInfo : MonoBehaviour
{
	public eMonsterType m_type;
	public bool m_aggressive;
	public int m_maxHP; 
	public int m_attackPower;
	public float m_attackCoolTime; 
	public float m_attackRange; 
	public float m_moveSpeed;
	public float m_hitboxRadius;
}
