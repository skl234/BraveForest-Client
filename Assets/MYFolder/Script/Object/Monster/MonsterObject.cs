using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class MonsterObject : MMO.Object
{
    [SerializeField] eMonsterType m_monsterType = eMonsterType.Rat;
    [SerializeField] eMonsterState m_state = eMonsterState.Idle;
    [SerializeField] GameObject[] m_monsterPrefabList = new GameObject[5];
    [SerializeField] RuntimeAnimatorController m_wanderController;
    [SerializeField, Range(0, 1)] float m_attackHitTime = 0.45f;

    GameObject m_monster;
    string m_idleAnimation = "IdleNormal";
    string m_moveAnimation = "WalkFWD";
    string m_deadAnimation = "Die";
    string m_attackAnimation = "Attack01";
    readonly List<Vector3> m_serverPath = new List<Vector3>();
    int m_serverPathIndex;
    float m_serverMoveSpeed;
    bool m_serverMoving;
    bool m_attackPlaying;
    bool m_serverControlled;
    float m_arrivalRotationY;
    eMonsterState m_arrivalState = eMonsterState.Idle;

    protected override void Awake()
    {
        CreateMonster();
        base.Awake();
        if (m_wanderController != null && m_animator != null)
        {
            m_animator.runtimeAnimatorController = m_wanderController;
            m_animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            m_animator.Rebind();
            m_animator.Update(0.0f);
            m_idleAnimation = "Idle";
            m_moveAnimation = "Move";
            m_deadAnimation = "Dead";
            m_attackAnimation = "Attack";
        }
        else if (m_monsterType == eMonsterType.Rat) m_moveAnimation = "Walk";
        ApplyState();
    }

    void CreateMonster()
    {
        int monsterIndex = (int)m_monsterType;
        if (monsterIndex < 0 || monsterIndex >= m_monsterPrefabList.Length) return;
        GameObject prefab = m_monsterPrefabList[monsterIndex];
        if (prefab == null) { Debug.LogError("MonsterObject 몬스터 프리팹이 연결되지 않았습니다: " + m_monsterType); return; }
        m_monster = Instantiate(prefab, transform);
        m_monster.name = m_monsterType.ToString();
        m_monster.transform.localPosition = Vector3.zero;
        m_monster.transform.localRotation = Quaternion.identity;
        MonoBehaviour[] behaviourList = m_monster.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviourList.Length; ++i) behaviourList[i].enabled = false;
    }

    public void InitializeServer()
    {
        m_serverControlled = true;
        // 온라인 몬스터는 서버 경로만 사용. 테스트 Agent/배회가 위치를 만지지 않게 함
        MonsterWander wander = GetComponent<MonsterWander>();
        if (wander != null) wander.enabled = false;
        NavMeshAgent agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.enabled = false;
    }

    public void SetState(eMonsterState _state)
    {
        if (_state != eMonsterState.Move) m_serverMoving = false;
        if (m_state == _state) return;
        m_state = _state;
        m_attackPlaying = false;
        if (m_animator != null) m_animator.speed = 1.0f;
        ApplyState();
    }

    public eMonsterType GetMonsterType() { return m_monsterType; }
    public eMonsterState GetState() { return m_state; }

    void ApplyState()
    {
        if (m_animator != null) m_animator.Play(GetStateAnimation(), 0, 0.0f);
    }

    public string GetStateAnimation()
    {
        if (m_state == eMonsterState.Move) return m_moveAnimation;
        if (m_state == eMonsterState.Dead) return m_deadAnimation;
        if (m_attackPlaying) return m_attackAnimation;
        return m_idleAnimation;
    }

    public void ApplyServerState(Vector3 _position, float _rotationY, byte _state)
    {
        if (!m_serverControlled) InitializeServer();
        if (_state == 5)
        {
            m_serverPath.Clear();
            SetState(eMonsterState.Dead);
            return;
        }
        if (m_state == eMonsterState.Dead)
        {
            // 부활은 새 스폰 위치. 이동 중 보정과 구분
            transform.position = ResolveHeight(_position);
            transform.rotation = Quaternion.Euler(0, _rotationY, 0);
            SetState(eMonsterState.Idle);
        }
        // 이동 중 HP/타겟 갱신으로 지난 좌표를 덮어쓰지 않음. 경로 패킷만 이동을 변경
        if (_state == 1 || _state == 2 || _state == 4) return;
        m_arrivalState = eMonsterState.Idle;
        if (_state == 3) m_arrivalState = eMonsterState.Attack;
        m_arrivalRotationY = _rotationY;
        Vector3 remaining = _position - transform.position;
        remaining.y = 0;
        if (remaining.sqrMagnitude > 0.0004f && m_serverMoveSpeed > 0)
        {
            // 아직 돌지 않은 코너를 남긴 채 정지점까지만 이동
            List<Vector3> path = GetPathToPosition(_position);
            m_serverPath.Clear();
            m_serverPath.AddRange(path);
            m_serverPathIndex = 0;
            SetState(eMonsterState.Move);
            m_serverMoving = true;
        }
        else
        {
            SetState(m_arrivalState);
            transform.rotation = Quaternion.Euler(0, _rotationY, 0);
        }
    }

    public float PlayServerAttack(Transform _target)
    {
        if (m_state == eMonsterState.Dead || m_animator == null) return 0;
        if (_target != null)
        {
            Vector3 direction = _target.position - transform.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
        }
        m_attackPlaying = true;
        m_animator.speed = 1;
        m_animator.Play(m_attackAnimation, 0, 0.0f);
        m_animator.Update(0);
        // 서버 판정은 이미 끝났음. 클라이언트는 공격 모션의 타격 시점에 피격을 보여줌
        AnimatorStateInfo state = m_animator.GetCurrentAnimatorStateInfo(0);
        return state.length * Mathf.Clamp01(m_attackHitTime);
    }

    public void SetServerPath(Vector3 _position, float _rotationY, float _moveSpeed, int _pathIndex, List<Vector3> _path)
    {
        if (m_state == eMonsterState.Dead) return;
        if (!m_serverControlled) InitializeServer();
        m_serverMoveSpeed = Mathf.Max(0, _moveSpeed);
        int index = Mathf.Clamp(_pathIndex, 0, _path.Count);
        List<Vector3> path = new List<Vector3>();
        // 새 경로의 첫 선분을 이미 이동 중이면 뒤의 시작 좌표로 돌아갈 필요 없음
        // 전체 경로에서 가장 가까운 선분을 찾으면 U자 경로의 끝으로 건너뛰게 됨
        bool onFirstSegment = index < _path.Count && IsOnSegment(transform.position, _position, _path[index]);
        if (!onFirstSegment) path.AddRange(GetPathToPosition(_position));
        for (int i = index; i < _path.Count; ++i) path.Add(_path[i]);
        m_serverPath.Clear();
        m_serverPath.AddRange(path);
        m_serverPathIndex = 0;
        m_arrivalState = eMonsterState.Idle;
        m_arrivalRotationY = _rotationY;
        m_serverMoving = m_serverPathIndex < m_serverPath.Count;
        if (m_serverMoving) SetState(eMonsterState.Move);
        else SetState(m_arrivalState);
    }

    List<Vector3> GetPathToPosition(Vector3 _position)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 start = transform.position;
        start.y = 0;
        _position.y = 0;
        if ((start - _position).sqrMagnitude < 0.000001f) return path;
        // 서버가 도착한 위치까지 기존 경로를 순서대로 잘라 사용. 속도를 올리거나 순간이동하지 않음
        for (int i = m_serverPathIndex; i < m_serverPath.Count; ++i)
        {
            Vector3 end = m_serverPath[i];
            if (IsOnSegment(_position, start, end))
            {
                path.Add(_position);
                return path;
            }
            path.Add(end);
            start = end;
        }
        // 새 스폰 이후 첫 경로 등 기존 경로가 없는 경우
        path.Clear();
        path.Add(_position);
        return path;
    }

    bool IsOnSegment(Vector3 _position, Vector3 _start, Vector3 _end)
    {
        _position.y = 0;
        _start.y = 0;
        _end.y = 0;
        Vector3 direction = _end - _start;
        if (direction.sqrMagnitude < 0.000001f) return (_position - _start).sqrMagnitude < 0.000001f;
        float ratio = Vector3.Dot(_position - _start, direction) / direction.sqrMagnitude;
        if (ratio < 0 || ratio > 1) return false;
        return (_position - (_start + direction * ratio)).sqrMagnitude < 0.0025f;
    }

    void Update()
    {
        if (m_state == eMonsterState.Dead)
        {
            if (m_animator != null && m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1)
            {
                m_animator.Play(m_deadAnimation, 0, 1);
                m_animator.Update(0);
                m_animator.speed = 0;
            }
            return;
        }
        if (m_attackPlaying && m_animator != null)
        {
            AnimatorStateInfo state = m_animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(m_attackAnimation) && state.normalizedTime >= 1)
            {
                m_attackPlaying = false;
                ApplyState();
            }
        }
        MoveServerPath(Time.deltaTime);
    }

    void MoveServerPath(float _deltaTime)
    {
        if (!m_serverMoving) return;
        float distanceLeft = m_serverMoveSpeed * _deltaTime;
        while (distanceLeft > 0 && m_serverPathIndex < m_serverPath.Count)
        {
            Vector3 destination = ResolveHeight(m_serverPath[m_serverPathIndex]);
            Vector3 direction = destination - transform.position;
            direction.y = 0;
            float distance = direction.magnitude;
            if (!m_attackPlaying && distance > 0.0001f) transform.rotation = Quaternion.LookRotation(direction);
            if (distance <= distanceLeft)
            {
                transform.position = destination;
                distanceLeft -= distance;
                ++m_serverPathIndex;
                continue;
            }
            transform.position = ResolveHeight(transform.position + direction / distance * distanceLeft);
            distanceLeft = 0;
        }
        if (m_serverPathIndex >= m_serverPath.Count)
        {
            m_serverMoving = false;
            SetState(m_arrivalState);
            if (m_arrivalState == eMonsterState.Attack) transform.rotation = Quaternion.Euler(0, m_arrivalRotationY, 0);
        }
    }

    Vector3 ResolveHeight(Vector3 _position)
    {
        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null) _position.y = terrain.SampleHeight(_position) + terrain.transform.position.y;
        return _position;
    }
}
