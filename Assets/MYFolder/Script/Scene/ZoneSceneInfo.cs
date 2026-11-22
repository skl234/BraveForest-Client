using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class ZoneSceneInfo : MonoBehaviour
{
	[SerializeField] ulong m_zoneId;
	[SerializeField] string m_zoneName;
	[SerializeField] Vector2 m_worldMin;
	[SerializeField] Vector2 m_worldMax = new Vector2(100.0f, 100.0f);
	[SerializeField, Min(1)] uint m_sectorWidth = 80;
	[SerializeField] Texture2D m_mapImage;
	[Header("Server Zone Data")]
	[SerializeField] Transform m_enterPoint;
	[SerializeField] bool m_isStartZone;
	[SerializeField] bool m_isActive = true;

	public ulong GetZoneId() { return m_zoneId; }
	public string GetZoneName() { return m_zoneName; }
	public Vector2 GetWorldMin() { return m_worldMin; }
	public Vector2 GetWorldMax() { return m_worldMax; }
	public Vector2 GetFieldSize() { return m_worldMax - m_worldMin; }
	public uint GetSectorWidth() { return m_sectorWidth; }
	public Transform GetEnterPoint() { return m_enterPoint; }
	public Texture2D GetMapImage() { return m_mapImage; }
	public Vector3 GetEnterPosition()
	{
		if (m_enterPoint == null) throw new System.InvalidOperationException(name + " 입장 위치가 연결되지 않았습니다.");
		Vector3 position = m_enterPoint.position;
		position.y = 0.0f;
		return position;
	}
	public bool IsStartZone() { return m_isStartZone; }
	public bool IsActive() { return m_isActive; }

	public static T[] GetSceneObjects<T>(Scene _scene) where T : Component
	{
		List<T> list = new List<T>();
		if (!_scene.IsValid() || !_scene.isLoaded) return list.ToArray();
		GameObject[] roots = _scene.GetRootGameObjects();
		for (int i = 0; i < roots.Length; ++i) list.AddRange(roots[i].GetComponentsInChildren<T>(true));
		return list.ToArray();
	}

	public static ZoneSceneInfo Find(Scene _scene)
	{
		ZoneSceneInfo[] list = GetSceneObjects<ZoneSceneInfo>(_scene);
		if (list.Length != 1) return null;
		return list[0];
	}

	void OnDrawGizmosSelected()
	{
		if (m_enterPoint == null) return;
		Gizmos.color = Color.cyan;
		Gizmos.DrawWireSphere(m_enterPoint.position, 0.75f);
	}
}
