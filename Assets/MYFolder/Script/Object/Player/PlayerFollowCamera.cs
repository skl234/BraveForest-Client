using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerFollowCamera : MonoBehaviour
{
	[SerializeField] Transform m_target;
	[SerializeField] float m_distance = 14.0f;
	[SerializeField] float m_minDistance = 4.5f;
	[SerializeField] float m_maxDistance = 35.0f;
	[SerializeField] float m_zoomSpeed = 8.0f;
	[SerializeField] float m_targetHeight = 1.0f;
	public void SetTarget(Transform _target) { m_target = _target; }

	void LateUpdate()
	{
		if (m_target == null) return;
		// 채팅/목록 위의 휠 입력으로 카메라까지 움직이지 않게
		if (!UIInputBlocker.IsGameplayBlocked() && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
			m_distance = Mathf.Clamp(m_distance - Input.GetAxis("Mouse ScrollWheel") * m_zoomSpeed, m_minDistance, m_maxDistance);
		Vector3 focus = m_target.position + Vector3.up * m_targetHeight;
		// 남쪽(-Z), 위쪽(+Y), 45도. 회전 구도만 고정하고 위치는 항상 추적합니다.
		transform.rotation = Quaternion.Euler(45.0f, 0.0f, 0.0f);
		transform.position = focus - transform.forward * m_distance;
	}
}
