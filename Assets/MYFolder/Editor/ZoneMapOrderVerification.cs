using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 저장된 두 존의 UI를 함께 켜서 지도 순서와 클릭 우선순위를 확인함
public static class ZoneMapOrderVerification
{
	const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

	public static void Run()
	{
		Verify("TownScene");
		Verify("BeginnerZoneScene");
		Debug.Log("[MapOrderTest] COMPLETE (no scene saves or network connections)");
	}

	static void Check(bool _condition, string _message)
	{
		if (!_condition) throw new Exception(_message);
		Debug.Log("[MapOrderTest] PASS " + _message);
	}

	static void Verify(string _sceneName)
	{
		EditorSceneManager.OpenScene("Assets/MYFolder/Scenes/" + _sceneName + ".unity");
		ZoneMapUI map = UnityEngine.Object.FindObjectOfType<ZoneMapUI>(true);
		Check(map != null, _sceneName + " shared ZoneMapUI exists");
		Canvas mapCanvas = map.GetComponentInParent<Canvas>();
		mapCanvas.gameObject.SetActive(true);
		map.gameObject.SetActive(true);
		ZoneGameplayUI gameplay = UnityEngine.Object.FindObjectOfType<ZoneGameplayUI>(true);
		gameplay.gameObject.SetActive(true);
		gameplay.SetLoading(false);
		ZoneChannelUI channel = UnityEngine.Object.FindObjectOfType<ZoneChannelUI>(true);
		channel.Open();
		int smallOrder = mapCanvas.sortingOrder;
		typeof(ZoneMapUI).GetMethod("Awake", Flags).Invoke(map, null);
		Button toggle = (Button)typeof(ZoneMapUI).GetField("m_toggleButton", Flags).GetValue(map);
		RectTransform panel = (RectTransform)typeof(ZoneMapUI).GetField("m_panel", Flags).GetValue(map);
		Vector2 smallSize = panel.sizeDelta;
		Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
		for (int repeat = 0; repeat < 20; ++repeat)
		{
			toggle.onClick.Invoke();
			for (int i = 0; i < canvases.Length; ++i)
			{
				Canvas other = canvases[i];
				if (other == mapCanvas || other.renderMode == RenderMode.WorldSpace || !other.isRootCanvas) continue;
				if (mapCanvas.sortingOrder <= other.sortingOrder) throw new Exception(_sceneName + " map behind " + other.name);
			}
			toggle.onClick.Invoke();
			if (mapCanvas.sortingOrder != smallOrder || panel.sizeDelta != smallSize) throw new Exception("collapsed map changed");
		}
		Check(true, _sceneName + " 20 expand/collapse cycles: topmost then original order and size");
		toggle.onClick.Invoke();

		Camera camera = Camera.main;
		RenderTexture render = new RenderTexture(1920, 1080, 24);
		camera.targetTexture = render;
		camera.aspect = 16.0f / 9.0f;
		camera.cullingMask = ~0;
		for (int i = 0; i < canvases.Length; ++i)
		{
			if (canvases[i].renderMode == RenderMode.WorldSpace) continue;
			canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
			canvases[i].worldCamera = camera;
			canvases[i].planeDistance = 1.0f;
		}
		Canvas.ForceUpdateCanvases();
		// Edit Mode에서는 BaseRaycaster의 OnEnable이 실행되지 않으므로 테스트에서 등록
		GraphicRaycaster[] raycasters = UnityEngine.Object.FindObjectsOfType<GraphicRaycaster>();
		for (int i = 0; i < raycasters.Length; ++i)
			typeof(BaseRaycaster).GetMethod("OnEnable", Flags).Invoke(raycasters[i], null);
		EventSystem events = UnityEngine.Object.FindObjectOfType<EventSystem>();
		if (events == null) events = new GameObject("MapTestEventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
		camera.Render();
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = render;
		Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
		image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
		image.Apply();
		Directory.CreateDirectory("Build/Validation/MapOrder");
		File.WriteAllBytes("Build/Validation/MapOrder/" + _sceneName + ".png", image.EncodeToPNG());
		CheckTopHit(events, camera, panel, panel, _sceneName + " map receives input above other UI");
		CheckTopHit(events, camera, (RectTransform)toggle.transform, (RectTransform)toggle.transform, _sceneName + " collapse button remains clickable");
		RenderTexture.active = previous;
		camera.targetTexture = null;
		UnityEngine.Object.DestroyImmediate(image);
		UnityEngine.Object.DestroyImmediate(render);
	}

	static void CheckTopHit(EventSystem _events, Camera _camera, RectTransform _point, RectTransform _expected, string _message)
	{
		Vector3 world = _point.TransformPoint(_point.rect.center);
		PointerEventData pointer = new PointerEventData(_events);
		pointer.position = RectTransformUtility.WorldToScreenPoint(_camera, world);
		List<RaycastResult> hits = new List<RaycastResult>();
		_events.RaycastAll(pointer, hits);
		Debug.Log("[MapOrderTest] pointer=" + pointer.position + " screen=" + Screen.width + "x" + Screen.height + " display=" + Display.activeEditorGameViewTarget + " hits=" + hits.Count);
		for (int i = 0; i < hits.Count; ++i) Debug.Log("[MapOrderTest] hit=" + hits[i].gameObject.name + " order=" + hits[i].sortingOrder);
		Check(hits.Count > 0 && hits[0].gameObject.transform.IsChildOf(_expected), _message);
	}
}
