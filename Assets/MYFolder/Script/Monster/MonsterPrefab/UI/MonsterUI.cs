using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonsterUI : MonoBehaviour
{
	private GameObject m_canvasObj;
	private Canvas m_canvas;

	public  GameObject m_monsterObj;
	private Monster m_monster;

	public GameObject m_hpBarObj;
	private Image m_hpBarImg;

	public GameObject m_damageTextPrefap;

	void Awake()
	{
		m_canvasObj = transform.Find("Canvas").gameObject;
		m_canvas = m_canvasObj.GetComponent<Canvas>();
		m_canvas.worldCamera = Camera.main;

		m_monster = m_monsterObj.GetComponent<Monster>();
		m_hpBarImg = m_hpBarObj.GetComponent<Image>();
	}
	void Update()
	{
		m_hpBarImg.fillAmount = (float)m_monster.GetHP() / (float)m_monster.GetMaxHP();

		//HPBar 카메라방향으로
		m_hpBarObj.transform.LookAt(m_hpBarObj.transform.position + Camera.main.transform.rotation * Vector3.forward,
						Camera.main.transform.rotation * Vector3.up);
	}
	public void PlayDamage(int _damage)
	{
		GameObject damageTextObj = Instantiate(m_damageTextPrefap, transform.position, Quaternion.identity);

		damageTextObj.GetComponent<DamageText>().Play(_damage);
	}
}
