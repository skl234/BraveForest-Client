using UnityEngine;
using UnityEngine.UI;

public enum eUIFrame
{
	Panel,
	Button,
	Gauge,
	Badge,
	Grip,
	Marker,
}

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasRenderer))]
public class UIFrameGraphic : MaskableGraphic
{
	[SerializeField] eUIFrame m_style;
	float m_emphasis;

	public float GetCornerCut()
	{
		if (m_style == eUIFrame.Panel) return 9;
		if (m_style == eUIFrame.Button) return 6;
		if (m_style == eUIFrame.Gauge) return 2.8f;
		return 0;
	}

	public void Setup(eUIFrame _style)
	{
		m_style = _style;
		raycastTarget = false;
		SetVerticesDirty();
	}

	public void SetEmphasis(float _value)
	{
		if (Mathf.Approximately(m_emphasis, _value)) return;
		m_emphasis = _value;
		SetVerticesDirty();
	}

	protected override void OnPopulateMesh(VertexHelper _mesh)
	{
		_mesh.Clear();
		Rect rect = GetPixelAdjustedRect();
		if (rect.width < 4 || rect.height < 4) return;
		Color gold = new Color(.84f, .69f, .035f);
		Color light = new Color(1, .95f, .37f);
		if (m_emphasis > 0)
		{
			gold = Color.Lerp(gold, new Color(1, .95f, .35f), m_emphasis);
			light = Color.Lerp(light, Color.white, m_emphasis * .6f);
		}
		if (m_emphasis < 0)
		{
			gold = Color.Lerp(gold, new Color(.37f, .39f, .29f), -m_emphasis);
			light = Color.Lerp(light, new Color(.5f, .52f, .41f), -m_emphasis);
		}
		if (m_style == eUIFrame.Badge)
		{
			Badge(_mesh, rect);
			return;
		}
		if (m_style == eUIFrame.Grip)
		{
			Diamond(_mesh, rect.center, 8, gold, light);
			Line(_mesh, rect.center + new Vector2(-25, 0), rect.center + new Vector2(-9, 0), 2, gold);
			Line(_mesh, rect.center + new Vector2(9, 0), rect.center + new Vector2(25, 0), 2, gold);
			return;
		}
		if (m_style == eUIFrame.Gauge)
		{
			Ring(_mesh, rect, 1.8f, GetCornerCut(), new Color(0, .025f, .015f, .95f));
			Ring(_mesh, Inset(rect, 1), .8f, 2, new Color(.27f, .39f, .16f));
			Line(_mesh, new Vector2(rect.xMin + 3, rect.yMax - 1), new Vector2(rect.xMax - 3, rect.yMax - 1), .6f, new Color(.7f, .76f, .38f, .55f));
			return;
		}
		if (m_style == eUIFrame.Marker)
		{
			Ring(_mesh, rect, 2, 0, new Color(1, .84f, .02f));
			return;
		}
		float corner = 8;
		if (m_style == eUIFrame.Button) corner = 5;
		Ring(_mesh, rect, 4, GetCornerCut(), new Color(.008f, .014f, .002f, .95f));
		Ring(_mesh, Inset(rect, 1), 1.65f, corner, gold);
		Ring(_mesh, Inset(rect, 3.3f), .85f, corner - 1, new Color(.33f, .40f, .065f, .95f));
		Ring(_mesh, Inset(rect, 4.6f), .55f, corner - 1, new Color(.01f, .025f, .018f, .85f));
		Line(_mesh, new Vector2(rect.xMin + corner + 3, rect.yMax - 1), new Vector2(rect.xMax - corner - 3, rect.yMax - 1), .65f, new Color(light.r, light.g, light.b, .65f));
		Line(_mesh, new Vector2(rect.xMin + corner + 3, rect.yMin + 2), new Vector2(rect.xMax - corner - 3, rect.yMin + 2), .55f, new Color(.9f, .77f, .08f, .8f));
		if (m_style != eUIFrame.Panel || rect.height < 38) return;
		Corner(_mesh, new Vector2(rect.xMin, rect.yMax), new Vector2(1, -1), gold, light);
		Corner(_mesh, new Vector2(rect.xMax, rect.yMax), new Vector2(-1, -1), gold, light);
		Corner(_mesh, new Vector2(rect.xMin, rect.yMin), new Vector2(1, 1), gold, light);
		Corner(_mesh, new Vector2(rect.xMax, rect.yMin), new Vector2(-1, 1), gold, light);
		if (rect.width > 160)
		{
			float[] positions = { .16f, .34f, .66f, .84f };
			for (int i = 0; i < positions.Length; ++i)
			{
				float x = Mathf.Lerp(rect.xMin, rect.xMax, positions[i]);
				Line(_mesh, new Vector2(x, rect.yMax - 1), new Vector2(x + 9, rect.yMax - 1), 1.1f, light);
			}
		}
	}

	static Rect Inset(Rect _rect, float _size)
	{
		return new Rect(_rect.x + _size, _rect.y + _size, _rect.width - _size * 2, _rect.height - _size * 2);
	}

