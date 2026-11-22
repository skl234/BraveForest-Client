using System;
using UnityEngine;

[CreateAssetMenu(menuName = "BraveForest/Town Layout")]
public class TownLayout : ScriptableObject
{
	[Serializable]
	public class Road
	{
		public float width;
		public Vector2[] points;
	}

	[Serializable]
	public class Square
	{
		public string name;
		public Vector2 center;
		public Vector2 size;
		public float corner;
	}

	[Serializable]
	public class Building
	{
		public string name;
		public Vector2 position;
		public Vector2 size;
		public float rotation;
		public int style;
	}

	[SerializeField] Vector2 m_size = new Vector2(200, 180);
	[SerializeField] Vector2 m_enterPosition = new Vector2(100, 35);
	[SerializeField] Vector2 m_beginnerPortal = new Vector2(165, 145);
	[SerializeField] Vector2 m_beginnerArrival = new Vector2(165, 136);
	[SerializeField] Vector2 m_veteranPortal = new Vector2(32, 152);
	[SerializeField] Road[] m_roadList;
	[SerializeField] Square[] m_squareList;
	[SerializeField] Building[] m_buildingList;

	public Vector2 GetSize() { return m_size; }
	public Vector2 GetEnterPosition() { return m_enterPosition; }
	public Vector2 GetBeginnerPortal() { return m_beginnerPortal; }
	public Vector2 GetBeginnerArrival() { return m_beginnerArrival; }
	public Vector2 GetVeteranPortal() { return m_veteranPortal; }
	public Road[] GetRoadList() { return m_roadList; }
	public Square[] GetSquareList() { return m_squareList; }
	public Building[] GetBuildingList() { return m_buildingList; }

	public void Initialize(Road[] _roads, Square[] _squares, Building[] _buildings)
	{
		m_roadList = _roads;
		m_squareList = _squares;
		m_buildingList = _buildings;
	}

	public float GetPaving(Vector2 _position)
	{
		float distance = float.PositiveInfinity;
		for (int i = 0; i < m_squareList.Length; ++i)
		{
			Square square = m_squareList[i];
			Vector2 delta = _position - square.center;
			Vector2 q = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) - square.size * .5f + Vector2.one * square.corner;
			float value = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - square.corner;
			distance = Mathf.Min(distance, value);
		}
		for (int i = 0; i < m_roadList.Length; ++i)
		{
			Road road = m_roadList[i];
			for (int j = 1; j < road.points.Length; ++j)
			{
				Vector2 start = road.points[j - 1];
				Vector2 direction = road.points[j] - start;
				float t = Mathf.Clamp01(Vector2.Dot(_position - start, direction) / Mathf.Max(.001f, direction.sqrMagnitude));
				distance = Mathf.Min(distance, Vector2.Distance(_position, start + direction * t) - road.width * .5f);
			}
		}
		// 건물 출입구와 대로 사이의 짧은 진입로.
		for (int i = 0; i < m_buildingList.Length; ++i)
		{
			Building building = m_buildingList[i];
			Vector3 direction3 = Quaternion.Euler(0, building.rotation, 0) * Vector3.back;
			Vector2 direction = new Vector2(direction3.x, direction3.z);
			Vector2 start = building.position + direction * (building.size.y * .5f);
			Vector2 end = GetNearestRoad(start);
			Vector2 segment = end - start;
			float t = Mathf.Clamp01(Vector2.Dot(_position - start, segment) / Mathf.Max(.001f, segment.sqrMagnitude));
			distance = Mathf.Min(distance, Vector2.Distance(_position, start + segment * t) - 2.0f);
		}
		return 1.0f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-.5f, 1.0f, distance));
	}

	Vector2 GetNearestRoad(Vector2 _position)
	{
		Vector2 result = _position;
		float nearest = float.PositiveInfinity;
		for (int i = 0; i < m_roadList.Length; ++i)
			for (int j = 1; j < m_roadList[i].points.Length; ++j)
			{
				Vector2 start = m_roadList[i].points[j - 1];
				Vector2 delta = m_roadList[i].points[j] - start;
				float t = Mathf.Clamp01(Vector2.Dot(_position - start, delta) / Mathf.Max(.001f, delta.sqrMagnitude));
				Vector2 point = start + delta * t;
				float distance = (point - _position).sqrMagnitude;
				if (distance >= nearest) continue;
				nearest = distance;
				result = point;
			}
		return result;
	}
}
