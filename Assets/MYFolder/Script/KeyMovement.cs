using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//마우스(회전)과 키보드(방향)을 둘다 이용한 조작...
//기본적으로 카메라랑 연동되고 카메라의 바라보는 위치가 w전진 방향
//키보드 입력을 이용한 방향 감지는 키입력이 변환될때마다
//키보드 입력도 입력제한을 두는게 좋을려나? 0.1초 둬보자.....이거 생각보다 너무 불편한데...? 일단 다시 빼자
//마우스회전으로 인한 회전은 0.2초 딜레이
//단 마우스 회전으로인한 0.2초 딜레이는 존재하지만 사용자가 볼때 실시간으로 회전하는것처럼 보이게

public class KeyMovement
{
    public Transform m_cameraTransform; 
    public float m_moveSpeed;

    private Vector3 m_prevVisualMovement;      // 마지막으로 실제 움직임이 바뀌었을때 시점의 시각적움직임
    private Vector3 m_visualMovement;          // 시각적 움직임(회전방향)
    private Vector3 m_actualMovement;          // 실제 움직임

    private float m_lastActMovementUpdateTime;
    private float m_actMovementUpdateInterval;

    private Vector3 m_inputMovement;
    private float m_lastInputMovementUpdateTime;
    private float m_inputMovementUpdateInterval;

    public KeyMovement(Transform _cameraTransform, float _speed)
    {
        m_cameraTransform = _cameraTransform;
        m_moveSpeed = _speed;

        m_actMovementUpdateInterval = 0.2f;
        m_inputMovementUpdateInterval = 0.1f;
    }
    public Vector3 GetActualMovement()
    {
        return m_actualMovement;
    }
    public bool IsMoving()
    {
        if (m_actualMovement == Vector3.zero) return false;
        return true;
    }
    public bool UpdateMovement()
    {
        Vector3 prevInputMovement = m_inputMovement;
        Vector3 prevActualMovement = m_actualMovement;

        UpdateInputMovement();
        UpdateVisualMovement();

        if (prevInputMovement != m_inputMovement) //키입력 감지, 즉시 방향 변경
        {
            m_prevVisualMovement = m_visualMovement;
            m_actualMovement = m_visualMovement;
            m_lastActMovementUpdateTime = Time.time;
            Debug.Log("키로인한 방향변경");
        }
        else if (Time.time - m_lastActMovementUpdateTime > m_actMovementUpdateInterval // 0.2초 확인
            && m_prevVisualMovement != m_visualMovement)                       // 시각적 방향 변경 확인
        {
            m_prevVisualMovement = m_visualMovement;
            m_actualMovement = m_visualMovement;
            m_lastActMovementUpdateTime = Time.time;
            Debug.Log("마우스 회전으로인한 방향변경");
        }

        if (prevActualMovement != m_actualMovement) return true;
        return false;
    }
    public void Move(Transform _transform, LayerMask _groundLayer)
    {
        Vector3 moveDirection = m_actualMovement;
        RaycastHit rayHit;
        if (Physics.Raycast(_transform.position, Vector3.down, out rayHit, Mathf.Infinity, _groundLayer))
        {
            moveDirection = Vector3.ProjectOnPlane(m_actualMovement, rayHit.normal).normalized;
        }
        _transform.position += moveDirection * Time.deltaTime * m_moveSpeed;

        if (m_visualMovement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(m_visualMovement);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, Time.deltaTime * 20f);
        }
    }
    void UpdateInputMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 prevInputMovement = m_inputMovement;
        Vector3 newInputMovement = new Vector3(horizontal, 0, vertical).normalized;

        /*if(prevInputMovement == Vector3.zero && newInputMovement != prevInputMovement)
        {
            m_lastInputMovementUpdateTime = Time.time;
            m_inputMovement = newInputMovement;
        }
        else if(newInputMovement == Vector3.zero && newInputMovement != prevInputMovement)
        {
            m_lastInputMovementUpdateTime = Time.time;
            m_inputMovement = newInputMovement;
        }
        else if (Time.time - m_lastInputMovementUpdateTime > m_inputMovementUpdateInterval)*/
        {
            m_lastInputMovementUpdateTime = Time.time;
            m_inputMovement = newInputMovement;
        }
    }
    void UpdateVisualMovement()
    {
        Vector3 camForward = m_cameraTransform.forward;
        camForward.y = 0;
        camForward.Normalize();

        Vector3 camRight = m_cameraTransform.right;
        camRight.y = 0;
        camRight.Normalize();

        m_visualMovement = (camForward * m_inputMovement.z + camRight * m_inputMovement.x).normalized;
    }
}
