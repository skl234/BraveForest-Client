using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UICornerClip : BaseMeshEffect
{
	[SerializeField] UIFrameGraphic m_frame;
	readonly List<UIVertex> m_vertices = new List<UIVertex>();
	List<UIVertex> m_polygon = new List<UIVertex>(8);
	List<UIVertex> m_output = new List<UIVertex>(8);

	public void Setup(UIFrameGraphic _frame)
	{
		m_frame = _frame;
		if (graphic != null) graphic.SetVerticesDirty();
	}

	public override void ModifyMesh(VertexHelper _mesh)
	{
		if (!IsActive() || m_frame == null || _mesh.currentVertCount == 0) return;
		Rect rect = graphic.GetPixelAdjustedRect();
		float cut = Mathf.Min(m_frame.GetCornerCut(), Mathf.Min(rect.width, rect.height) * .3f);
		if (cut <= 0) return;
		m_vertices.Clear();
		_mesh.GetUIVertexStream(m_vertices);
		_mesh.Clear();
		for (int i = 0; i < m_vertices.Count; i += 3)
		{
			m_polygon.Clear();
			m_polygon.Add(m_vertices[i]);
			m_polygon.Add(m_vertices[i + 1]);
			m_polygon.Add(m_vertices[i + 2]);
			Clip(new Vector2(1, 1), rect.xMin + rect.yMin + cut);
			Clip(new Vector2(-1, 1), -rect.xMax + rect.yMin + cut);
			Clip(new Vector2(-1, -1), -rect.xMax - rect.yMax + cut);
			Clip(new Vector2(1, -1), rect.xMin - rect.yMax + cut);
			int start = _mesh.currentVertCount;
			for (int j = 0; j < m_polygon.Count; ++j) _mesh.AddVert(m_polygon[j]);
			for (int j = 1; j < m_polygon.Count - 1; ++j) _mesh.AddTriangle(start, start + j, start + j + 1);
		}
	}

	void Clip(Vector2 _normal, float _distance)
	{
		if (m_polygon.Count == 0) return;
		m_output.Clear();
		UIVertex previous = m_polygon[m_polygon.Count - 1];
		float previousDistance = Vector2.Dot(_normal, previous.position) - _distance;
		for (int i = 0; i < m_polygon.Count; ++i)
		{
			UIVertex current = m_polygon[i];
			float currentDistance = Vector2.Dot(_normal, current.position) - _distance;
			if ((previousDistance >= 0) != (currentDistance >= 0))
				m_output.Add(Intersection(previous, current, previousDistance / (previousDistance - currentDistance)));
			if (currentDistance >= 0) m_output.Add(current);
			previous = current;
			previousDistance = currentDistance;
		}
		List<UIVertex> temp = m_polygon;
		m_polygon = m_output;
		m_output = temp;
	}

	static UIVertex Intersection(UIVertex _a, UIVertex _b, float _ratio)
	{
		UIVertex vertex = _a;
		vertex.position = Vector3.Lerp(_a.position, _b.position, _ratio);
		vertex.color = Color32.Lerp(_a.color, _b.color, _ratio);
		vertex.uv0 = Vector4.Lerp(_a.uv0, _b.uv0, _ratio);
		vertex.uv1 = Vector4.Lerp(_a.uv1, _b.uv1, _ratio);
		vertex.uv2 = Vector4.Lerp(_a.uv2, _b.uv2, _ratio);
		vertex.uv3 = Vector4.Lerp(_a.uv3, _b.uv3, _ratio);
		vertex.normal = Vector3.Lerp(_a.normal, _b.normal, _ratio);
		vertex.tangent = Vector4.Lerp(_a.tangent, _b.tangent, _ratio);
		return vertex;
	}
}
