using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class ChatScrollbarSetup
{
	const string InterfacePath = "Assets/MYFolder/Prefab/UI/ZoneInterface.prefab";
	const string BarPath = "Assets/MYFolder/Prefab/UI/Common/VerticalScrollbar.prefab";
	public static void Run()
	{
		Directory.CreateDirectory("Build/Validation/Backups");
		File.Copy(InterfacePath, "Build/Validation/Backups/ChatScrollbar_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".prefab");
		GameObject root = PrefabUtility.LoadPrefabContents(InterfacePath);
		try
		{
			ZoneChatUI chat = root.GetComponentInChildren<ZoneChatUI>(true);
			ConfigureChat(chat.gameObject);
			ConfigureChannels(root.transform.Find("ChannelUI/ChannelWindow/Panel"));
			PrefabUtility.SaveAsPrefabAsset(root, InterfacePath);
		}
		finally { PrefabUtility.UnloadPrefabContents(root); }
		AssetDatabase.SaveAssets();
		Debug.Log("[ChatScrollbarSetup] PASS shared ZoneInterface chat/channel scrollbars saved");
	}

	public static void ConfigureChannels(Transform _panel)
	{
		RectTransform viewport = (RectTransform)_panel.Find("Viewport");
		ScrollRect previous = viewport.GetComponent<ScrollRect>();
		RectTransform content = previous.content;
		ScrollbarOnlyScrollRect scroll = previous as ScrollbarOnlyScrollRect;
		if (scroll == null)
		{
			UnityEngine.Object.DestroyImmediate(previous);
			scroll = viewport.gameObject.AddComponent<ScrollbarOnlyScrollRect>();
		}
		scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
		scroll.inertia = false; scroll.scrollSensitivity = 0;
		scroll.movementType = ScrollRect.MovementType.Clamped;
		viewport.anchoredPosition = new Vector2(-12, -14); viewport.sizeDelta = new Vector2(448, 310);
		VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
		if (layout != null) layout.childControlWidth = true;
		Transform existing = _panel.Find("ChannelScrollbar");
		GameObject bar;
		if (existing != null) bar = existing.gameObject;
		else bar = (GameObject)PrefabUtility.InstantiatePrefab(GetBarPrefab(), _panel);
		bar.name = "ChannelScrollbar";
		RectTransform rect = (RectTransform)bar.transform;
		rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
		rect.anchoredPosition = new Vector2(226, -14); rect.sizeDelta = new Vector2(20, 310);
		scroll.verticalScrollbar = bar.GetComponent<Scrollbar>();
		scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
		scroll.verticalNormalizedPosition = 1;
		ZoneChannelUI channel = _panel.GetComponentInParent<ZoneChannelUI>();
		SerializedObject data = new SerializedObject(channel);
		data.FindProperty("m_scrollRect").objectReferenceValue = scroll;
		data.ApplyModifiedPropertiesWithoutUndo();
	}

	public static void ConfigureChat(GameObject _root)
	{
		ZoneChatUI chat = _root.GetComponent<ZoneChatUI>();
		ScrollRect previous = _root.GetComponent<ScrollRect>();
		RectTransform viewport = previous.viewport;
		RectTransform content = previous.content;
		ScrollbarOnlyScrollRect scroll = previous as ScrollbarOnlyScrollRect;
		if (scroll == null)
		{
			UnityEngine.Object.DestroyImmediate(previous);
			scroll = _root.AddComponent<ScrollbarOnlyScrollRect>();
		}
		scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true;
		scroll.inertia = false; scroll.scrollSensitivity = 0;
		scroll.movementType = ScrollRect.MovementType.Clamped;
		// 입력창 폭은 그대로 두고, 기록 영역 우측에만 스크롤바 자리를 확보
		viewport.anchorMin = viewport.anchorMax = new Vector2(.5f, .5f);
		viewport.anchoredPosition = new Vector2(-12, 18);
		viewport.sizeDelta = new Vector2(296, 176);
		Image back = viewport.GetComponent<Image>();
		if (back != null) back.raycastTarget = true;
		Transform existing = _root.transform.Find("HistoryScrollbar");
		GameObject bar;
		if (existing != null) bar = existing.gameObject;
		else bar = (GameObject)PrefabUtility.InstantiatePrefab(GetBarPrefab(), _root.transform);
		bar.name = "HistoryScrollbar";
		RectTransform rect = (RectTransform)bar.transform;
		rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
		rect.anchoredPosition = new Vector2(151, 18); rect.sizeDelta = new Vector2(20, 176);
		scroll.verticalScrollbar = bar.GetComponent<Scrollbar>();
		scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
		scroll.verticalNormalizedPosition = 0;
		SerializedObject data = new SerializedObject(chat);
		data.FindProperty("m_scrollRect").objectReferenceValue = scroll;
		data.ApplyModifiedPropertiesWithoutUndo();
	}

	static GameObject GetBarPrefab()
	{
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarPath);
		if (prefab != null) return prefab;
		Directory.CreateDirectory(Path.GetDirectoryName(BarPath)); AssetDatabase.Refresh();
		GameObject root = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
		try
		{
			Image track = root.GetComponent<Image>(); track.color = new Color(.14f, .15f, .17f, 1);
			RectTransform area = new GameObject("SlidingArea", typeof(RectTransform)).GetComponent<RectTransform>(); area.SetParent(root.transform, false);
			area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one; area.offsetMin = new Vector2(3, 3); area.offsetMax = new Vector2(-3, -3);
			RectTransform handle = new GameObject("Handle", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>(); handle.SetParent(area, false);
			handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.offsetMin = handle.offsetMax = Vector2.zero;
			Image image = handle.GetComponent<Image>(); image.color = Color.white;
			Scrollbar scrollbar = root.GetComponent<Scrollbar>(); scrollbar.handleRect = handle; scrollbar.targetGraphic = image;
			scrollbar.direction = Scrollbar.Direction.BottomToTop;
			ColorBlock colors = scrollbar.colors; colors.normalColor = new Color(.67f,.68f,.71f,1); colors.highlightedColor = new Color(.84f,.85f,.88f,1); colors.pressedColor = new Color(.76f,.65f,.45f,1); scrollbar.colors = colors;
			UnityEngine.UI.Navigation navigation = scrollbar.navigation; navigation.mode = UnityEngine.UI.Navigation.Mode.None; scrollbar.navigation = navigation;
			prefab = PrefabUtility.SaveAsPrefabAsset(root, BarPath);
		}
		finally { UnityEngine.Object.DestroyImmediate(root); }
		return prefab;
	}
}
