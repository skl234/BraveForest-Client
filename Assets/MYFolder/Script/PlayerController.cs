using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using System.IO;
using System.Net.Sockets;
public class PlayerController : Player
{
	private Camera m_camera;
	protected NavMovement m_navMovement;
	private NetworkStream m_networkStream;

	private GameObject m_targetMonster = null;
	private bool m_targetMonsterInAttackRange = false;
	public float m_hitTriggerPoint = 0.5f;
	private bool m_isSendAttack;

	void Start()
	{
		m_navMovement = new NavMovement(GetComponent<UnityEngine.AI.NavMeshAgent>(), m_groundLayer, m_moveSpeed);

		m_camera = Camera.main;
		m_camera.GetComponent<CameraController>().m_target = transform;
		StartCoroutine(SendPositionCoroutine());
		m_isSendAttack = false;
	}
	void Update()
	{
		UpdateTartgetMonster();
		if (m_currentState != ePlayerState.Attack) m_navMovement.Update(m_camera, m_groundLayer);
		if (m_navMovement.IsUpdateCorenr()) SendDestPosition();
		UpdateState();
	}
	public void SetDestPosition(Vector3 _position)
	{
		m_navMovement.SetDestPosition(_position);
	}
	public void SetNetworkStream(NetworkStream _networkStream)
	{
		m_networkStream = _networkStream;
	}
	void UpdateState()
	{
		m_prevState = m_currentState;
		if(m_currentState == ePlayerState.Attack)   //유저가 공격중이었다면
		{
			AnimatorStateInfo stateInfo = m_animator.GetCurrentAnimatorStateInfo(0); // 0은 Base Layer
			if (stateInfo.IsName("NormalAttack01_SwordShield") && stateInfo.normalizedTime >= 1f && !m_animator.IsInTransition(0))
			{
				m_isSendAttack = false;
				m_currentState = ePlayerState.Idle;
			}
			else if(stateInfo.IsName("NormalAttack01_SwordShield") && stateInfo.normalizedTime >= 0.3f && !m_animator.IsInTransition(0))
			{
				if(m_isSendAttack == false)
				{
					m_isSendAttack = true;
					m_targetMonster.GetComponent<Monster>().OnHit(5);
					SendAttack(m_targetMonster.GetComponent<Monster>().GetNumber());
				}
			}
		}
		else if (m_currentState != ePlayerState.Attack && m_targetMonster && m_targetMonsterInAttackRange) //타겟 설정된 몬스터가 있고, 공격범위 안에 있다면
		{
			Debug.Log(m_currentState);
			Debug.Log("공격 시작");
			m_currentState = ePlayerState.Attack;
			m_targetMonster.GetComponent<Monster>().LookAt(transform.position);
			m_targetMonster.GetComponent<Monster>().SetLookTarget(gameObject);
		}
		else if (m_navMovement.IsMoving()) m_currentState = ePlayerState.Move;
		else m_currentState = ePlayerState.Idle;

		if (m_prevState != m_currentState)
		{
			m_animator.SetInteger("PlayerAniState", (int)m_currentState);
			SendState();
		}
	}
	void UpdateTartgetMonster()
	{
		if(MouseController.GetMouseButtonDown(eMouseBtnType.LEFT))
		{
			Ray ray = m_camera.ScreenPointToRay(MouseController.GetMousePosition());
			if (Physics.Raycast(ray, out RaycastHit hit))
			{
				GameObject targetObject = hit.collider.gameObject;
				if (targetObject.CompareTag("Monster"))
				{
					if (m_targetMonster != targetObject)
					{
						m_targetMonster = targetObject;
						m_targetMonsterInAttackRange = false;
						Debug.Log("몬스터 타겟팅!");
						Debug.Log("몬스터 번호 : " + m_targetMonster.GetComponent<Monster>().GetNumber());
					}
				}
			}
		}
		if(MouseController.GetMouseButtonDown(eMouseBtnType.Right))
		{
			m_targetMonster = null;
			m_targetMonsterInAttackRange = false;
			Debug.Log("몬스터 타겟팅 해제!");
		}
		if(m_targetMonster)
		{
			if (m_currentState == ePlayerState.Attack) return;

			Vector3 closestPoint = m_targetMonster.GetComponent<Monster>().GetClosestPoint(transform.position);
			float distance = m_targetMonster.GetComponent<Monster>().GetHorizontalDistance(transform.position);

			if (distance <= m_attackRange)
			{ 
				m_navMovement.CleanCorners();
				m_targetMonsterInAttackRange = true;
				transform.rotation = Quaternion.LookRotation(
					(new Vector3(m_targetMonster.transform.position.x, 0, m_targetMonster.transform.position.z) -
					new Vector3(transform.position.x, 0, transform.position.z)).normalized);
			}
			else 
			{ 
				m_navMovement.SetDestPosition(closestPoint);
				m_targetMonsterInAttackRange = false;
			}
		}
	}

