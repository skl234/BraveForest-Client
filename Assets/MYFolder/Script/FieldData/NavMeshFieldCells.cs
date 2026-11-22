using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// 클라이언트 NavMesh를 서버의 1m 셀로 내보낼 때 사용.
public sealed class NavMeshFieldCells
{
	const float Tolerance = .00001f;
	readonly int m_rows;
	readonly int m_cols;
	readonly bool[] m_walkable;

	public NavMeshFieldCells(int _rows, int _cols, Vector3 _origin, Vector3 _entry)
	{
		m_rows = _rows;
		m_cols = _cols;
		m_walkable = new bool[_rows * _cols];
		NavMeshTriangulation mesh = NavMesh.CalculateTriangulation();
		if (mesh.indices.Length == 0) throw new InvalidOperationException("NavMesh를 먼저 구워주세요.");
		for (int i = 0; i < mesh.indices.Length; i += 3)
		{
			if (mesh.areas[i / 3] == 1) continue;
			Vector3 va = mesh.vertices[mesh.indices[i]] - _origin;
			Vector3 vb = mesh.vertices[mesh.indices[i + 1]] - _origin;
			Vector3 vc = mesh.vertices[mesh.indices[i + 2]] - _origin;
			Vector2 a = new Vector2(va.x, va.z);
			Vector2 b = new Vector2(vb.x, vb.z);
			Vector2 c = new Vector2(vc.x, vc.z);
			int left = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x, c.x) - Tolerance));
			int right = Mathf.Min(_rows - 1, Mathf.FloorToInt(Mathf.Max(a.x, b.x, c.x) + Tolerance));
			int bottom = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y, c.y) - Tolerance));
			int top = Mathf.Min(_cols - 1, Mathf.FloorToInt(Mathf.Max(a.y, b.y, c.y) + Tolerance));
			for (int x = left; x <= right; ++x)
			{
				for (int z = bottom; z <= top; ++z)
				{
					Vector2 center = new Vector2(x + .5f, z + .5f);
					if (Separated(a, b, c, center, b - a)) continue;
					if (Separated(a, b, c, center, c - b)) continue;
					if (Separated(a, b, c, center, a - c)) continue;
					m_walkable[x * _cols + z] = true;
				}
			}
		}
		KeepConnected(_entry - _origin);
	}

	public bool IsWalkable(int _row, int _col)
	{
		if (_row < 0 || _col < 0 || _row >= m_rows || _col >= m_cols) return false;
		return m_walkable[_row * m_cols + _col];
	}

	static bool Separated(Vector2 _a, Vector2 _b, Vector2 _c, Vector2 _center, Vector2 _edge)
	{
		Vector2 axis = new Vector2(-_edge.y, _edge.x);
		float a = Vector2.Dot(_a - _center, axis);
		float b = Vector2.Dot(_b - _center, axis);
		float c = Vector2.Dot(_c - _center, axis);
		float radius = .5f * (Mathf.Abs(axis.x) + Mathf.Abs(axis.y));
		return Mathf.Min(a, b, c) > radius + Tolerance || Mathf.Max(a, b, c) < -radius - Tolerance;
	}

	void KeepConnected(Vector3 _entry)
	{
		int x = Mathf.FloorToInt(_entry.x);
		int z = Mathf.FloorToInt(_entry.z);
		if (!IsWalkable(x, z)) throw new InvalidOperationException("입장 위치가 NavMesh 셀 밖입니다.");
		bool[] visited = new bool[m_walkable.Length];
		Queue<int> queue = new Queue<int>();
		int first = x * m_cols + z;
		visited[first] = true;
		queue.Enqueue(first);
		while (queue.Count > 0)
		{
			int index = queue.Dequeue();
			x = index / m_cols;
			z = index % m_cols;
			Visit(x - 1, z, visited, queue);
			Visit(x + 1, z, visited, queue);
			Visit(x, z - 1, visited, queue);
			Visit(x, z + 1, visited, queue);
		}
		// 고립 셀은 서버 파일에서만 제외. 클라이언트 경로는 변경하지 않음.
		Array.Copy(visited, m_walkable, visited.Length);
	}

	void Visit(int _x, int _z, bool[] _visited, Queue<int> _queue)
	{
		if (!IsWalkable(_x, _z)) return;
		int index = _x * m_cols + _z;
		if (_visited[index]) return;
		_visited[index] = true;
		_queue.Enqueue(index);
	}
}
