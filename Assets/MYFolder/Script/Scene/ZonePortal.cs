using UnityEngine;

public class ZonePortal : MonoBehaviour
{
	[SerializeField] ulong m_portalId = 1;
	[SerializeField] bool m_active = true;
	[SerializeField, Min(0.1f)] float m_enterDistance = 2.4f;
	[Header("Server Portal Data")]
	[SerializeField] ulong m_targetZoneId;
	[SerializeField] ulong m_targetPortalId = 1;
	[SerializeField] Transform m_arrivalPoint;
	bool m_positionChecked;
	bool m_inside;

	public ulong GetPortalId() { return m_portalId; }
	public bool IsActive() { return m_active; }
	public ulong GetTargetZoneId() { return m_targetZoneId; }
	public ulong GetTargetPortalId() { return m_targetPortalId; }
	public Transform GetArrivalPoint() { return m_arrivalPoint; }
	public Vector3 GetArrivalPosition()
	{
		if (m_arrivalPoint == null) throw new System.InvalidOperationException(name + " 도착 위치가 연결되지 않았습니다.");
		Vector3 position = m_arrivalPoint.position;
		position.y = 0.0f;
		return position;
	}
	public float GetEnterDistance() { return m_enterDistance; }

	void OnEnable()
	{
		m_positionChecked = false;
		m_inside = false;
	}

	public bool IsInRange(Vector3 _position)
	{
		Vector3 difference = _position - transform.position;
		difference.y = 0.0f;
		return difference.sqrMagnitude <= m_enterDistance * m_enterDistance;
	}

	public bool TryEnter(Vector3 _position)
	{
		bool inside = IsInRange(_position);
		// 입장 위치가 포탈 안이어도 바로 되돌아가지 않게 함
		if (!m_positionChecked)
		{
			m_positionChecked = true;
			m_inside = inside;
			return false;
		}
		bool entered = inside && !m_inside;
		m_inside = inside;
		// 실패해도 안에 서 있는 동안 반복 요청하지 않음. 밖으로 나왔다가 재진입
		return m_active && entered;
	}

	void Update()
	{
		if (!m_active || ZoneSceneNetwork.Instance == null || ZoneSceneNetwork.Instance.IsWaiting()) return;
		LocalPlayerObject player = ZoneSceneNetwork.Instance.GetLocalPlayer();
		if (player == null) return;
		if (TryEnter(player.transform.position)) ZoneSceneNetwork.Instance.RequestChangeZone(m_portalId);
	}

	void OnDrawGizmosSelected()
	{
		if (m_arrivalPoint != null)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(m_arrivalPoint.position, 0.75f);
			Gizmos.DrawLine(transform.position, m_arrivalPoint.position);
		}
		Gizmos.color = new Color(0.2f, 0.8f, 1.0f, 1.0f);
		Vector3 center = transform.position + Vector3.up * .05f;
		Vector3 previous = center + Vector3.right * m_enterDistance;
		for (int i = 1; i <= 64; ++i)
		{
			float angle = i * Mathf.PI * 2.0f / 64;
			Vector3 next = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * m_enterDistance;
			Gizmos.DrawLine(previous, next);
			previous = next;
		}
	}
}
