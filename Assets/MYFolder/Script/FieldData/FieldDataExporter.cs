using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FieldDataExporter : MonoBehaviour
{
	const float CellSize = 1.0f;
	[SerializeField] ZoneSceneInfo m_sceneInfo;

	[SerializeField] Terrain m_terrain;
	[SerializeField] float m_maxSlopeAngle = 1.0f;
	[SerializeField] LayerMask m_obstacleMask;
	[SerializeField] float m_obstacleCheckHeight = 4.0f;
	[SerializeField] bool m_requireNavMesh;
	[SerializeField] bool m_useNavMeshCells;
	NavMeshFieldCells m_navigationCells;
	[SerializeField] string m_fileName = "BeginnerZoneField.txt";
	[SerializeField] bool m_showCellGrid = true;
	[SerializeField] bool m_showBlockCell = true;
	[SerializeField] bool m_showSectorGrid = true;
	[SerializeField] float m_previewHeight = 0.15f;
	[SerializeField, HideInInspector] uint m_previewRows;
	[SerializeField, HideInInspector] uint m_previewCols;
	[SerializeField, HideInInspector] uint m_previewSectorRows;
	[SerializeField, HideInInspector] uint m_previewSectorCols;
	[SerializeField, HideInInspector] List<uint> m_previewBlockList = new List<uint>();

	public ZoneSceneInfo GetSceneInfo() { return m_sceneInfo; }
	public Terrain GetTerrain() { return m_terrain; }
	public string GetFileName() { return m_fileName; }

	public void RefreshNavigationCells()
	{
		m_navigationCells = null;
	}

	void OnEnable() { RefreshNavigationCells(); }
	void OnValidate() { RefreshNavigationCells(); }

	public void Initialize(ZoneSceneInfo _sceneInfo, Terrain _terrain)
	{
		m_sceneInfo = _sceneInfo;
		m_terrain = _terrain;
		RefreshNavigationCells();
	}

	public bool Validate()
	{
		if (m_sceneInfo == null || m_sceneInfo != ZoneSceneInfo.Find(gameObject.scene)) return false;
		if (m_terrain == null || m_terrain.terrainData == null || m_terrain.gameObject.scene != gameObject.scene) return false;
		if (m_sceneInfo.GetZoneId() == 0 || m_sceneInfo.GetSectorWidth() == 0) return false;
		if (string.IsNullOrEmpty(m_fileName) || Path.GetFileName(m_fileName) != m_fileName) return false;

		Vector2 size = m_sceneInfo.GetFieldSize();
		Vector3 terrainSize = m_terrain.terrainData.size;
		Vector3 origin = m_terrain.transform.position;
		// 서버 좌표는 필드의 XZ 원점에서 시작
		if (m_sceneInfo.GetWorldMin() != Vector2.zero || Mathf.Abs(origin.x) > 0.001f || Mathf.Abs(origin.z) > 0.001f) return false;
		if (size.x <= 0 || size.y <= 0 || float.IsInfinity(size.x) || float.IsInfinity(size.y) || float.IsNaN(size.x) || float.IsNaN(size.y)) return false;
		return Mathf.Abs(size.x - terrainSize.x) < 0.001f && Mathf.Abs(size.y - terrainSize.z) < 0.001f;
	}

	[ContextMenu("Export ZoneServer Field Data")]
	public void Export()
	{
		Export(Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData"));
	}

	public void Export(string _directory)
	{
		if (!Validate()) throw new System.InvalidOperationException(name + " 필드 설정을 확인하세요.");
		Directory.CreateDirectory(_directory);
		using (StreamWriter writer = new StreamWriter(Path.Combine(_directory, m_fileName), false)) Write(writer);
	}

	public void Write(StreamWriter _writer)
	{
		if (!Validate()) throw new System.InvalidOperationException(name + " 필드 설정을 확인하세요.");
		Vector2 size = m_sceneInfo.GetFieldSize();
		uint widthX = (uint)Mathf.CeilToInt(size.x);
		uint widthZ = (uint)Mathf.CeilToInt(size.y);
		uint sectorWidth = m_sceneInfo.GetSectorWidth();
		uint cellRows = widthX;
		uint cellCols = widthZ;
		uint sectorRows = (widthX + sectorWidth - 1) / sectorWidth;
		uint sectorCols = (widthZ + sectorWidth - 1) / sectorWidth;
		Physics.SyncTransforms();
		List<uint> blockList = CreateBlockList(cellRows, cellCols);
		m_previewRows = cellRows;
		m_previewCols = cellCols;
		m_previewSectorRows = sectorRows;
		m_previewSectorCols = sectorCols;
		m_previewBlockList = blockList;

		_writer.WriteLine("WidthX " + widthX);
		_writer.WriteLine("WidthZ " + widthZ);
		_writer.WriteLine("CellRows " + cellRows);
		_writer.WriteLine("CellCols " + cellCols);
		_writer.WriteLine("SectorWidth " + sectorWidth);
		_writer.WriteLine("SectorRows " + sectorRows);
		_writer.WriteLine("SectorCols " + sectorCols);
		_writer.WriteLine("BlockCount " + blockList.Count);
		for (int i = 0; i < blockList.Count; ++i) _writer.WriteLine(blockList[i]);
		// 스폰은 별도 파일로 내보냄. 기존 Field 로더의 끝 표시는 유지
		_writer.WriteLine("MonsterCount 0");
	}

	public bool IsWalkablePosition(Vector3 _position)
	{
		if (!Validate() || float.IsNaN(_position.x) || float.IsNaN(_position.z) || float.IsInfinity(_position.x) || float.IsInfinity(_position.z)) return false;
		Vector3 origin = m_terrain.transform.position;
		Vector2 size = m_sceneInfo.GetFieldSize();
		Vector3 local = _position - origin;
		if (local.x < 0 || local.z < 0 || local.x >= size.x || local.z >= size.y) return false;
		return !IsBlock((uint)Mathf.FloorToInt(local.x), (uint)Mathf.FloorToInt(local.z), origin);
	}

	public List<uint> GetBlockList()
	{
		if (!Validate()) throw new System.InvalidOperationException(name + " 필드 설정을 확인하세요.");
		Physics.SyncTransforms();
		Vector2 size = m_sceneInfo.GetFieldSize();
		return CreateBlockList((uint)Mathf.CeilToInt(size.x), (uint)Mathf.CeilToInt(size.y));
	}

	List<uint> CreateBlockList(uint _rows, uint _cols)
	{
		RefreshNavigationCells();
		List<uint> blockList = new List<uint>();
		Vector3 origin = m_terrain.transform.position;

		for (uint row = 0; row < _rows; ++row)
		{
			for (uint col = 0; col < _cols; ++col)
			{
				if (!IsBlock(row, col, origin)) continue;
				blockList.Add(row * _cols + col);
			}
		}
		return blockList;
	}

	bool IsBlock(uint _row, uint _col, Vector3 _origin)
	{
		if (m_useNavMeshCells)
		{
			if (m_navigationCells == null)
			{
				Vector2 size = m_sceneInfo.GetFieldSize();
				m_navigationCells = new NavMeshFieldCells(Mathf.CeilToInt(size.x), Mathf.CeilToInt(size.y), _origin, m_sceneInfo.GetEnterPosition());
			}
			return !m_navigationCells.IsWalkable((int)_row, (int)_col);
		}

		float localX = (_row + 0.5f) * CellSize;
		float localZ = (_col + 0.5f) * CellSize;
		Vector3 terrainSize = m_terrain.terrainData.size;
		if (localX >= terrainSize.x || localZ >= terrainSize.z) return true;

		Vector3 position = _origin + new Vector3(localX, 0.0f, localZ);
		float normalizedX = localX / terrainSize.x;
		float normalizedZ = localZ / terrainSize.z;
		float slope = m_terrain.terrainData.GetSteepness(normalizedX, normalizedZ);
		if (slope > m_maxSlopeAngle) return true;
		if (m_requireNavMesh)
		{
			UnityEngine.AI.NavMeshHit hit;
			Vector3 ground = position;
			ground.y = m_terrain.SampleHeight(position) + _origin.y;
			if (!UnityEngine.AI.NavMesh.SamplePosition(ground, out hit, 0.2f, UnityEngine.AI.NavMesh.AllAreas)) return true;
		}

		position.y = _origin.y + m_terrain.SampleHeight(position) + m_obstacleCheckHeight * 0.5f;
		// 셀 사이 빈틈 없이 검사. 경계에 놓인 얇은 기둥도 Block에 포함
		Vector3 halfExtents = new Vector3(CellSize * 0.5f + 0.001f, m_obstacleCheckHeight * 0.5f, CellSize * 0.5f + 0.001f);
		Collider[] colliderList = Physics.OverlapBox(position, halfExtents, Quaternion.identity,
			m_obstacleMask, QueryTriggerInteraction.Ignore);
		for (int i = 0; i < colliderList.Length; ++i)
		{
			if (colliderList[i] is TerrainCollider) continue;
			if (colliderList[i].GetComponentInParent<MonsterSpawnPoint>() != null) continue;
			return true;
		}
		return false;
	}

	public bool FindNearestWalkablePosition(Vector3 _wantedPosition, out Vector3 _position)
	{
		_position = _wantedPosition;
		if (m_terrain == null) return false;
		Vector3 origin = m_terrain.transform.position;
		int wantedRow = Mathf.FloorToInt(_wantedPosition.x - origin.x);
		int wantedCol = Mathf.FloorToInt(_wantedPosition.z - origin.z);
		uint rows = (uint)Mathf.CeilToInt(m_terrain.terrainData.size.x / CellSize);
		uint cols = (uint)Mathf.CeilToInt(m_terrain.terrainData.size.z / CellSize);
		for (int range = 0; range <= 20; ++range)
		{
			for (int row = wantedRow - range; row <= wantedRow + range; ++row)
			{
				for (int col = wantedCol - range; col <= wantedCol + range; ++col)
				{
					if (row < 0 || col < 0) continue;
					if ((uint)row >= rows || (uint)col >= cols) continue;
					if (IsBlock((uint)row, (uint)col, origin)) continue;
					_position = origin + new Vector3(row + 0.5f, 0.0f, col + 0.5f);
					_position.y = m_terrain.SampleHeight(_position) + origin.y;
					return true;
				}
			}
		}
		return false;
	}

	void OnDrawGizmosSelected()
	{
		if (m_terrain == null) return;

		Vector3 origin = m_terrain.transform.position;
		Vector3 size = m_terrain.terrainData.size;
		float y = m_previewHeight;

		if (m_showCellGrid)
		{
			Gizmos.color = new Color(0.25f, 1.0f, 0.35f, 0.35f);
			uint rows = (uint)Mathf.CeilToInt(size.x / CellSize);
			uint cols = (uint)Mathf.CeilToInt(size.z / CellSize);
			for (uint row = 0; row <= rows; ++row)
			{
				float x = Mathf.Min(row * CellSize, size.x);
				Gizmos.DrawLine(origin + new Vector3(x, y, 0.0f), origin + new Vector3(x, y, size.z));
			}
			for (uint col = 0; col <= cols; ++col)
			{
				float z = Mathf.Min(col * CellSize, size.z);
				Gizmos.DrawLine(origin + new Vector3(0.0f, y, z), origin + new Vector3(size.x, y, z));
			}
		}

		if (m_showBlockCell && m_previewCols > 0)
		{
			Gizmos.color = new Color(1.0f, 0.15f, 0.1f, 0.75f);
			for (int i = 0; i < m_previewBlockList.Count; ++i)
			{
				uint index = m_previewBlockList[i];
				uint row = index / m_previewCols;
				uint col = index % m_previewCols;
				Vector3 center = origin + new Vector3((row + 0.5f) * CellSize, y + 0.03f, (col + 0.5f) * CellSize);
				Gizmos.DrawWireCube(center, new Vector3(CellSize * 0.9f, 0.08f, CellSize * 0.9f));
			}
		}

		uint sectorWidth = 0;
		if (m_sceneInfo != null) sectorWidth = m_sceneInfo.GetSectorWidth();
		if (m_showSectorGrid && sectorWidth > 0)
		{
			Gizmos.color = new Color(1.0f, 0.82f, 0.05f, 1.0f);
			uint sectorRows = (uint)Mathf.CeilToInt(size.x / sectorWidth);
			uint sectorCols = (uint)Mathf.CeilToInt(size.z / sectorWidth);
			for (uint row = 0; row <= sectorRows; ++row)
			{
				float x = Mathf.Min(row * sectorWidth, size.x);
				Vector3 start = origin + new Vector3(x, y + 0.08f, 0.0f);
				Vector3 end = origin + new Vector3(x, y + 0.08f, size.z);
				Gizmos.DrawLine(start, end);
				Gizmos.DrawLine(start + Vector3.right * 0.04f, end + Vector3.right * 0.04f);
			}
			for (uint col = 0; col <= sectorCols; ++col)
			{
				float z = Mathf.Min(col * sectorWidth, size.z);
				Vector3 start = origin + new Vector3(0.0f, y + 0.08f, z);
				Vector3 end = origin + new Vector3(size.x, y + 0.08f, z);
				Gizmos.DrawLine(start, end);
				Gizmos.DrawLine(start + Vector3.forward * 0.04f, end + Vector3.forward * 0.04f);
			}
		}
	}
}
