using UnityEngine;

public class ZoneSceneInfo : MonoBehaviour
{
	[SerializeField] ulong m_zoneId;
	[SerializeField] string m_zoneName;
	[SerializeField] Vector2 m_worldMin;
	[SerializeField] Vector2 m_worldMax = new Vector2(100.0f, 100.0f);
	[SerializeField] Texture2D m_mapImage;
	[Header("Server Zone Data")]
	[SerializeField] Vector3 m_enterPosition;
	[SerializeField] bool m_isStartZone;
	[SerializeField] bool m_isActive = true;

	public ulong GetZoneId() { return m_zoneId; }
	public string GetZoneName() { return m_zoneName; }
	public Vector2 GetWorldMin() { return m_worldMin; }
	public Vector2 GetWorldMax() { return m_worldMax; }
	public Texture2D GetMapImage() { return m_mapImage; }
	public Vector3 GetEnterPosition() { return m_enterPosition; }
	public bool IsStartZone() { return m_isStartZone; }
	public bool IsActive() { return m_isActive; }
}
