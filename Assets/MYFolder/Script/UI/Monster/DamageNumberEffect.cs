using TMPro;
using UnityEngine;

public class DamageNumberEffect : MonoBehaviour
{
    [SerializeField] TMP_Text m_text;
    [SerializeField] float m_duration = 0.9f;
    [SerializeField] float m_height = 1.2f;
    float m_startTime;
    Vector3 m_startPosition;
    Color m_startColor;

    public void Play(int _damage)
    {
        m_startTime = Time.time;
        m_startPosition = transform.position;
        m_text.text = _damage.ToString();
        m_startColor = m_text.color;
    }

    public void Play(int _damage, Color _color)
    {
        m_text.color = _color;
        Play(_damage);
    }

    void Update()
    {
        float ratio = (Time.time - m_startTime) / m_duration;
        transform.position = m_startPosition + Vector3.up * (m_height * ratio);
        Camera camera = Camera.main;
        if (camera != null) transform.rotation = camera.transform.rotation;
        Color color = m_startColor;
        color.a = 1.0f - Mathf.Clamp01(ratio);
        m_text.color = color;
        if (ratio >= 1.0f) Destroy(gameObject);
    }
}
