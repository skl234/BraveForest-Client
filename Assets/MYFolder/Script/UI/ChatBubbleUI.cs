using TMPro;
using UnityEngine;

public class ChatBubbleUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_text;
	[SerializeField] CanvasGroup m_group;
	[SerializeField] float m_duration = 3.0f;
	float m_startTime;
	float m_visibleDuration;

	public void Show(string _text)
	{
		Show(_text, m_duration);
	}

	public void ShowPermanent(string _text)
	{
		Show(_text, 0);
	}

	void Show(string _text, float _duration)
	{
		m_text.richText = false;
		m_text.text = _text;
		Vector2 size = m_text.GetPreferredValues(_text, 240, float.PositiveInfinity);
		RectTransform rect = (RectTransform)transform;
		rect.sizeDelta = new Vector2(Mathf.Clamp(size.x + 30, 88, 270), Mathf.Max(size.y + 22, 46));
		m_startTime = Time.unscaledTime;
		m_visibleDuration = _duration;
		m_group.alpha = 0;
		gameObject.SetActive(true);
	}

	public void Hide() { gameObject.SetActive(false); }

	void LateUpdate()
	{
		float elapsed = Time.unscaledTime - m_startTime;
		m_group.alpha = Mathf.Clamp01(elapsed / .12f);
		if (m_visibleDuration > 0 && elapsed > m_visibleDuration - .25f) m_group.alpha = Mathf.Clamp01((m_visibleDuration - elapsed) / .25f);
		if (m_visibleDuration > 0 && elapsed >= m_visibleDuration) Hide();
		Camera camera = Camera.main;
		if (camera == null || transform.parent == null) return;
		transform.rotation = camera.transform.rotation;
		// 멀리 줌아웃해도 대화 글자 크기는 읽을 수 있게 유지
		float depth = Vector3.Dot(transform.position - camera.transform.position, camera.transform.forward);
		float pixelSize = 2 * Mathf.Max(depth, .1f) * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad) / Mathf.Max(camera.pixelHeight, 1);
		float parentScale = Mathf.Max(transform.parent.lossyScale.y, .0001f);
		transform.localScale = Vector3.one * (pixelSize / parentScale) * Mathf.Lerp(.94f, 1, Mathf.Clamp01(elapsed / .12f));
	}
}
