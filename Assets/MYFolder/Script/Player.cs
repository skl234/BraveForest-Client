using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
public enum ePlayerState : byte
{
    Idle,
    Move,
    Attack,
}
public class Player : MonoBehaviour
{
    public float m_attackRange = 2.5f;
    public int m_attackPower = 10;
    public float m_moveSpeed = 5.0f;
    public int m_maxHP = 100;
    public int m_hp = 100;

    protected int m_id;
    protected LayerMask m_groundLayer;
    protected ePlayerState m_prevState;
    protected ePlayerState m_currentState;
    protected Animator m_animator;
    protected TextMeshPro m_positionText;

    void Awake()
    {
        m_groundLayer = LayerMask.GetMask("Ground");
        m_animator = transform.Find("Animation").GetComponent<Animator>();

        m_prevState = ePlayerState.Idle;
        m_currentState = ePlayerState.Idle;
    }
    public void SetID(int _id)
    {
        m_id = _id;
    }
    public void SetPosition(Vector3 _position)
    {
        transform.position = _position;
    }
    public void SetRotation(Quaternion _rotation)
    {
        transform.rotation = _rotation;
    }
    public void SetState(ePlayerState _state)
    {
        m_currentState = _state;
    }
    public void OnHit(int _damage)
    {
        StartCoroutine(OnHitCoroutine(_damage));
    }
    IEnumerator OnHitCoroutine(int _damage)
    {
        yield return new WaitForSeconds(0.3f);

        transform.Find("HitEffect").GetComponent<HitEffect>().PlayHitEffect();
        transform.Find("PlayerUI").GetComponent<PlayerUI>().PlayDamage(_damage);
    }
    public void SetHP(int _hp)
    {
        m_hp = _hp;
    }
    public int GetID()
    {
        return m_id;
    }    
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    public int GetHP()
    {
        return m_hp;
    }
    public int GetMaxHP()
    {
        return m_maxHP;
    }
}
