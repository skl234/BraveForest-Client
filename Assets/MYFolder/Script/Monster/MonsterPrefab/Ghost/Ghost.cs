using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : Monster
{
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (m_path.Count == 0) m_animator.SetBool("Move", false);
        else m_animator.SetBool("Move", true);

        if (m_prevState != m_currentState)
        {
            //if (m_currentState == eMonsterState.Move) m_animator.Play("WalkFWD");
            if (m_currentState == eMonsterState.Hit) OnHitAnimation();
            if (m_currentState == eMonsterState.Attack) m_animator.SetTrigger("Attack");
            if (m_currentState == eMonsterState.Dead) {m_animator.Play("Die"); m_lookTarget = null; }
            if (m_prevState == eMonsterState.Dead && m_currentState == eMonsterState.Idle) m_animator.CrossFade("IdleNormal", 0.25f);
        }

        if (m_currentState == eMonsterState.Move) UpdateTransform();
        if (m_currentState == eMonsterState.Idle && m_lookTarget)
        {
            Vector3 dirno = (m_lookTarget.transform.position - transform.position).normalized;
            if (dirno == Vector3.zero) return;
            Quaternion targetRotation = Quaternion.LookRotation(dirno);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 20.0f);
        }

        m_prevState = m_currentState;   
    }
    public void OnHitAnimation()
    {
        StartCoroutine(OnHitnimationCoroutine());
    }
    IEnumerator OnHitnimationCoroutine()
    {
        yield return new WaitForSeconds(0.3f);
        m_animator.SetTrigger("Hit");
    }
}

//sector 넘어가면 target 정보가 초기화 되면서 날아가게 되네....
//흠...생각해보니 server에서 target이 변경되는 때는? hit 되었을때지
//그렇다면 걍 클라이언트에서도 hit되면 걜 바라보게 만들까?
