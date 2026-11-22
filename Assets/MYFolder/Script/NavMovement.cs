using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class NavMovement
{
	NavMeshAgent m_agent;
	LayerMask m_groundLayer;
	float        m_lastUpdateTime;
	float        m_updateInterval;
	bool         m_isUpdateDest;
	bool         m_isUpdateCurrentCorner;
	
	Vector3 m_direction;    //정규화된 방향
	Vector3[] m_corners;
	int m_prevTargetCornerIndex;
	int m_currentTargetCornerIndex;

	public NavMovement(NavMeshAgent _agent, LayerMask _groundLayer, float _speed)
	{
		m_agent = _agent;
		m_agent.speed = _speed;
		m_agent.updatePosition = false;
		m_agent.updateRotation = false;
		m_agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;    //에이전트간의 회피계산 비활성화

		m_groundLayer = _groundLayer;

		m_lastUpdateTime = 0f;
		m_updateInterval = 0.2f;
		m_isUpdateDest = false;
		m_isUpdateCurrentCorner = false;

		m_corners = null;
		m_prevTargetCornerIndex = 0;
		m_currentTargetCornerIndex = 0;
	}
	public void Update(Camera _camera, LayerMask _groundLayer)
	{
		UpdateDestPosition(_camera, _groundLayer);
		UpdateCornersArray();
		UpdateTransform();
	}
	public bool IsUpdateCorenr()
	{
		return m_isUpdateCurrentCorner;
	}
	public Vector3 GetCurrentTargetCorner()
	{
		if (m_currentTargetCornerIndex == 0) return m_agent.transform.position;
		return m_corners[m_currentTargetCornerIndex];
	}
	public bool IsMoving()
	{
		if (m_currentTargetCornerIndex == 0) return false;
		return true;
	}
	private void UpdateDestPosition(Camera _camera, LayerMask _groundLayer)
	{
		if (!MouseController.GetMouseButton(eMouseBtnType.Right)) return;

		float currentTime = Time.time;
		if (currentTime - m_lastUpdateTime < m_updateInterval) return;

		RaycastHit rayHit;
		Ray mouseRay = MouseController.GetScreenPointToRay(_camera);
		if (!Physics.Raycast(mouseRay, out rayHit, Mathf.Infinity, _groundLayer)) return;

		Vector3 destPosition = new Vector3(rayHit.point.x, rayHit.point.y, rayHit.point.z);
		m_agent.SetDestination(destPosition);

		m_lastUpdateTime = currentTime;
		m_isUpdateDest = true;
		//Debug.Log("목적지 설정" + destPosition);
	}
	public void SetDestPosition(Vector3 _destPosition)
	{
		float currentTime = Time.time;
		if (currentTime - m_lastUpdateTime < m_updateInterval) return;
		m_agent.SetDestination(_destPosition);

		m_lastUpdateTime = currentTime;
		m_isUpdateDest = true;
	}
	private void UpdateCornersArray()
	{
		if (!m_isUpdateDest || m_agent.pathPending || m_agent.path.corners.Length < 2) return;
		//Debug.Log("UpdateCorners");

		m_isUpdateDest = false;
		m_corners = m_agent.path.corners;
		for(int i = 0; i < m_corners.Length; ++i)
		{
			m_corners[i] = new Vector3(m_corners[i].x, 0f, m_corners[i].z);
			//Debug.Log(m_corners[i]);
		}
		m_prevTargetCornerIndex = 0;
		m_currentTargetCornerIndex = 1;
		m_direction = m_corners[m_currentTargetCornerIndex] - new Vector3(m_agent.transform.position.x, 0f, m_agent.transform.position.z);
		m_direction.Normalize();
	}
	private void UpdateTransform()
	{
		if (m_currentTargetCornerIndex == 0) return;
		if (m_prevTargetCornerIndex != m_currentTargetCornerIndex) m_isUpdateCurrentCorner = true;
		else m_isUpdateCurrentCorner = false;
		m_prevTargetCornerIndex = m_currentTargetCornerIndex;

		Vector3 movement = m_direction * Time.deltaTime * m_agent.speed;  //움직일 방향

		Vector3 currentVerticalPosition = new Vector3(m_agent.transform.position.x, 0f, m_agent.transform.position.z); //현재 평면좌표

		if (movement.sqrMagnitude >= (m_corners[m_currentTargetCornerIndex] - currentVerticalPosition).sqrMagnitude) //만약 움직일 힘이 가야할 거리가 현재위치에서 목적지까지의 거리와 같거나 길다면
		{
			m_agent.transform.position = m_corners[m_currentTargetCornerIndex];
			if (++m_currentTargetCornerIndex >= m_corners.Length) CleanCorners();   //마지막 코너에 도착
			else //그 외
			{
				m_direction = m_corners[m_currentTargetCornerIndex] - new Vector3(m_agent.transform.position.x, 0f, m_agent.transform.position.z);
				m_direction.Normalize();
			}
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
		m_agent.transform.position += _movement; //수평이동
	}
	private void UpdateHorizontalPosition()
	{
		Vector3 origin = new Vector3(m_agent.transform.position.x, m_agent.transform.position.y + 1f, m_agent.transform.position.z);
		Ray ray = new Ray(origin, Vector3.down);

		if (Physics.Raycast(ray, out RaycastHit hit, 100f, m_groundLayer))
		{
			m_agent.transform.position = new Vector3(m_agent.transform.position.x, hit.point.y, m_agent.transform.position.z);
		}
	}
	private void UpdateRotation(Vector3 _direction)
	{
		if (_direction == Vector3.zero) return;

		Quaternion targetRotation = Quaternion.LookRotation(_direction);
		m_agent.transform.rotation = Quaternion.Slerp(m_agent.transform.rotation, targetRotation, Time.deltaTime * 10f);
	}
	public void CleanCorners()
	{
		m_isUpdateCurrentCorner = false;
		m_corners = null;
		m_prevTargetCornerIndex = 0;
		m_currentTargetCornerIndex = 0;
	}
}
