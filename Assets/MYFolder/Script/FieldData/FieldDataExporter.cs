using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public class FieldDataExporter : MonoBehaviour
{
    const float CellSize = 1.0f;
	[SerializeField] ulong m_zoneId = 2;

    [SerializeField] Terrain m_terrain;
    [SerializeField] MonsterSpawnExporter m_monsterSpawnExporter;
    [SerializeField] uint m_sectorWidth = 80;
    [SerializeField] float m_maxSlopeAngle = 1.0f;
    [SerializeField] LayerMask m_obstacleMask;
    [SerializeField] float m_obstacleCheckHeight = 4.0f;
    [SerializeField] bool m_requireNavMesh;
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

    [ContextMenu("Export ZoneServer Field Data")]
    public void Export()
    {
        if (m_terrain == null || m_monsterSpawnExporter == null)
        {
            Debug.LogError("[FieldDataExporter] Terrain 또는 MonsterSpawnExporter가 연결되지 않았습니다.");
            return;
        }
        if (m_sectorWidth == 0)
        {
            Debug.LogError("[FieldDataExporter] SectorWidth는 0보다 커야 합니다.");
            return;
        }
		if (!m_monsterSpawnExporter.IsValidZone(m_zoneId) || !m_monsterSpawnExporter.Validate())
		{
			Debug.LogError("[FieldDataExporter] 씬/맵/몬스터 내보내기의 ZoneId 또는 스폰 설정을 확인하세요.");
			return;
		}

        Vector3 terrainSize = m_terrain.terrainData.size;
        uint widthX = (uint)Mathf.CeilToInt(terrainSize.x);
        uint widthZ = (uint)Mathf.CeilToInt(terrainSize.z);
        uint cellRows = (uint)Mathf.CeilToInt(terrainSize.x / CellSize);
        uint cellCols = (uint)Mathf.CeilToInt(terrainSize.z / CellSize);
        uint sectorRows = (widthX + m_sectorWidth - 1) / m_sectorWidth;
        uint sectorCols = (widthZ + m_sectorWidth - 1) / m_sectorWidth;
        List<uint> blockList = CreateBlockList(cellRows, cellCols);
        m_previewRows = cellRows;
        m_previewCols = cellCols;
        m_previewSectorRows = sectorRows;
        m_previewSectorCols = sectorCols;
        m_previewBlockList = blockList;

        string directory = Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, m_fileName);

        using (StreamWriter writer = new StreamWriter(path, false))
        {
            writer.WriteLine("WidthX " + widthX);
            writer.WriteLine("WidthZ " + widthZ);
            writer.WriteLine("CellRows " + cellRows);
            writer.WriteLine("CellCols " + cellCols);
            writer.WriteLine("SectorWidth " + m_sectorWidth);
            writer.WriteLine("SectorRows " + sectorRows);
            writer.WriteLine("SectorCols " + sectorCols);
            writer.WriteLine("BlockCount " + blockList.Count);
            for (int i = 0; i < blockList.Count; ++i) writer.WriteLine(blockList[i]);

            if (!m_monsterSpawnExporter.Write(writer, m_terrain.transform.position))
            {
                Debug.LogError("[FieldDataExporter] 몬스터 스폰 정보가 올바르지 않습니다.");
                return;
            }
        }

        Debug.Log(string.Format(CultureInfo.InvariantCulture,
            "[FieldDataExporter] {0} Cells={1} Blocks={2} Sectors={3}x{4}",
            path, cellRows * cellCols, blockList.Count, sectorRows, sectorCols));
    }

    List<uint> CreateBlockList(uint _rows, uint _cols)
    {
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
        float y = origin.y + m_previewHeight;

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

        if (m_showSectorGrid && m_sectorWidth > 0)
        {
            Gizmos.color = new Color(1.0f, 0.82f, 0.05f, 1.0f);
            uint sectorRows = (uint)Mathf.CeilToInt(size.x / m_sectorWidth);
            uint sectorCols = (uint)Mathf.CeilToInt(size.z / m_sectorWidth);
            for (uint row = 0; row <= sectorRows; ++row)
            {
                float x = Mathf.Min(row * m_sectorWidth, size.x);
                Vector3 start = origin + new Vector3(x, y + 0.08f, 0.0f);
                Vector3 end = origin + new Vector3(x, y + 0.08f, size.z);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawLine(start + Vector3.right * 0.04f, end + Vector3.right * 0.04f);
            }
            for (uint col = 0; col <= sectorCols; ++col)
            {
                float z = Mathf.Min(col * m_sectorWidth, size.z);
                Vector3 start = origin + new Vector3(0.0f, y + 0.08f, z);
                Vector3 end = origin + new Vector3(size.x, y + 0.08f, z);
                Gizmos.DrawLine(start, end);
                Gizmos.DrawLine(start + Vector3.forward * 0.04f, end + Vector3.forward * 0.04f);
            }
        }
    }
}
