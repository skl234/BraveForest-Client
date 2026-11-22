using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class UICanvasOrder : MonoBehaviour
{
	[SerializeField] int m_order;
	public int GetOrder() { return m_order; }
	public void Setup(int _order) { m_order = _order; Apply(); }
	void OnEnable() { Apply(); }
	void OnTransformParentChanged() { Apply(); }
	void Apply()
	{
		Canvas canvas = GetComponent<Canvas>();
		canvas.overrideSorting = true;
		canvas.sortingOrder = m_order;
	}
}
