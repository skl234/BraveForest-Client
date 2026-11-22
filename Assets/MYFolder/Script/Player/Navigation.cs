using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
public class Navigation
{
	NavMeshAgent m_agent;
	float m_prevNavigateTime;
	public Navigation(NavMeshAgent _agent)
	{
		m_agent = _agent;
		m_prevNavigateTime = 0.0f;
	}
	public List<Vector3> Navigate(Vector3 _destPosition)
	{
		return CalculatePath(_destPosition);
	}
	public List<Vector3> NavigateWithCoolTime(Vector3 _destPosition)
	{
		if (Time.time - m_prevNavigateTime < 0.2f) return new List<Vector3>();

		m_prevNavigateTime = Time.time;
		return CalculatePath(_destPosition);
	}
	private List<Vector3> CalculatePath(Vector3 _destPosition)
	{
		List<Vector3> path = new List<Vector3>();
		NavMeshPath navPath = new NavMeshPath();

		if (m_agent == null || !m_agent.CalculatePath(_destPosition, navPath))
			return path;

		path = navPath.corners.ToList();
		path.RemoveAt(0);
		path.Reverse();

		for (int i = 0; i < path.Count; i++)
		{
			path[i] = new Vector3(path[i].x, 0f, path[i].z);
		}

		return path;
	}
}