	void SendDestPosition()
	{
		Vector3 destPosition = m_navMovement.GetCurrentTargetCorner();

		MemoryStream memoryStream = new MemoryStream(128);
		memoryStream.Write(BitConverter.GetBytes((ushort)28));
		memoryStream.Write(BitConverter.GetBytes((ushort)PacketType.C2S_DestPosition));
		memoryStream.Write(BitConverter.GetBytes(destPosition.x));
		memoryStream.Write(BitConverter.GetBytes(0f));
		memoryStream.Write(BitConverter.GetBytes(destPosition.z));
		memoryStream.Write(BitConverter.GetBytes(transform.position.x));
		memoryStream.Write(BitConverter.GetBytes(transform.position.y));
		memoryStream.Write(BitConverter.GetBytes(transform.position.z));

		m_networkStream.Write(memoryStream.GetBuffer(), 0, 28);
	}
	void SendState()
	{
		MemoryStream memoryStream = new MemoryStream(128);
		memoryStream.Write(BitConverter.GetBytes((ushort)33));
		memoryStream.Write(BitConverter.GetBytes((ushort)PacketType.C2S_UserState));
		memoryStream.Write(BitConverter.GetBytes((byte)m_currentState), 0, 1);
		memoryStream.Write(BitConverter.GetBytes(transform.position.x));
		memoryStream.Write(BitConverter.GetBytes(transform.position.y));
		memoryStream.Write(BitConverter.GetBytes(transform.position.z));
		memoryStream.Write(BitConverter.GetBytes(transform.rotation.x));
		memoryStream.Write(BitConverter.GetBytes(transform.rotation.y));
		memoryStream.Write(BitConverter.GetBytes(transform.rotation.z));
		memoryStream.Write(BitConverter.GetBytes(transform.rotation.w));
		m_networkStream.Write(memoryStream.GetBuffer(), 0, 33);
	}
	void SendAttack(int _monsterNum)
	{
		MemoryStream memoryStream = new MemoryStream(128);
		memoryStream.Write(BitConverter.GetBytes((ushort)24));
		memoryStream.Write(BitConverter.GetBytes((ushort)PacketType.C2S_Attack));
		memoryStream.Write(BitConverter.GetBytes(transform.position.x));
		memoryStream.Write(BitConverter.GetBytes(transform.position.y));
		memoryStream.Write(BitConverter.GetBytes(transform.position.z));
		memoryStream.Write(BitConverter.GetBytes(m_attackRange));
		memoryStream.Write(BitConverter.GetBytes(_monsterNum));

		m_networkStream.Write(memoryStream.GetBuffer(), 0, 24);
	}
	IEnumerator SendPositionCoroutine()
	{
		MemoryStream memoryStream = new MemoryStream(128);
		while (true)
		{
			yield return new WaitForSeconds(0.2f);

			memoryStream.Position = 0;
			memoryStream.Write(BitConverter.GetBytes((ushort)32));
			memoryStream.Write(BitConverter.GetBytes((ushort)PacketType.C2S_TransForm));
			memoryStream.Write(BitConverter.GetBytes(transform.position.x));
			memoryStream.Write(BitConverter.GetBytes(transform.position.y));
			memoryStream.Write(BitConverter.GetBytes(transform.position.z));
			memoryStream.Write(BitConverter.GetBytes(transform.rotation.x));
			memoryStream.Write(BitConverter.GetBytes(transform.rotation.y));
			memoryStream.Write(BitConverter.GetBytes(transform.rotation.z));
			memoryStream.Write(BitConverter.GetBytes(transform.rotation.w));
			m_networkStream.Write(memoryStream.GetBuffer(), 0, 32);
		}
	}
}
