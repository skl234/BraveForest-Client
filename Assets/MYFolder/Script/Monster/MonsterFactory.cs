using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterFactory : MonoBehaviour
{
	public GameObject m_ghostPrefab;
	public GameObject m_lizardPrefab;
	public GameObject m_knightPrefab;
	public GameObject m_ratPrefab;
	public GameObject m_demonPrefab; 
	public GameObject CreateMonster(eMonsterType _type)
	{
		if (_type == eMonsterType.Ghost) return Instantiate(m_ghostPrefab);
		if (_type == eMonsterType.BlueMushroom) return Instantiate(m_lizardPrefab);
		if (_type == eMonsterType.Knight) return Instantiate(m_knightPrefab);
		if (_type == eMonsterType.Slime) return Instantiate(m_ratPrefab);
		if (_type == eMonsterType.Demon) return Instantiate(m_demonPrefab);

		return null;
	}
}
