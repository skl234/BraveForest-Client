using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneMapUI : MonoBehaviour
{
	[SerializeField] RectTransform m_panel;
	[SerializeField] RawImage m_mapImage;
	[SerializeField] RectTransform m_playerMarker;
	[SerializeField] RectTransform[] m_portalMarkerList;
	Vector2[] m_portalPositionList = new Vector2[0];
	[SerializeField] TMP_Text m_zoneName;
	[SerializeField] Button m_toggleButton;
	[SerializeField] Vector2 m_smallSize = new Vector2(230.0f, 230.0f);
	[SerializeField] Vector2 m_largeSize = new Vector2(720.0f, 720.0f);
	[SerializeField] float m_smallWorldView = 0.28f;
	[SerializeField] int m_expandedSortingOrder = 100;
	Canvas m_canvas;
	TMP_Text m_toggleText;
	int m_smallSortingOrder;
	ZoneSceneInfo m_sceneInfo;
	bool m_expanded;
	Vector2 m_smallPosition;
	Vector2 m_smallAnchor;
	Vector2 m_smallPivot;
	Vector3 m_playerPosition;
	bool m_hasPosition;

	void Awake()
	{
		m_smallPosition = m_panel.anchoredPosition;
		m_smallAnchor = m_panel.anchorMin;
		m_smallPivot = m_panel.pivot;
		m_toggleText = m_toggleButton.GetComponentInChildren<TMP_Text>(true);
		m_canvas = GetComponentInParent<Canvas>();
		if (m_canvas != null) m_smallSortingOrder = m_canvas.sortingOrder;
		m_toggleButton.onClick.AddListener(Toggle);
		m_playerMarker.localRotation = Quaternion.identity;
	}

	void Start()
	{
		// Awake에서는 아직 씬 로딩이 끝나지 않아 씬 정보를 찾을 수 없음.
		m_sceneInfo = ZoneSceneInfo.Find(gameObject.scene);
		if (m_sceneInfo != null)
		{
			m_mapImage.texture = m_sceneInfo.GetMapImage();
			m_zoneName.text = m_sceneInfo.GetZoneName();
		}
		InitializePortals();
		ApplySize();
	}

	public void InitializePortals()
	{
		ZoneSceneInfo info = ZoneSceneInfo.Find(gameObject.scene);
		if (info == null) return;
		ZonePortal[] portals = ZoneSceneInfo.GetSceneObjects<ZonePortal>(gameObject.scene);
		for (int i = 0; i < portals.Length; ++i)
		{
			for (int j = i + 1; j < portals.Length; ++j)
			{
				if (portals[i].GetPortalId() <= portals[j].GetPortalId()) continue;
				ZonePortal portal = portals[i];
				portals[i] = portals[j];
				portals[j] = portal;
			}
		}

		if (m_portalMarkerList == null) m_portalMarkerList = new RectTransform[0];
		if (portals.Length > m_portalMarkerList.Length)
		{
			if (m_portalMarkerList.Length == 0 || m_portalMarkerList[0] == null)
			{
				Debug.LogError("[ZoneMapUI] 포탈 마커 원본이 없습니다.", this);
				return;
			}
			int count = m_portalMarkerList.Length;
			System.Array.Resize(ref m_portalMarkerList, portals.Length);
			for (int i = count; i < portals.Length; ++i)
				m_portalMarkerList[i] = Instantiate(m_portalMarkerList[0], m_portalMarkerList[0].parent);
		}

		m_portalPositionList = new Vector2[portals.Length];
		Vector2 min = info.GetWorldMin();
		Vector2 max = info.GetWorldMax();
		for (int i = 0; i < portals.Length; ++i)
		{
			Vector3 position = portals[i].transform.position;
			m_portalPositionList[i] = new Vector2(Mathf.InverseLerp(min.x, max.x, position.x), Mathf.InverseLerp(min.y, max.y, position.z));
			Image marker = m_portalMarkerList[i].GetComponentInChildren<Image>(true);
			if (marker != null)
			{
				marker.color = Color.white;
				if (!portals[i].IsActive()) marker.color = new Color(.5f, .55f, .55f, .85f);
			}
		}
		for (int i = portals.Length; i < m_portalMarkerList.Length; ++i)
			if (m_portalMarkerList[i] != null) m_portalMarkerList[i].gameObject.SetActive(false);
	}

	void OnDestroy() { if (m_toggleButton != null) m_toggleButton.onClick.RemoveListener(Toggle); }

	void LateUpdate()
	{
		if (ZoneSceneNetwork.Instance != null)
		{
			LocalPlayerObject player = ZoneSceneNetwork.Instance.GetLocalPlayer();
			if (player != null)
			{
				SetPlayerPosition(player.transform.position);
				return;
			}
		}
		if (m_hasPosition) RefreshMap();
	}

	public void SetPlayerPosition(Vector3 _position)
	{
		m_playerPosition = _position;
		m_hasPosition = true;
		RefreshMap();
	}

	void RefreshMap()
	{
		if (m_sceneInfo == null || !m_hasPosition) return;
		if (!m_playerMarker.gameObject.activeSelf) m_playerMarker.gameObject.SetActive(true);
		Vector2 min = m_sceneInfo.GetWorldMin();
		Vector2 max = m_sceneInfo.GetWorldMax();
		float x = Mathf.InverseLerp(min.x, max.x, m_playerPosition.x);
		float y = Mathf.InverseLerp(min.y, max.y, m_playerPosition.z);
		Rect view = GetMapView(new Vector2(x, y));
		m_mapImage.uvRect = view;
		UpdateMarker(m_playerMarker, new Vector2(x, y), view);
		for (int i = 0; i < m_portalMarkerList.Length && i < m_portalPositionList.Length; ++i)
		{
			UpdateMarker(m_portalMarkerList[i], m_portalPositionList[i], view);
		}
	}

	void Toggle() { m_expanded = !m_expanded; ApplySize(); }
	public void Collapse() { m_expanded = false; ApplySize(); }
	void ApplySize()
	{
		float padding = 8;
		float top = 36;
		m_zoneName.gameObject.SetActive(true);
		if (m_expanded)
		{
			Vector2 imageSize = new Vector2(m_largeSize.x - padding * 2, m_largeSize.y - top - padding);
			if (m_mapImage.texture != null)
			{
				float ratio = (float)m_mapImage.texture.width / m_mapImage.texture.height;
				imageSize.x = Mathf.Min(imageSize.x, imageSize.y * ratio);
				imageSize.y = imageSize.x / ratio;
			}
			m_panel.anchorMin = m_panel.anchorMax = new Vector2(.5f, .5f);
			m_panel.pivot = new Vector2(.5f, .5f);
			m_panel.anchoredPosition = Vector2.zero;
			// 지도 비율에 틀을 맞춤. 제목줄 외에는 같은 여백
			m_panel.sizeDelta = imageSize + new Vector2(padding * 2, top + padding);
			if (m_toggleText != null) m_toggleText.text = "-";
			// 펼친 지도는 채팅/스킬창보다 위에 표시
			if (m_canvas != null) m_canvas.sortingOrder = Mathf.Max(m_smallSortingOrder, m_expandedSortingOrder);
		}
		else
		{
			m_panel.anchorMin = m_panel.anchorMax = m_smallAnchor;
			m_panel.pivot = m_smallPivot;
			m_panel.anchoredPosition = m_smallPosition;
			m_panel.sizeDelta = m_smallSize;
			if (m_toggleText != null) m_toggleText.text = "+";
			if (m_canvas != null) m_canvas.sortingOrder = m_smallSortingOrder;
		}
		m_mapImage.rectTransform.anchorMin = Vector2.zero;
		m_mapImage.rectTransform.anchorMax = Vector2.one;
		m_mapImage.rectTransform.offsetMin = new Vector2(padding, padding);
		m_mapImage.rectTransform.offsetMax = new Vector2(-padding, -top);
		m_playerMarker.SetAsLastSibling();
		Vector2 markerSize = new Vector2(24, 24);
		if (m_expanded) markerSize = new Vector2(28, 28);
		m_playerMarker.sizeDelta = new Vector2(14, 14);
		if (m_expanded) m_playerMarker.sizeDelta = new Vector2(16, 16);
		for (int i = 0; i < m_portalMarkerList.Length; ++i)
			if (m_portalMarkerList[i] != null) m_portalMarkerList[i].sizeDelta = markerSize;
		RefreshMap();
	}

	Rect GetMapView(Vector2 _playerPosition)
	{
		if (m_expanded) return new Rect(0.0f, 0.0f, 1.0f, 1.0f);
		Rect rect = m_mapImage.rectTransform.rect;
		float textureRatio = 1;
		if (m_mapImage.texture != null) textureRatio = (float)m_mapImage.texture.width / m_mapImage.texture.height;
		float height = Mathf.Clamp(m_smallWorldView, .01f, 1);
		float width = height * rect.width / Mathf.Max(1, rect.height) / textureRatio;
		if (width > 1)
		{
			height /= width;
			width = 1;
		}
		float x = Mathf.Clamp(_playerPosition.x - width * .5f, 0, 1 - width);
		float y = Mathf.Clamp(_playerPosition.y - height * .5f, 0, 1 - height);
		return new Rect(x, y, width, height);
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
