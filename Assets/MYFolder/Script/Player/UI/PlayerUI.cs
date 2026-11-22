using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PlayerUI : MonoBehaviour
{
	public  GameObject m_playerObj;
	private Player m_player;

	public GameObject m_hpBarObj;
	private Image m_hpBarImg;

	public GameObject m_damageTextPrefap;
	void Awake()
	{
		transform.Find("Canvas").GetComponent<Canvas>().worldCamera = Camera.main;
		m_player = m_playerObj.GetComponent<Player>();
		m_hpBarImg = m_hpBarObj.GetComponent<Image>();
	}
	void Start()
	{
		
	}

	// Update is called once per frame
	void Update()
	{
		m_hpBarImg.fillAmount = (float)m_player.GetHP() / m_player.GetMaxHP();

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
