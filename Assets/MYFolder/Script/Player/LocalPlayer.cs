using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Net.Sockets;
public class LocalPlayer : Player
{
	Navigation      m_navigation;
	List<Vector3>   m_path = new List<Vector3>();

	NetworkStream   m_networkStream;

	GameObject      m_attackTarget = null;
	float           m_prevAttackTime = 0.0f;

	// Start is called before the first frame update
	void Start()
	{
		m_navigation = new Navigation(GetComponent<UnityEngine.AI.NavMeshAgent>());
		Camera.main.GetComponent<CameraController>().m_target = transform;

		StartCoroutine(SendPositionCoroutine());


	}

	// Update is called once per frame
	void Update()
	{
		OnKeyEvent();
		HandleState();
	}
	public void SetNetworkStream(NetworkStream _networkStream)
	{
		m_networkStream = _networkStream;
	}
	void OnKeyEvent()
	{
		if (MouseController.GetMouseButtonDown(eMouseBtnType.LEFT))
		{
			Ray ray = MouseController.GetScreenPointToRay(Camera.main);
			if (!Physics.Raycast(ray, out RaycastHit hit)) return;

			GameObject targetObject = hit.collider.gameObject;
			if (!targetObject.CompareTag("Monster")) return;
			if (targetObject.GetComponent<Monster>().GetState() == eMonsterState.Dead) return;
			if (targetObject.GetComponent<Monster>().GetHorizontalDistance(transform.position) > m_attackRange) return;
			if (m_currentState == ePlayerState.Attack) return;

			m_attackTarget = targetObject;

			ChangeState(ePlayerState.Attack);
		}
		else if (MouseController.GetMouseButton(eMouseBtnType.Right) && m_currentState != ePlayerState.Attack)
		{
			RaycastHit rayHit;
			Ray mouseRay = MouseController.GetScreenPointToRay(Camera.main);
			if (!Physics.Raycast(mouseRay, out rayHit, Mathf.Infinity, m_groundLayer)) return;

			List<Vector3> newPath = m_navigation.NavigateWithCoolTime(rayHit.point);
			if (newPath.Count == 0) return;

			m_path = newPath;
			SendDestPosition(m_path[m_path.Count - 1]);
		}
	}
	void HandleState()
	{
		switch (m_currentState)
		{
			case ePlayerState.Idle:
				HandleIdle();
				break;

			case ePlayerState.Move:
				HandleMove();
				break;

			case ePlayerState.Attack:
				HandleAttack();
				break;

			default:
				break;
		}
	}
	void HandleIdle()
	{
		if (m_path.Count > 0) ChangeState(ePlayerState.Move);
	}
	void HandleMove()
	{
		if (m_path.Count == 0) ChangeState(ePlayerState.Idle);
		UpdateTransform();
	}
	void HandleAttack()
	{
		RuntimeAnimatorController controller = m_animator.runtimeAnimatorController;

		foreach (AnimationClip clip in controller.animationClips)
		{
			if (clip.name != "NormalAttack01_SwordShield") continue;

			if(Time.time - m_prevAttackTime >= clip.length)
			{
				m_attackTarget = null;
				ChangeState(ePlayerState.Idle);
			}
			return;
		}
	}
	void ChangeState(ePlayerState _state)
	{
		m_prevState = m_currentState;
		m_currentState = _state;

		if (m_prevState == ePlayerState.Move) m_path.Clear();

		switch(m_currentState)
		{
			case ePlayerState.Idle:
				StartIdle();
				break;

			case ePlayerState.Move:
				StartMove();
				break;

			case ePlayerState.Attack:
				StartAttack();
				break;

			default:
				break;
		}
	}
	void StartIdle()
	{
		m_animator.SetBool("Move", false);
		SendState();
	}
	void StartMove()
	{
		if (m_path.Count < 1) return;

		m_animator.SetBool("Move", true);
		SendState();
		SendDestPosition(m_path[m_path.Count - 1]);
	}
	void StartAttack()
	{
		Vector3 lookDirNomalize = (m_attackTarget.transform.position - transform.position).normalized;
		if (lookDirNomalize != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDirNomalize);

		m_animator.SetTrigger("Attack");
		m_prevAttackTime = Time.time;
		SendState();
		SendAttack(m_attackTarget.GetComponent<Monster>().GetNumber());
	}
	void UpdateTransform()
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

			SendDestPosition(m_path[currentPathIndex]);
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

		if (m_path.Count == 0) Debug.Log("path 0개");
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
	void SendDestPosition(Vector3 _position)
	{
		MemoryStream memoryStream = new MemoryStream(128);
		memoryStream.Write(BitConverter.GetBytes((ushort)28));
		memoryStream.Write(BitConverter.GetBytes((ushort)PacketType.C2S_DestPosition));
		memoryStream.Write(BitConverter.GetBytes(_position.x));
		memoryStream.Write(BitConverter.GetBytes(0f));
		memoryStream.Write(BitConverter.GetBytes(_position.z));
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
		memoryStream.Write(BitConverter.GetBytes((ushort)28));
		memoryStream.Write(BitConverter.GetBytes((ushort)PacketType.C2S_Attack));
		memoryStream.Write(BitConverter.GetBytes(transform.position.x));
		memoryStream.Write(BitConverter.GetBytes(transform.position.y));
		memoryStream.Write(BitConverter.GetBytes(transform.position.z));
		memoryStream.Write(BitConverter.GetBytes(m_attackRange));
		memoryStream.Write(BitConverter.GetBytes(m_attackPower));
		memoryStream.Write(BitConverter.GetBytes(_monsterNum));

		m_networkStream.Write(memoryStream.GetBuffer(), 0, 28);
	}
}
