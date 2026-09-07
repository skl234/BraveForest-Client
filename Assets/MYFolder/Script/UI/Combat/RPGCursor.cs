using UnityEngine;

public class RPGCursor : MonoBehaviour
{
    [SerializeField] Texture2D m_normal;
    [SerializeField] Texture2D m_attack;
    [SerializeField] Vector2 m_normalHotspot = new Vector2(6, 5);
    [SerializeField] Vector2 m_attackHotspot = new Vector2(24, 24);
    bool m_targetable;
    bool m_initialized;
    public bool IsTargetable() { return m_targetable; }
    void OnEnable() { m_initialized = false; SetTargetable(false); }
    public void SetTargetable(bool _targetable)
    {
        if (m_initialized && m_targetable == _targetable) return;
        m_initialized = true;
        m_targetable = _targetable;
        Cursor.SetCursor(_targetable ? m_attack : m_normal, _targetable ? m_attackHotspot : m_normalHotspot, CursorMode.Auto);
    }
    void OnDisable() { Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto); m_initialized = false; }
}
