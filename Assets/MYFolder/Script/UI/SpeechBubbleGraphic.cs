using UnityEngine;
using UnityEngine.UI;

// 해상도에 따라 늘어나도 모서리와 꼬리 모양은 유지
[RequireComponent(typeof(CanvasRenderer))]
public class SpeechBubbleGraphic : MaskableGraphic
{
	[SerializeField] float m_radius = 12.0f;
	[SerializeField] Color m_border = new Color(.23f, .25f, .24f, 1.0f);

	protected override void OnPopulateMesh(VertexHelper _mesh)
	{
		_mesh.Clear();
		Rect rect = rectTransform.rect;
		Rect shadow = rect;
		shadow.position += new Vector2(2.0f, -3.0f);
		DrawBubble(_mesh, shadow, m_radius, new Color(0, 0, 0, .22f), 0);
		DrawBubble(_mesh, rect, m_radius, m_border, 0);
		Rect inner = new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width - 3, rect.height - 3);
		DrawBubble(_mesh, inner, m_radius - 1.5f, color, 1.5f);
	}

	void DrawBubble(VertexHelper _mesh, Rect _rect, float _radius, Color _color, float _inset)
	{
		float radius = Mathf.Min(_radius, Mathf.Min(_rect.width, _rect.height) * .5f);
		int center = _mesh.currentVertCount;
		_mesh.AddVert(_rect.center, _color, Vector2.zero);
		for (int corner = 0; corner < 4; ++corner)
		{
			Vector2 point = _rect.center;
			if (corner < 2) point.x = _rect.xMax - radius;
			else point.x = _rect.xMin + radius;
			if (corner == 1 || corner == 2) point.y = _rect.yMax - radius;
			else point.y = _rect.yMin + radius;
			for (int step = 0; step <= 6; ++step)
			{
				float angle = (-90 + corner * 90 + step * 15) * Mathf.Deg2Rad;
				Vector2 vertex = point + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
				_mesh.AddVert(vertex, _color, Vector2.zero);
			}
		}
		for (int i = 0; i < 28; ++i) _mesh.AddTriangle(center, center + 1 + i, center + 1 + (i + 1) % 28);
		int tail = _mesh.currentVertCount;
		float x = _rect.center.x - 12;
		_mesh.AddVert(new Vector2(x - 9 + _inset, _rect.yMin + 2), _color, Vector2.zero);
		_mesh.AddVert(new Vector2(x + 9 - _inset, _rect.yMin + 2), _color, Vector2.zero);
		_mesh.AddVert(new Vector2(x - 2, _rect.yMin - 10 + _inset), _color, Vector2.zero);
		_mesh.AddTriangle(tail, tail + 1, tail + 2);
	}
}
