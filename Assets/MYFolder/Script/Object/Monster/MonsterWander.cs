using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MonsterObject), typeof(NavMeshAgent))]
public class MonsterWander : MonoBehaviour
{
    [SerializeField] float m_moveSpeed = 6.0f;
    [SerializeField] float m_wanderRadius = 4.0f;
    [SerializeField] float m_idleDuration = 1.0f;
    [SerializeField] float m_rotationSpeed = 12.0f;

    MonsterObject m_monster;
    MonsterHitbox m_hitbox;
    NavMeshAgent m_agent;
    NavMeshPath m_path;
    Vector3 m_origin;
    int m_cornerIndex;
    float m_idleUntil;
    bool m_wandering;

    void Awake()
    {
        m_monster = GetComponent<MonsterObject>();
        m_hitbox = GetComponentInChildren<MonsterHitbox>(true);
        m_agent = GetComponent<NavMeshAgent>();
        m_agent.updatePosition = false;
        m_agent.updateRotation = false;
        m_path = new NavMeshPath();
        m_origin = transform.position;
    }

    void Start()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3.0f, m_agent.areaMask))
        {
            m_agent.Warp(hit.position);
            transform.position = hit.position;
            m_agent.nextPosition = hit.position;
            m_origin = hit.position;
        }
        SetWandering(false);
    }

    void Update()
    {
        if (!m_wandering || m_hitbox == null || !m_hitbox.IsAlive())
        {
            StopMove();
            return;
        }
        if (m_cornerIndex < m_path.corners.Length)
        {
            Move();
            return;
        }
        m_monster.SetState(eMonsterState.Idle);
        if (Time.time < m_idleUntil) return;
        FindDestination();
    }

    public void SetWandering(bool _wandering)
    {
        m_wandering = _wandering;
        StopMove();
        m_idleUntil = Time.time + (_wandering ? m_idleDuration : 0.0f);
    }

    public bool IsWandering() { return m_wandering; }
    public float GetMoveSpeed() { return m_moveSpeed; }

    public void ResetOrigin(Vector3 _position)
    {
        if (m_agent.isOnNavMesh) m_agent.Warp(_position);
        transform.position = _position;
        m_agent.nextPosition = _position;
        m_origin = _position;
        StopMove();
        m_idleUntil = Time.time + m_idleDuration;
    }

    void FindDestination()
    {
        for (int i = 0; i < 12; ++i)
        {
            Vector2 random = Random.insideUnitCircle * m_wanderRadius;
            Vector3 requested = m_origin + new Vector3(random.x, 0.0f, random.y);
            // 장애물이 섞인 공터에서도 중심 주변의 가장 가까운 이동면을 선택합니다.
            if (!NavMesh.SamplePosition(requested, out NavMeshHit hit, m_wanderRadius, m_agent.areaMask)) continue;
            if ((hit.position - m_origin).sqrMagnitude > m_wanderRadius * m_wanderRadius) continue;
            Vector3 delta = hit.position - transform.position; delta.y = 0.0f;
            if (delta.sqrMagnitude < 0.25f) continue;
            m_agent.nextPosition = transform.position;
            if (!m_agent.CalculatePath(hit.position, m_path) || m_path.status != NavMeshPathStatus.PathComplete || m_path.corners.Length < 2) continue;
            m_cornerIndex = 1;
            m_monster.SetState(eMonsterState.Move);
            return;
        }
        m_idleUntil = Time.time + m_idleDuration;
    }

    void Move()
    {
        float distanceLeft = m_moveSpeed * Time.deltaTime;
        while (distanceLeft > 0.0f && m_cornerIndex < m_path.corners.Length)
        {
            Vector3 destination = m_path.corners[m_cornerIndex];
            Vector3 direction = destination - transform.position;
            float distance = direction.magnitude;
            if (distance <= distanceLeft)
            {
                transform.position = destination;
                distanceLeft -= distance;
                ++m_cornerIndex;
                continue;
            }
            direction /= distance;
            transform.position += direction * distanceLeft;
            direction.y = 0.0f;
            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * m_rotationSpeed);
            distanceLeft = 0.0f;
        }
        m_agent.nextPosition = transform.position;
        if (m_cornerIndex < m_path.corners.Length) return;
        m_monster.SetState(eMonsterState.Idle);
        m_idleUntil = Time.time + m_idleDuration;
    }

    void StopMove()
    {
        if (m_path != null) m_path.ClearCorners();
        m_cornerIndex = 0;
        if (m_monster != null && (m_hitbox == null || m_hitbox.IsAlive())) m_monster.SetState(eMonsterState.Idle);
        if (m_agent != null && m_agent.isOnNavMesh) m_agent.nextPosition = transform.position;
    }
}
