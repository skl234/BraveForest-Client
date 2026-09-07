using UnityEngine;
using UnityEngine.EventSystems;

// 포인터 입력만 담당합니다. 전투 판정/경로 계산/시각 효과 구현은 별도 컴포넌트에 위임합니다.
[RequireComponent(typeof(PlayerCombat))]
public class WorldPointerInput : MonoBehaviour
{
    [SerializeField] float m_commandInterval = 0.2f;
    [SerializeField] LayerMask m_worldLayers = (1 << 0) | (1 << 3) | (1 << 7);
    [SerializeField] DestinationIndicator m_destinationPrefab;
    [SerializeField] RPGCursor m_cursor;
    PlayerCombat m_combat;
    float m_nextCommand;
    DestinationIndicator m_indicator;
    public Vector3 LastDestination { get; private set; }
    public int MoveCommandCount { get; private set; }
    void Awake() { m_combat = GetComponent<PlayerCombat>(); }

    public void Tick()
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>() != null) return;
        if (Input.GetKeyDown(KeyCode.A)) m_combat.TryHeavy();
        if (Input.GetKeyDown(KeyCode.S)) m_combat.TryArea();
		if (Input.GetKeyDown(KeyCode.D)) m_combat.TryPotion();
        bool ui = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        bool hit = false;
        RaycastHit result = default;
        if (!ui && Camera.main != null)
            hit = Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out result, 2000, m_worldLayers, QueryTriggerInteraction.Collide);
        MonsterHitbox monster = hit ? result.collider.GetComponent<MonsterHitbox>() : null;
        if (monster != null && !monster.IsAlive()) monster = null;
        if (m_cursor != null) m_cursor.SetTargetable(monster != null);
        if (!hit || ui) return;
        if (Input.GetMouseButtonDown(0)) SubmitLeft(monster);
        else if (Input.GetMouseButton(1)) SubmitRight(monster, result.point, Time.unscaledTime);
    }

    public void SubmitLeft(MonsterHitbox _monster)
    {
        if (_monster != null && _monster.IsAlive())
        {
            HideDestination();
            m_combat.CommandTarget(_monster, true);
        }
        else if (m_combat.GetTarget() != null) m_combat.ClearTarget();
    }

    // 클릭을 떼었다 다시 눌러도 타이머를 초기화하지 않으므로 연타로 제한을 우회할 수 없습니다.
    public bool SubmitRight(MonsterHitbox _monster, Vector3 _point, float _now)
    {
        if (_now + 0.00001f < m_nextCommand) return false;
        m_nextCommand = _now + m_commandInterval;
        if (_monster != null && _monster.IsAlive())
        {
            HideDestination();
            m_combat.CommandTarget(_monster, true);
            return true;
        }
        if (!m_combat.TryCommandMove(_point, out Vector3 destination)) return false;
        LastDestination = destination;
        ++MoveCommandCount;
        if (m_destinationPrefab != null)
        {
            HideDestination();
            m_indicator = Instantiate(m_destinationPrefab, destination + Vector3.up * 0.06f, Quaternion.identity);
        }
        return true;
    }
    void HideDestination() { if (m_indicator != null) Destroy(m_indicator.gameObject); }
    void OnDisable() { HideDestination(); if (m_cursor != null) m_cursor.SetTargetable(false); }
}
