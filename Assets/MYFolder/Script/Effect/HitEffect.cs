using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HitEffect : MonoBehaviour
{
	public GameObject m_hitEffectPrefab;
	public void PlayHitEffect()
	{
		StartCoroutine(PlayHitEffectCoroutine());
	}
	private IEnumerator PlayHitEffectCoroutine()
	{
		GameObject effect = Instantiate(m_hitEffectPrefab, transform.position, Quaternion.identity);

		yield return new WaitForSeconds(1.15f); 

		Destroy(effect);
	}
}
