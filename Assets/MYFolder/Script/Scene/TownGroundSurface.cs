using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshRenderer))]
public class TownGroundSurface : MonoBehaviour
{
	[SerializeField] TownGroundLayout m_layout;
	[SerializeField] Transform m_origin;
	MaterialPropertyBlock m_properties;

	public void Initialize(TownGroundLayout _layout, Transform _origin)
	{
		m_layout = _layout;
		m_origin = _origin;
		Refresh();
	}

	void OnEnable() { Refresh(); }

	public void Refresh()
	{
		if (m_layout == null || m_origin == null) return;
		if (m_properties == null) m_properties = new MaterialPropertyBlock();
		m_layout.Apply(m_properties, m_origin.position);
		GetComponent<MeshRenderer>().SetPropertyBlock(m_properties);
	}
}
