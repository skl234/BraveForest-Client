using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
	[SerializeField] UIFrameGraphic m_frame;
	Button m_button;
	bool m_hover;
	bool m_focus;
	bool m_pressed;
	bool m_selected;
	float m_release;

	void Awake() { m_button = GetComponent<Button>(); }
	void OnDisable() { m_hover = false; m_focus = false; m_pressed = false; m_release = 0; }
	public void SetSelected(bool _selected) { m_selected = _selected; }
	public void OnPointerEnter(PointerEventData _event) { m_hover = true; }
	public void OnPointerExit(PointerEventData _event) { m_hover = false; m_pressed = false; }
	public void OnPointerDown(PointerEventData _event)
	{
		if (_event.button == PointerEventData.InputButton.Left && m_button.IsInteractable()) m_pressed = true;
	}
	public void OnPointerUp(PointerEventData _event)
	{
		if (_event.button != PointerEventData.InputButton.Left) return;
		if (m_pressed && m_hover && m_button.IsInteractable()) m_release = .18f;
		m_pressed = false;
	}
	public void OnSelect(BaseEventData _event) { m_focus = true; }
	public void OnDeselect(BaseEventData _event) { m_focus = false; m_pressed = false; }
	public void OnSubmit(BaseEventData _event) { if (m_button.IsInteractable()) m_release = .18f; }
	void Update()
	{
		if (m_frame == null) return;
		float emphasis = 0;
		if (m_hover || m_focus || m_selected) emphasis = .6f;
		if (m_pressed) emphasis = .2f;
		if (m_release > 0) emphasis = 1;
		if (!m_button.IsInteractable()) emphasis = -.85f;
		m_frame.SetEmphasis(emphasis);
		m_release = Mathf.Max(0, m_release - Time.unscaledDeltaTime);
	}
}
