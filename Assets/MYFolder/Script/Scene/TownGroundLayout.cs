using UnityEngine;

[CreateAssetMenu(menuName = "BraveForest/Town Ground Layout")]
public class TownGroundLayout : ScriptableObject
{
	[SerializeField] float m_scale = .82f;
	[SerializeField] Vector4[] m_routes;
	[SerializeField] Vector4[] m_beds;
	[SerializeField] float[] m_bedRadii;
	[SerializeField] Vector4[] m_footings;
	[SerializeField] Vector4[] m_yards = new Vector4[0];

	public void SetCourtyards(Vector4[] _yards)
	{
		m_yards = _yards;
	}

	public void Initialize(Vector4[] _routes, Vector4[] _beds, float[] _radii, Vector4[] _footings)
	{
		m_routes = _routes;
		m_beds = _beds;
		m_bedRadii = _radii;
		m_footings = _footings;
	}

	public void Apply(MaterialPropertyBlock _block, Vector3 _origin)
	{
		_block.SetVector("_LayoutOrigin", _origin);
		_block.SetFloat("_WorldScale", m_scale);
		_block.SetVectorArray("_Routes", m_routes);
		_block.SetInt("_RouteCount", m_routes.Length);
		_block.SetVectorArray("_Beds", m_beds);
		_block.SetFloatArray("_BedRadii", m_bedRadii);
		_block.SetInt("_BedCount", m_beds.Length);
		_block.SetVectorArray("_Footings", m_footings);
		_block.SetInt("_FootingCount", m_footings.Length);
		_block.SetInt("_YardCount", m_yards.Length);
		if (m_yards.Length > 0) _block.SetVectorArray("_Yards", m_yards);
	}
}
