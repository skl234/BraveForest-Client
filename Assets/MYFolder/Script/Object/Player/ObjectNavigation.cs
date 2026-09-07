using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectNavigation
{
    NavMeshAgent m_agent;
    NavMeshTriangulation m_mesh;
    bool m_cached;
    struct Candidate
    {
        public Vector3 point;
        public float distance;
    }

    public ObjectNavigation(NavMeshAgent _agent)
    {
        m_agent = _agent;
    }

    public List<Vector3> Navigate(Vector3 _destination)
    {
        List<Vector3> path = new List<Vector3>();
        if (m_agent == null || !m_agent.isOnNavMesh) return path;

        NavMeshPath navPath = new NavMeshPath();
        if (!m_agent.CalculatePath(_destination, navPath)) return path;
        if (navPath.status != NavMeshPathStatus.PathComplete) return path;
        if (navPath.corners.Length < 2) return path;

        for (int i = navPath.corners.Length - 1; i >= 1; --i)
        {
            Vector3 corner = navPath.corners[i];
            path.Add(new Vector3(corner.x, 0.0f, corner.z));
        }

        return path;
    }

    // 필드 이동용: 클릭한 XZ에 가장 가까운 '도달 가능한' NavMesh 지점을 찾습니다.
    // 맵의 경사/높이는 이동 영역이 아니므로 높이 차이가 아닌 평면 거리를 비교합니다.
    public bool TryNavigate(Vector3 _requested, out List<Vector3> _path, out Vector3 _destination)
    {
        _path = new List<Vector3>();
        _destination = _requested;
        if (m_agent == null || !m_agent.isOnNavMesh) return false;
        Vector3 point = new Vector3(_requested.x, m_agent.nextPosition.y, _requested.z);
        if (NavMesh.SamplePosition(point, out NavMeshHit direct, 0.15f, m_agent.areaMask) &&
            CompletePath(direct.position, out _path, out _destination)) return true;

        // 이동 가능한 지형은 현재 테스트 맵에서 고정입니다. 재베이크 시 새 Navigation을 사용합니다.
        if (!m_cached) { m_mesh = NavMesh.CalculateTriangulation(); m_cached = true; }
        List<Candidate> candidates = new List<Candidate>(m_mesh.indices.Length / 3);
        for (int i = 0; i < m_mesh.indices.Length; i += 3)
        {
            if ((m_agent.areaMask & (1 << m_mesh.areas[i / 3])) == 0) continue;
            Vector3 a = m_mesh.vertices[m_mesh.indices[i]];
            Vector3 b = m_mesh.vertices[m_mesh.indices[i + 1]];
            Vector3 c = m_mesh.vertices[m_mesh.indices[i + 2]];
            Vector3 closest = ClosestXZ(point, a, b, c);
            float distance = DistanceXZ(point, closest);
            // 경계선 부동소수점 오차를 피하기 위해 삼각형 안쪽으로 최대 2cm 보정합니다.
            closest = Vector3.MoveTowards(closest, (a + b + c) / 3.0f, 0.02f);
            candidates.Add(new Candidate { point = closest, distance = distance });
        }
        candidates.Sort((left, right) => left.distance.CompareTo(right.distance));
        for (int i = 0; i < candidates.Count; ++i)
        {
            if (CompletePath(candidates[i].point, out _path, out _destination)) return true;
        }
        return false;
    }

    bool CompletePath(Vector3 _point, out List<Vector3> _path, out Vector3 _destination)
    {
        _path = new List<Vector3>(); _destination = _point;
        NavMeshPath navPath = new NavMeshPath();
        if (!m_agent.CalculatePath(_point, navPath) || navPath.status != NavMeshPathStatus.PathComplete) return false;
        Vector3[] corners = navPath.corners;
        if (corners.Length == 0) return false;
        _destination = corners[corners.Length - 1];
        for (int i = corners.Length - 1; i >= 1; --i)
            _path.Add(new Vector3(corners[i].x, 0, corners[i].z));
        return true;
    }

    static float DistanceXZ(Vector3 a, Vector3 b)
    {
        float x = a.x - b.x, z = a.z - b.z;
        return x * x + z * z;
    }
    static float Cross(Vector3 a, Vector3 b) { return a.x * b.z - a.z * b.x; }
    static Vector3 Segment(Vector3 p, Vector3 a, Vector3 b)
    {
        float length = DistanceXZ(a, b);
        float t = length > 0 ? Mathf.Clamp01(((p.x - a.x) * (b.x - a.x) + (p.z - a.z) * (b.z - a.z)) / length) : 0;
        return Vector3.Lerp(a, b, t);
    }
    static Vector3 ClosestXZ(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        float denominator = Cross(b - a, c - a);
        if (Mathf.Abs(denominator) > 0.000001f)
        {
            float u = Cross(p - a, c - a) / denominator;
            float v = Cross(b - a, p - a) / denominator;
            if (u >= 0 && v >= 0 && u + v <= 1) return a + u * (b - a) + v * (c - a);
        }
        Vector3 ab = Segment(p, a, b), bc = Segment(p, b, c), ca = Segment(p, c, a);
        Vector3 closest = DistanceXZ(p, ab) <= DistanceXZ(p, bc) ? ab : bc;
        return DistanceXZ(p, closest) <= DistanceXZ(p, ca) ? closest : ca;
    }
}
