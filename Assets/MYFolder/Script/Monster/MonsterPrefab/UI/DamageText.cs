using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
	private TextMeshPro m_text;
	private float   m_duration = 1.0f;
	private Vector3 m_moveOffset = new Vector3(0, 0.9f, 0);
	private float m_initialFontSize = 7.5f; // 시작 폰트 크기
	private float m_finalFontSize = 5f;   // 작아질 때 최종 크기

	private void Awake()
	{
		m_text = transform.Find("DamageText").GetComponent<TextMeshPro>();
	}

	public void Play(int _damage)
	{
		m_text.text = _damage.ToString();
		m_text.fontSize = m_initialFontSize;
		StartCoroutine(AnimateText());
	}

	private IEnumerator AnimateText()
	{
		float elapsed = 0f;
		Vector3 startPos = transform.position;
		Vector3 endPos = startPos + m_moveOffset;

		Color startColor = m_text.color;

		while (elapsed < m_duration)
		{
			float t = elapsed / m_duration;

			// 위치 이동
			transform.position += m_moveOffset;
			transform.position = Vector3.Lerp(startPos, endPos, t);

			// 투명도 감소
			Color c = startColor;
			c.a = Mathf.Lerp(1f, 0f, t);
			m_text.color = c;

			// 크기 점점 줄이기
			m_text.fontSize = Mathf.Lerp(m_initialFontSize, m_finalFontSize, t);

			// 카메라를 항상 바라보게
			if (Camera.main != null)
			{
				transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
						Camera.main.transform.rotation * Vector3.up);
			}

			elapsed += Time.deltaTime;
			yield return null;
		}

		Destroy(gameObject);
	}
}
