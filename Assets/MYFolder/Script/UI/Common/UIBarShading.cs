using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class UIBarShading : BaseMeshEffect
{
	readonly List<UIVertex> m_vertices = new List<UIVertex>();
	static readonly float[] Position = { 0, .16f, .48f, .78f, .92f, 1 };
	static readonly float[] Brightness = { .38f, .76f, 1, 1.03f, 1.25f, .82f };

	public override void ModifyMesh(VertexHelper _mesh)
	{
		if (!IsActive() || _mesh.currentVertCount == 0) return;
		m_vertices.Clear();
		_mesh.GetUIVertexStream(m_vertices);
		Vector2 min = m_vertices[0].position;
		Vector2 max = min;
		for (int i = 1; i < m_vertices.Count; ++i)
		{
			min = Vector2.Min(min, m_vertices[i].position);
			max = Vector2.Max(max, m_vertices[i].position);
		}
		if (max.x - min.x < .1f) return;
		Color color = m_vertices[0].color;
		Vector2 uv = m_vertices[0].uv0;
		_mesh.Clear();
		for (int i = 0; i < Position.Length - 1; ++i)
		{
			float bottom = Mathf.Lerp(min.y, max.y, Position[i]);
			float top = Mathf.Lerp(min.y, max.y, Position[i + 1]);
			float bottomCut = 0; float topCut = 0;
			if (i == 0) bottomCut = Mathf.Min(2, (max.x - min.x) * .2f);
			if (i == Position.Length - 2) topCut = Mathf.Min(2, (max.x - min.x) * .2f);
			int index = _mesh.currentVertCount;
			_mesh.AddVert(new Vector2(min.x + bottomCut, bottom), Shade(color, Brightness[i]), uv);
			_mesh.AddVert(new Vector2(max.x - bottomCut, bottom), Shade(color, Brightness[i]), uv);
			_mesh.AddVert(new Vector2(max.x - topCut, top), Shade(color, Brightness[i + 1]), uv);
			_mesh.AddVert(new Vector2(min.x + topCut, top), Shade(color, Brightness[i + 1]), uv);
			_mesh.AddTriangle(index, index + 1, index + 2); _mesh.AddTriangle(index, index + 2, index + 3);
		}
	}

	static Color Shade(Color _color, float _brightness)
	{
		Color value = _color * _brightness;
		value.a = _color.a;
		if (_brightness > 1) value = Color.Lerp(_color, new Color(1, 1, 1, _color.a), (_brightness - 1) * .7f);
		return value;
	}
}
