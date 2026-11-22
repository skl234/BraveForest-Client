using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
	public Transform m_target;                 // 따라갈 플레이어
	public float m_distance = 20f;              // 기본 거리
	public float m_minDistance = 2f;
	public float maxDistance = 300f;
	public float m_zoomSpeed = 5f;

	public float m_mouseSpeed = 3f;

	public float m_heightAngle = 45f;                // 상하 각도
	public float m_widthAngle = 0f;                   // 좌우 각도

	public float m_maxHeightAngle = -30f;
	public float m_maxWidthAngle = 80f;

	public LayerMask m_groundLayer;            // 카메라가 뚫지 않게 할 레이어

	private void Start()
	{
		m_target = null;
		//Cursor.lockState = CursorLockMode.Locked; // 커서 숨기고 고정
	}

	private void LateUpdate()
	{
		if (m_target == null) return;

		// 1. 마우스 입력으로 카메라 회전
		float mouseX = Input.GetAxis("Mouse X");
		float mouseY = Input.GetAxis("Mouse Y");

		//m_widthAngle += mouseX * m_mouseSpeed;
		//m_heightAngle -= mouseY * m_mouseSpeed;
		//m_heightAngle = Mathf.Clamp(m_heightAngle, m_maxHeightAngle, m_maxWidthAngle);

		// 2. 마우스 휠 줌
		float scroll = Input.GetAxis("Mouse ScrollWheel");
		m_distance -= scroll * m_zoomSpeed;
		m_distance = Mathf.Clamp(m_distance, m_minDistance, maxDistance);

		// 3. 회전 방향으로 offset 계산
		Vector3 targetOffset = m_target.position + Vector3.up * 2.0f;
		Vector3 offset = Quaternion.Euler(m_heightAngle, m_widthAngle, 0f) * new Vector3(0, 0, -m_distance);
		Vector3 desiredPosition = targetOffset + offset;

		// 4. 바닥이나 장애물로 카메라 뚫지 않도록 Raycast
		if (Physics.Linecast(targetOffset, desiredPosition, out RaycastHit hit, m_groundLayer))
		{
			transform.position = hit.point;
		}
		else
		{
			transform.position = desiredPosition;
		}

		transform.LookAt(targetOffset);
	}
}
