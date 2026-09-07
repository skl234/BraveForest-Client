using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public enum eMonsterAIType : ushort
{
    Static = 0, //정지 몬스터
    Patrol,     //배회 몬스터
    Aggressive, //선공 몬스터
}
public enum eMonsterType : ushort  
{
    Ghost = 0,
    Lizard,
    Knight,
    Rat,
    Demon,
}
public enum eMonsterState : ushort
{
    Idle = 0,           //가만히 있는 상태
    Move,               //이동 상태
    Attack,             //공격 상태
    Hit,                //피격상태
    Dead,				//사망 상태
}
public class Monster : MonoBehaviour
{
    protected int m_number;
    protected eMonsterType  m_type;
    protected eMonsterState m_prevState;
    protected eMonsterState m_currentState;
    protected int m_maxHP;
    protected int m_hp;
    protected float m_moveSpeed;
    protected float m_hitboxRadius;
    protected List<Vector3> m_path;

    protected LayerMask m_groundLayer;
    protected Animator m_animator;
    protected GameObject m_lookTarget;

    private void Awake()
    {
        m_path = new List<Vector3>();
        m_groundLayer = LayerMask.GetMask("Ground");
        m_animator = transform.Find("Animation").GetComponent<Animator>();
        m_lookTarget = null;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void Initialize(int _number, eMonsterType _type, eMonsterState _state, int _maxHP, int _hp, 
        float _moveSpeed, float _hitboxRadius, Vector3 _position, Quaternion _rotation, List<Vector3> _path)
    {
        m_number = _number;
        m_type = _type;
        m_prevState = _state;
        m_currentState = _state;
        m_maxHP = _maxHP;
        m_hp = _hp;
        m_moveSpeed = _moveSpeed;
        m_hitboxRadius = _hitboxRadius;
        m_path = _path;

        transform.position = _position;
        transform.rotation = _rotation;
    }
    public int GetNumber()
    {
        return m_number;
    }
    public eMonsterState GetState()
    {
        return m_currentState;
    }
    public int GetMaxHP()
    {
        return m_maxHP;
    }
    public int GetHP()
    {
        return m_hp;
    }
    public void SetHP(int _hp)
    {
        m_hp = _hp;
    }
    public void SetPath(List<Vector3> _path)
    {
        m_path = _path;
    }
    public void SetState(eMonsterState _state)
    {
        m_currentState = _state;
        m_path.Clear();
    }
    public void UpdateTransform()
    {
        if (m_path.Count == 0) return;

        int currentPathIndex = m_path.Count - 1;
        float remainingDistance = Time.deltaTime * m_moveSpeed;
        Vector3 direction = m_path[currentPathIndex] - new Vector3(transform.position.x, 0.0f, transform.position.z);
        Vector3 dirNomalize = direction.normalized;
        Vector3 movement = dirNomalize * remainingDistance;

        //수평 이동
        while (true)
        {
            if (movement.sqrMagnitude < direction.sqrMagnitude)
            {
                transform.position += movement;
                break;
            }

            transform.position = m_path[currentPathIndex];
            m_path.RemoveAt(currentPathIndex);
            if (m_path.Count == 0) break;

            currentPathIndex -= 1;
            remainingDistance -= direction.magnitude;
            direction = m_path[currentPathIndex] - new Vector3(transform.position.x, 0.0f, transform.position.z);
            dirNomalize = direction.normalized;
            movement = dirNomalize * remainingDistance;
        }

        //수직 이동
        Vector3 origin = new Vector3(transform.position.x, transform.position.y + 50f, transform.position.z);
        Ray ray = new Ray(origin, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, m_groundLayer))
        {
            transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
        }

        //회전
        if (dirNomalize == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(dirNomalize);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20f);
    }
    public void SetPosition(Vector3 _position)
    {
        transform.position = _position;
    }
    public void SetRotation(Quaternion _rotation)
    {
        transform.rotation = _rotation;
    }
    public void LookAt(Vector3 _position)
    {
        if (m_currentState == eMonsterState.Attack) return;
        Vector3 dirno = (_position - transform.position).normalized;
        if (dirno == Vector3.zero) return;
        transform.rotation = Quaternion.LookRotation(dirno);
    }
    public void SetLookTarget(GameObject _target)
    {
        m_lookTarget = _target;
    }
    public float GetHorizontalDistance(Vector3 _position)
    {
        Vector2 XZPosition = new Vector2(transform.position.x, transform.position.z);
        Vector2 XZTargetPosition = new Vector2(_position.x, _position.z);

        float distance = Vector2.Distance(XZPosition, XZTargetPosition) - m_hitboxRadius;
        //내 좌표와 타겟간의 xz좌표간의 거리를 구한후, 히트박스의 반지름값을 빼면 히트박스를 포함한 나와 타겟간의 거리

        if (distance < 0) return 0; //0보다 작으면 target의 위치가 내 히트박스 범위안에 있는것
        return distance;
    }
    public Vector3 GetClosestPoint(Vector3 _position)
    {
        //인자로 넣어준 좌표와 가장 가까운 Collider 면의 좌표를 넘겨줌
        return GetComponent<SphereCollider>().ClosestPoint(transform.position);
    }
    public void OnHit(int _damage)
    {
        StartCoroutine(OnHitCoroutine(_damage));
    }
    IEnumerator OnHitCoroutine(int _damage)
    {
        yield return new WaitForSeconds(0.3f);

        transform.Find("HitEffect").GetComponent<HitEffect>().PlayHitEffect();
        transform.Find("MonsterUI").GetComponent<MonsterUI>().PlayDamage(_damage);
    }
}
