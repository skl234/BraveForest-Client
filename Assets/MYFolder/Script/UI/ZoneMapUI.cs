using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneMapUI : MonoBehaviour
{
	[SerializeField] RectTransform m_panel;
	[SerializeField] RawImage m_mapImage;
	[SerializeField] RectTransform m_playerMarker;
	[SerializeField] RectTransform[] m_portalMarkerList;
	[SerializeField] Vector2[] m_portalPositionList;
	[SerializeField] TMP_Text m_zoneName;
	[SerializeField] Button m_toggleButton;
	[SerializeField] Vector2 m_smallSize = new Vector2(230.0f, 230.0f);
	[SerializeField] Vector2 m_largeSize = new Vector2(720.0f, 720.0f);
	[SerializeField] float m_smallWorldView = 0.28f;
	[SerializeField] int m_expandedSortingOrder = 100;
	Canvas m_canvas;
	int m_smallSortingOrder;
	ZoneSceneInfo m_sceneInfo;
	bool m_expanded;

	void Awake()
	{
		m_canvas = GetComponentInParent<Canvas>();
		if (m_canvas != null) m_smallSortingOrder = m_canvas.sortingOrder;
		m_sceneInfo = FindObjectOfType<ZoneSceneInfo>();
		if (m_sceneInfo != null)
		{
			m_mapImage.texture = m_sceneInfo.GetMapImage();
			m_zoneName.text = m_sceneInfo.GetZoneName();
		}
		m_toggleButton.onClick.AddListener(Toggle);
		ApplySize();
	}

	void OnDestroy() { if (m_toggleButton != null) m_toggleButton.onClick.RemoveListener(Toggle); }

	void LateUpdate()
	{
		if (m_sceneInfo == null || ZoneSceneNetwork.Instance == null) return;
		LocalPlayerObject player = ZoneSceneNetwork.Instance.GetLocalPlayer();
		if (player == null) return;
		if (!m_playerMarker.gameObject.activeSelf) m_playerMarker.gameObject.SetActive(true);
		Vector2 min = m_sceneInfo.GetWorldMin();
		Vector2 max = m_sceneInfo.GetWorldMax();
		float x = Mathf.InverseLerp(min.x, max.x, player.transform.position.x);
		float y = Mathf.InverseLerp(min.y, max.y, player.transform.position.z);
		Rect view = GetMapView(new Vector2(x, y));
		m_mapImage.uvRect = view;
		UpdateMarker(m_playerMarker, new Vector2(x, y), view);
		for (int i = 0; i < m_portalMarkerList.Length; ++i)
		{
			UpdateMarker(m_portalMarkerList[i], m_portalPositionList[i], view);
		}
	}

	void Toggle() { m_expanded = !m_expanded; ApplySize(); }
	public void Collapse() { m_expanded = false; ApplySize(); }
	void ApplySize()
	{
		m_panel.sizeDelta = m_expanded ? m_largeSize : m_smallSize;
		m_zoneName.gameObject.SetActive(m_expanded);
		if (m_expanded)
		{
			// 펼친 지도는 채팅/스킬창보다 위에 표시
			if (m_canvas != null) m_canvas.sortingOrder = Mathf.Max(m_smallSortingOrder, m_expandedSortingOrder);
			m_mapImage.rectTransform.offsetMax = new Vector2(-8.0f, -58.0f);
		}
		else
		{
			if (m_canvas != null) m_canvas.sortingOrder = m_smallSortingOrder;
			m_mapImage.rectTransform.offsetMax = new Vector2(-8.0f, -8.0f);
		}
	}

	Rect GetMapView(Vector2 _playerPosition)
	{
		if (m_expanded) return new Rect(0.0f, 0.0f, 1.0f, 1.0f);
		float x = Mathf.Clamp(_playerPosition.x - m_smallWorldView * 0.5f, 0.0f, 1.0f - m_smallWorldView);
		float y = Mathf.Clamp(_playerPosition.y - m_smallWorldView * 0.5f, 0.0f, 1.0f - m_smallWorldView);
		return new Rect(x, y, m_smallWorldView, m_smallWorldView);
	}

	void UpdateMarker(RectTransform _marker, Vector2 _position, Rect _view)
	{
		if (_marker == null) return;
		float x = (_position.x - _view.xMin) / _view.width;
		float y = (_position.y - _view.yMin) / _view.height;
		bool visible = x >= 0.0f && x <= 1.0f && y >= 0.0f && y <= 1.0f;
		_marker.gameObject.SetActive(visible);
		if (!visible) return;
		Rect rect = m_mapImage.rectTransform.rect;
		_marker.anchoredPosition = new Vector2((x - 0.5f) * rect.width, (y - 0.5f) * rect.height);
	}
}
