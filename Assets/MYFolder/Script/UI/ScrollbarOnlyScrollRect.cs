using UnityEngine.EventSystems;
using UnityEngine.UI;

// 목록은 스크롤바로만 조작. 내용 영역의 휠/드래그는 다른 UI로 넘기지 않음
public class ScrollbarOnlyScrollRect : ScrollRect
{
	public override void OnScroll(PointerEventData _eventData) { }
	public override void OnBeginDrag(PointerEventData _eventData) { }
	public override void OnDrag(PointerEventData _eventData) { }
	public override void OnEndDrag(PointerEventData _eventData) { }
}
