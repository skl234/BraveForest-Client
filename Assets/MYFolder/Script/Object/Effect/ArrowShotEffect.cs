using UnityEngine;

// 타격 판정과 분리된 시각 효과. 이 효과의 도착을 기다려 피해를 처리하지 않습니다.
public class ArrowShotEffect : MonoBehaviour
{
    enum eTrailStyle { Basic, Power, Rapid }

    [SerializeField] LineRenderer m_trail;
    [SerializeField] LineRenderer[] m_streaks;
    [SerializeField] float[] m_streakOffsets;
    [SerializeField] eTrailStyle m_style;
    [SerializeField] float m_curveStrength = 0.12f;
    [SerializeField] float m_duration = 0.18f;
    float m_time;
    Color[] m_startColors;
    Color[] m_endColors;

    public void Initialize(Vector3 _start, Vector3 _end)
    {
        m_time = Time.time;
        if (m_streaks == null || m_streaks.Length == 0) m_streaks = new LineRenderer[] { m_trail };
        m_startColors = new Color[m_streaks.Length];
        m_endColors = new Color[m_streaks.Length];
        Vector3 direction = (_end - _start).normalized;
        Vector3 side = Vector3.Cross(direction, Vector3.up).normalized;
        if (side.sqrMagnitude < 0.01f) side = Vector3.right;
        for (int i = 0; i < m_streaks.Length; ++i)
        {
            float offset = m_streakOffsets != null && i < m_streakOffsets.Length ? m_streakOffsets[i] : 0.0f;
            SetTrail(m_streaks[i], _start, _end, side, i, offset);
            m_startColors[i] = m_streaks[i].startColor;
            m_endColors[i] = m_streaks[i].endColor;
        }
    }

    void Update()
    {
        float t = Mathf.Clamp01((Time.time - m_time) / m_duration);
        // 발사 순간 전체 궤적이 나타나고, 위치는 고정한 채 잔광만 사라집니다.
        // 처음에는 강한 색을 유지하고 끝부분에서 빠르게 사라집니다.
        float alpha = 1.0f - t * t;
        for (int i = 0; i < m_streaks.Length; ++i)
        {
            Color start = m_startColors[i]; start.a *= alpha;
            Color end = m_endColors[i]; end.a *= alpha;
            m_streaks[i].startColor = start;
            m_streaks[i].endColor = end;
        }
        if (t >= 1.0f) Destroy(gameObject);
    }

    void SetTrail(LineRenderer _line, Vector3 _start, Vector3 _end, Vector3 _side, int _index, float _offset)
    {
        const int count = 12;
        _line.positionCount = count;
        for (int i = 0; i < count; ++i)
        {
            float t = i / (float)(count - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);
            Vector3 point = Vector3.Lerp(_start, _end, t);
            // 코어는 곧게 유지하고, 가는 바깥 잔광만 한 번 벌어졌다 수렴합니다.
            if (_index > 0)
            {
                if (m_style == eTrailStyle.Basic)
                    point += _side * (_offset * envelope) + Vector3.up * (m_curveStrength * 0.25f * envelope);
                else if (m_style == eTrailStyle.Power)
                    point += _side * (_offset * envelope)
                        + Vector3.up * ((_index & 1) == 0 ? 1.0f : -1.0f) * m_curveStrength * 0.35f * envelope;
                else
                    point += _side * (_offset * envelope)
                        + Vector3.up * ((_index & 1) == 0 ? 1.0f : -1.0f) * m_curveStrength * 0.5f * envelope;
            }
            _line.SetPosition(i, point);
        }
    }
}
