using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LocalPlayerObject : PlayerObject
{
	[SerializeField] float m_moveSpeed = 5.0f;
	[SerializeField] float m_rotationSpeed = 20.0f;
	[SerializeField] LayerMask m_groundLayer;

	ObjectNavigation m_navigation;
	NavMeshAgent m_agent;
	PlayerCombat m_combat;
	WorldPointerInput m_pointerInput;
	List<Vector3> m_path = new List<Vector3>();

	protected override void Awake()
	{
		base.Awake();

		m_agent = GetComponent<NavMeshAgent>();
		m_combat = GetComponent<PlayerCombat>();
		m_pointerInput = GetComponent<WorldPointerInput>();
		if (m_agent == null)
		{
			Debug.LogError("LocalPlayerObject에 NavMeshAgent가 없습니다.");
			return;
		}

		m_agent.updatePosition = false;
		m_agent.updateRotation = false;
		m_navigation = new ObjectNavigation(m_agent);
	}

	void Start()
	{
		if (m_state == ePlayerObjectState.Dead) return;
		// 외형 프리팹의 콜라이더가 마우스 판정을 가로채지 않게 합니다.
		Collider[] colliders = GetComponentsInChildren<Collider>(true);
		for (int i = 0; i < colliders.Length; ++i)
			if (colliders[i].gameObject != gameObject) colliders[i].enabled = false;

		if (m_agent != null && NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
		{
			m_agent.Warp(hit.position);
			transform.position = hit.position;
			m_agent.nextPosition = transform.position;
		}

		SetCameraTarget();
	}

	void Update()
	{
		if (m_state == ePlayerObjectState.Dead) { MaintainStateAnimation(); return; }
		if (ZoneSceneNetwork.Instance != null && ZoneSceneNetwork.Instance.IsWaiting()) return;
		SetCameraTarget();
		if (m_pointerInput != null) m_pointerInput.Tick();
		if (m_combat != null) m_combat.Tick();
		OnMouseEvent();
		UpdateMove();
		MaintainStateAnimation();
	}

	void SetCameraTarget()
	{
		Camera mainCamera = Camera.main;
		if (mainCamera == null) return;

		CameraController cameraController = mainCamera.GetComponent<CameraController>();
		if (cameraController != null && cameraController.m_target != transform)
			cameraController.m_target = transform;
	}

	void OnMouseEvent()
	{
		if (m_combat != null) return;
		if (!MouseController.GetMouseButtonDown(eMouseBtnType.Right)) return;

		Camera mainCamera = Camera.main;
		if (mainCamera == null || m_navigation == null) return;

		Ray mouseRay = MouseController.GetScreenPointToRay(mainCamera);
		if (!Physics.Raycast(mouseRay, out RaycastHit hit, Mathf.Infinity, m_groundLayer)) return;

		List<Vector3> path = m_navigation.Navigate(hit.point);
		if (path.Count == 0) return;

		m_path = path;
		SetState(ePlayerObjectState.Move);
	}

	void UpdateMove()
	{
		if (m_combat != null && m_combat.IsAttacking()) return;
		if (m_path.Count == 0)
		{
			SetState(ePlayerObjectState.Idle);
			return;
		}

		float remainingDistance = m_moveSpeed * Time.deltaTime;
		while (remainingDistance > 0.0f && m_path.Count > 0)
		{
			int pathIndex = m_path.Count - 1;
			Vector3 destination = m_path[pathIndex];
			Vector3 currentPosition = new Vector3(transform.position.x, 0.0f, transform.position.z);
			Vector3 direction = destination - currentPosition;
			direction.y = 0.0f;

			float distance = direction.magnitude;
			if (distance <= remainingDistance)
			{
				transform.position = new Vector3(destination.x, transform.position.y, destination.z);
				m_path.RemoveAt(pathIndex);
				remainingDistance -= distance;
				continue;
			}

			Vector3 normalizedDirection = direction.normalized;
			transform.position += normalizedDirection * remainingDistance;
			UpdateRotation(normalizedDirection);
			remainingDistance = 0.0f;
		}

		UpdateHeight();
		if (m_agent != null) m_agent.nextPosition = transform.position;
		if (m_path.Count == 0) SetState(ePlayerObjectState.Idle);
		if (ZoneSceneNetwork.Instance != null) ZoneSceneNetwork.Instance.NotifyMoveChanged();
	}

	void UpdateHeight()
	{
		Vector3 origin = transform.position + Vector3.up * 50.0f;
		if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 200.0f, m_groundLayer)) return;

		transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
	}

	void UpdateRotation(Vector3 _direction)
	{
		if (_direction == Vector3.zero) return;

		Quaternion targetRotation = Quaternion.LookRotation(_direction);
		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			targetRotation,
			Time.deltaTime * m_rotationSpeed);
	}

	public List<Vector3> GetPath()
	{
		return m_path;
	}

	public bool MoveTo(Vector3 _destination)
	{
		if (m_state == ePlayerObjectState.Dead) return false;
		if (m_navigation == null) return false;
		if (m_agent != null && m_agent.isOnNavMesh) m_agent.nextPosition = transform.position;
		List<Vector3> path = m_navigation.Navigate(_destination);
		if (path.Count == 0) return false;
		m_path = path;
		SetState(ePlayerObjectState.Move);
		if (ZoneSceneNetwork.Instance != null) ZoneSceneNetwork.Instance.NotifyMoveChanged();
		return true;
	}

	public void StopMove()
	{
		m_path.Clear();
		SetState(ePlayerObjectState.Idle);
		if (ZoneSceneNetwork.Instance != null) ZoneSceneNetwork.Instance.NotifyMoveChanged();
	}

	public float GetHitboxRadius()
	{
		CapsuleCollider hitbox = GetComponent<CapsuleCollider>();
		if (hitbox == null) return 0.45f;
		return hitbox.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
	}

	public float GetMoveSpeed() { return m_moveSpeed; }

	public bool MoveToResolved(Vector3 _requested, out Vector3 _destination)
	{
		_destination = transform.position;
		if (m_state == ePlayerObjectState.Dead) return false;
		if (m_navigation == null || m_agent == null || !m_agent.isOnNavMesh) return false;
		m_agent.nextPosition = transform.position;
		if (!m_navigation.TryNavigate(_requested, out List<Vector3> path, out _destination)) return false;
		m_path = path;
		if (m_path.Count > 0) SetState(ePlayerObjectState.Move);
		else SetState(ePlayerObjectState.Idle);
		if (ZoneSceneNetwork.Instance != null) ZoneSceneNetwork.Instance.NotifyMoveChanged();
		return true;
	}
}
