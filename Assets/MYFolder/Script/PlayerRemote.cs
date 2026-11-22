using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class PlayerRemote : Player
{
	Vector3 m_destPosition; //목적지
	Vector3 m_direction;    //정규화된 방향
	void Start()
	{

	}
	void Update()
	{
		if (m_prevState != m_currentState)
		{
			m_animator.SetInteger("PlayerAniState", (int)m_currentState);
		}
		m_prevState = m_currentState;

		UpdateTransform();
	}
	public void SetDestPosition(Vector3 _position)
	{
		//목적지, 정규화된 방향 세팅
		m_destPosition = _position;
		m_direction = m_destPosition - new Vector3(transform.position.x, 0f, transform.position.z);
		m_direction.Normalize();
	}
	public void SetDestPosition(Vector3 _destPosition, Vector3 _currentPosition)
	{
		//목적지와 현재위치, 정규화된 방향 세팅
		m_destPosition = _destPosition;
		if(Vector3.Distance(_currentPosition, transform.position) >= 0.2f) transform.position = _currentPosition;
		m_direction = m_destPosition - new Vector3(transform.position.x, 0f, transform.position.z);
		m_direction.Normalize();
	}
	private void UpdateTransform()
	{
		if (m_currentState == ePlayerState.Attack) return;

		Vector3 movement = m_direction * Time.deltaTime * m_moveSpeed;  //움직일 방향

		Vector3 currentVerticalPosition = new Vector3(transform.position.x, 0f, transform.position.z); //현재 평면좌표

		if (movement.sqrMagnitude >= (m_destPosition - currentVerticalPosition).sqrMagnitude) //만약 움직일 힘이 가야할 거리가 현재위치에서 목적지까지의 거리와 같거나 길다면
		{
			SetPosition(m_destPosition);    //현재 좌표를 목적지 좌표로 세팅
		}
		else //움직일 힘이 가야할 거리가 목적지까지의 거리보다 짧다면
		{
			UpdateVerticalPosition(movement); //이동
			UpdateRotation(m_direction);      //머리도 회전
		}
		UpdateHorizontalPosition(); //높이 조정은 조건에 관계없이
	}
	private void UpdateVerticalPosition(Vector3 _movement)
	{
		transform.position += _movement; //수평이동
	}
	private void UpdateHorizontalPosition()
	{
		//플레이어 오브젝트의 y+50f 높이에서
		Vector3 origin = new Vector3(transform.position.x, transform.position.y + 50f, transform.position.z);
		Ray ray = new Ray(origin, Vector3.down);

		//아래로 200f까지 빔을 쏜다
		//그 빔에 GroundLayer 가 있다면 그곳이 y축좌표이다.
		//복층 맵이라면 빔을 쏘는 위치와 기타등등을 고려해봐야겠지만
		//마우스로 이동하는 쿼터뷰게임에서 복층이 있는건 일단 내가 본적이없다.
		if (Physics.Raycast(ray, out RaycastHit hit, 200f, m_groundLayer)) 
		{
			transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
		}
	}
	private void UpdateRotation(Vector3 _direction)
	{
		if (_direction == Vector3.zero) return;

		Quaternion targetRotation = Quaternion.LookRotation(_direction);
		transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
	}
}