	static void Corner(VertexHelper _mesh, Vector2 _origin, Vector2 _sign, Color _gold, Color _light)
	{
		Vector2[] points = { new Vector2(3, 18), new Vector2(3, 9), new Vector2(9, 3), new Vector2(19, 3), new Vector2(19, 5), new Vector2(11, 5), new Vector2(6, 10), new Vector2(6, 14), new Vector2(10, 14), new Vector2(10, 10), new Vector2(8, 10) };
		for (int i = 0; i < points.Length - 1; ++i)
			Line(_mesh, _origin + Vector2.Scale(points[i], _sign), _origin + Vector2.Scale(points[i + 1], _sign), 1, _gold);
		Line(_mesh, _origin + Vector2.Scale(new Vector2(9, 3), _sign), _origin + Vector2.Scale(new Vector2(14, 3), _sign), 1, _light);
	}

	static void Diamond(VertexHelper _mesh, Vector2 _center, float _radius, Color _gold, Color _light)
	{
		Vector2[] points = { _center + new Vector2(0, _radius), _center + new Vector2(_radius, 0), _center + new Vector2(0, -_radius), _center + new Vector2(-_radius, 0) };
		int start = _mesh.currentVertCount;
		for (int i = 0; i < points.Length; ++i) _mesh.AddVert(points[i], new Color(.005f, .055f, .025f), Vector2.zero);
		_mesh.AddTriangle(start, start + 1, start + 2); _mesh.AddTriangle(start, start + 2, start + 3);
		for (int i = 0; i < points.Length; ++i)
		{
			Line(_mesh, points[i], points[(i + 1) % 4], 1.8f, _gold);
			Line(_mesh, _center + (points[i] - _center) * .47f, _center + (points[(i + 1) % 4] - _center) * .47f, 1, _light);
		}
	}

	static void Badge(VertexHelper _mesh, Rect _rect)
	{
		const int count = 32;
		Vector2 center = _rect.center;
		float radius = Mathf.Min(_rect.width, _rect.height) * .5f - .7f;
		_mesh.AddVert(center, new Color(.94f, .015f, .055f), Vector2.zero);
		for (int i = 0; i <= count; ++i)
		{
			float angle = Mathf.PI * 2 * i / count;
			Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
			Color color = Color.Lerp(new Color(.34f, .005f, .02f), new Color(1, .12f, .19f), (Mathf.Sin(angle) + 1) * .5f);
			_mesh.AddVert(point, color, Vector2.zero);
			if (i > 0) _mesh.AddTriangle(0, i, i + 1);
		}
		for (int i = 0; i < count; ++i)
		{
			float a = Mathf.PI * 2 * i / count;
			float b = Mathf.PI * 2 * (i + 1) / count;
			Line(_mesh, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, 1.4f, new Color(.14f, .025f, .005f));
		}
	}

	static void Ring(VertexHelper _mesh, Rect _rect, float _width, float _cut, Color _color)
	{
		if (_rect.width <= 0 || _rect.height <= 0) return;
		float width = Mathf.Min(_width, Mathf.Min(_rect.width, _rect.height) * .5f);
		float cut = Mathf.Min(_cut, Mathf.Min(_rect.width, _rect.height) * .3f);
		Rect innerRect = Inset(_rect, width);
		float innerCut = Mathf.Max(0, cut - (2 - Mathf.Sqrt(2)) * width);
		innerCut = Mathf.Min(innerCut, Mathf.Min(innerRect.width, innerRect.height) * .5f);
		Vector2[] outer = Points(_rect, cut);
		Vector2[] inner = Points(innerRect, innerCut);
		int start = _mesh.currentVertCount;
		for (int i = 0; i < outer.Length; ++i)
		{
			_mesh.AddVert(outer[i], _color, Vector2.zero);
			_mesh.AddVert(inner[i], _color, Vector2.zero);
		}
		// 모서리를 같은 꼭짓점으로 연결
		for (int i = 0; i < outer.Length; ++i)
		{
			int current = start + i * 2;
			int next = start + (i + 1) % outer.Length * 2;
			_mesh.AddTriangle(current, next, next + 1);
			_mesh.AddTriangle(current, next + 1, current + 1);
		}
	}

	static Vector2[] Points(Rect _rect, float _cut)
	{
		return new[] { new Vector2(_rect.xMin + _cut, _rect.yMin), new Vector2(_rect.xMax - _cut, _rect.yMin), new Vector2(_rect.xMax, _rect.yMin + _cut), new Vector2(_rect.xMax, _rect.yMax - _cut), new Vector2(_rect.xMax - _cut, _rect.yMax), new Vector2(_rect.xMin + _cut, _rect.yMax), new Vector2(_rect.xMin, _rect.yMax - _cut), new Vector2(_rect.xMin, _rect.yMin + _cut) };
	}

	static void Line(VertexHelper _mesh, Vector2 _a, Vector2 _b, float _width, Color _color)
	{
		Vector2 normal = new Vector2(-(_b - _a).y, (_b - _a).x).normalized * _width * .5f;
		int start = _mesh.currentVertCount;
		_mesh.AddVert(_a - normal, _color, Vector2.zero); _mesh.AddVert(_b - normal, _color, Vector2.zero);
		_mesh.AddVert(_b + normal, _color, Vector2.zero); _mesh.AddVert(_a + normal, _color, Vector2.zero);
		_mesh.AddTriangle(start, start + 1, start + 2); _mesh.AddTriangle(start, start + 2, start + 3);
	}
}
