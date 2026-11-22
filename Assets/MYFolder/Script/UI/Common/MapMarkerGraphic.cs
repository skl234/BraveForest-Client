using UnityEngine;
using UnityEngine.UI;

public enum eMapMarkerType
{
	Player,
	Portal,
}

[RequireComponent(typeof(CanvasRenderer))]
public class MapMarkerGraphic : Image
{
	[SerializeField] eMapMarkerType m_type;

	public void Setup(eMapMarkerType _type, Sprite _sprite = null)
	{
		m_type = _type;
		sprite = _sprite;
		preserveAspect = true;
		raycastTarget = false;
		SetVerticesDirty();
	}

	protected override void OnPopulateMesh(VertexHelper _mesh)
	{
		if (m_type == eMapMarkerType.Portal)
		{
			base.OnPopulateMesh(_mesh);
			return;
		}

		_mesh.Clear();
		Rect rect = GetPixelAdjustedRect();
		Vector2 center = rect.center;
		float radius = Mathf.Min(rect.width, rect.height) * .5f;
		Disc(_mesh, center, radius * .9f, new Color(.04f, .2f, .19f, .9f));
		Disc(_mesh, center, radius * .74f, new Color(.9f, 1, .98f));
		Disc(_mesh, center, radius * .52f, new Color(.08f, .82f, .64f));
	}

	static void Disc(VertexHelper _mesh, Vector2 _center, float _radius, Color _color)
	{
		for (int i = 0; i < 48; ++i)
		{
			float a = i * Mathf.PI * 2 / 48;
			float b = (i + 1) * Mathf.PI * 2 / 48;
			Triangle(_mesh, _center, _center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * _radius, _center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * _radius, _color);
		}
	}

	static void Triangle(VertexHelper _mesh, Vector2 _a, Vector2 _b, Vector2 _c, Color _color)
	{
		int index = _mesh.currentVertCount;
		_mesh.AddVert(_a, _color, Vector2.zero);
		_mesh.AddVert(_b, _color, Vector2.zero);
		_mesh.AddVert(_c, _color, Vector2.zero);
		_mesh.AddTriangle(index, index + 1, index + 2);
	}
}
