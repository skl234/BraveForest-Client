using UnityEngine;

// 판정이 없는 목적지 알림. 도착 여부와 무관하게 짧게 펼쳐졌다 사라집니다.
public class DestinationIndicator : MonoBehaviour
{
    [SerializeField] float m_lifetime = 0.55f;
    [SerializeField] LineRenderer[] m_lines;
    float m_started;
    void Awake() { m_started = Time.time; }
    void Update()
    {
        float t = Mathf.Clamp01((Time.time - m_started) / m_lifetime);
        transform.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.15f, t);
        for (int i = 0; i < m_lines.Length; ++i)
        {
            Color color = new Color(1, 1, 1, 1 - t);
            m_lines[i].startColor = color; m_lines[i].endColor = color;
        }
        if (t >= 1) Destroy(gameObject);
    }
}
