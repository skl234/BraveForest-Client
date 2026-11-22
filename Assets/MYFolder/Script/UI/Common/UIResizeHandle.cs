using UnityEngine;
using UnityEngine.EventSystems;

public class UIResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[SerializeField] RectTransform m_panel;
	[SerializeField] float m_minHeight = 160;
	[SerializeField] float m_maxHeight = 420;
	Vector2 m_start;
	float m_height;

	public void OnBeginDrag(PointerEventData _event)
	{
		RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_panel.parent, _event.pressPosition, _event.pressEventCamera, out m_start);
		m_height = m_panel.rect.height;
		UIInputBlocker.BlockCurrentFrame();
	}
	public void OnDrag(PointerEventData _event)
	{
		Vector2 position;
		RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)m_panel.parent, _event.position, _event.pressEventCamera, out position);
		SetHeight(m_height + position.y - m_start.y);
		UIInputBlocker.BlockCurrentFrame();
	}
	public void OnEndDrag(PointerEventData _event) { UIInputBlocker.BlockCurrentFrame(); }
	public void SetHeight(float _height)
	{
		m_panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(_height, m_minHeight, m_maxHeight));
	}
}
