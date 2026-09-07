using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class CellInfoCreater : MonoBehaviour
{
    public Terrain m_terrain;
    public int m_cellCount_x = 500;
    public int m_cellCount_z = 500;
    public int m_cellSize = 1;

    public int m_sectorWidth;
    public int m_sectorRows;
    public int m_sectorCols;

    [Range(0f, 90f)]
    public float m_maxSlopeAngle = 45f; // 절벽판단 경사각

    public LayerMask m_obstacleMask;
    public int m_startX;
    public int m_startZ;
    // 검사 시작 위치
    // 해당 위치를 시작으로 주변탐색
    // 뭐 대충 아무 평지로 시작하면 되긴하다
    // 지금 x랑 z가 index번호로도 좌표로도 쓰이니까 헷갈리네...는 수정하기엔 시간이 좀 늦었네

    private bool[,] m_moveableCellList;  //이동유무 cellList

    void Start()
    {
        GenerateFinalMoveableCells();
    }

    void GenerateFinalMoveableCells()
    {
        m_moveableCellList = new bool[m_cellCount_x, m_cellCount_z];
        if (!IsInCellList(m_startX, m_startZ)) return;
        if (!IsMoveable(m_startX, m_startZ)) return;

        Queue<Vector3Int> checkQueue = new Queue<Vector3Int>();
        bool[,] checkedCell = new bool[m_cellCount_x, m_cellCount_z];

        checkQueue.Enqueue(new Vector3Int(m_startX, 0, m_startZ));
        checkedCell[m_startX, m_startZ] = true;
        Vector3Int[] directions = { new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 0, -1), new Vector3Int(0, 0, 1) };

        while (checkQueue.Count > 0)
        {
            Vector3Int v3i = checkQueue.Dequeue();
            m_moveableCellList[v3i.x, v3i.z] = true;

            foreach (Vector3Int dir in directions)
            {
                int nextX = v3i.x + dir.x;
                int nextZ = v3i.z + dir.z;
                if (!IsInCellList(nextX, nextZ)) continue;
                if (checkedCell[nextX, nextZ]) continue;
                if (!IsMoveable(nextX, nextZ)) continue;

                checkedCell[nextX, nextZ] = true;
                checkQueue.Enqueue(new Vector3Int(nextX, 0, nextZ));
            }
        }

        SaveFile(Path.Combine(Application.dataPath, "GridInfo.bin"));
        Debug.Log("CellInfo 생성 완료");
    }

    bool IsMoveable(int _x, int _z)
    {
        Vector3 center = GetCellCenterWorldPos(_x, _z);
        float centerY = m_terrain.SampleHeight(center);

        if (IsOverSlope(_x, _z, centerY)) return false;
        if (IsObstacle(center)) return false;

        return true;
    }

    Vector3 GetCellCenterWorldPos(int _x, int _z)
    {
        //터레인을 기준
        //Vector3 origin = m_terrain.transform.position;
        //return new Vector3((_x + 0.5f) * m_cellSize + origin.x, 0, (_z + 0.5f) * m_cellSize + origin.z);

        //월드를 기준
        return new Vector3((_x + 0.5f) * m_cellSize, 0, (_z + 0.5f) * m_cellSize);
    }

    bool IsOverSlope(int _x, int _z, float _centerY)
    {
        Vector3Int[] direstions = { new Vector3Int(-1, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 0, -1), new Vector3Int(0, 0, 1) };

        foreach (Vector3Int dir in direstions)
        {
            int nextX = _x + dir.x;
            int nextZ = _z + dir.z;
            if (!IsInCellList(nextX, nextZ)) continue;

            Vector3 neighborCenter = GetCellCenterWorldPos(nextX, nextZ);
            float neighborY = m_terrain.SampleHeight(neighborCenter);
            float slope = Mathf.Abs(neighborY - _centerY) / m_cellSize;
            float angle = Mathf.Atan(slope) * Mathf.Rad2Deg;

            if (angle > m_maxSlopeAngle) return true;
        }

        return false;
    }

    bool IsObstacle(Vector3 _center)
    {
        float y = m_terrain.SampleHeight(_center);
        Vector3 checkCenter = new Vector3(_center.x, y, _center.z);

        return Physics.CheckBox(checkCenter, Vector3.one * (m_cellSize * 0.5f), Quaternion.identity, m_obstacleMask);
    }

    bool IsInCellList(int _x, int _z)
    {
        if (_x >= 0 && _x < m_cellCount_x && _z >= 0 && _z < m_cellCount_z) return true;
        return false;
    }
    void SaveFile(string _path)
    {
        BinaryWriter stream = new BinaryWriter(File.Open(_path, FileMode.Create));

        //cellGrid 저장
        stream.Write(m_cellCount_x);
        stream.Write(m_cellCount_z);
        stream.Write(m_cellSize);
        for(int i = 0; i < m_cellCount_x; ++i)
        {
            for(int j = 0; j < m_cellCount_z; ++j)
            {
                stream.Write(m_moveableCellList[i, j]);
            }
        }

        //sectorGird 저장
        stream.Write(m_sectorWidth);
        stream.Write(m_sectorRows);
        stream.Write(m_sectorCols);

        stream.Dispose();
        Debug.Log("stream 저장");
    }

    void OnDrawGizmos()
    {
        if (m_moveableCellList == null) return;

        for (int x = 0; x < m_cellCount_x; x++)
        {
            for (int z = 0; z < m_cellCount_z; z++)
            {
                Vector3 center = GetCellCenterWorldPos(x, z);
                center.y = m_terrain.SampleHeight(center) + 0.1f;

                Gizmos.color = m_moveableCellList[x, z] ? Color.blue : Color.red;
                Gizmos.DrawCube(center, Vector3.one * m_cellSize);
            }
        }
    }
}
