using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ChatScrollbarVerification
{
	const string Key = "ChatScrollbarVerification.Running";
	const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
	static int m_phase;
	static int m_frame = -1;
	static float m_started;
	static bool m_failed;
	static GameObject m_root;
	static Camera m_camera;
	static ZoneChatUI m_chat;
	static ScrollbarOnlyScrollRect m_history;
	static ScrollbarOnlyScrollRect m_channels;
	static TMP_Text m_text;
	static int m_checks;
	static ChatScrollbarVerification() { if (SessionState.GetBool(Key, false)) Register(); }
	public static void Run()
	{
		ChatScrollbarSetup.Run();
		string[] scenes = { "TownScene", "BeginnerZoneScene" };
		for (int i=0;i<scenes.Length;++i)
		{
			EditorSceneManager.OpenScene("Assets/MYFolder/Scenes/" + scenes[i] + ".unity");
			ZoneChatUI chat = UnityEngine.Object.FindObjectOfType<ZoneChatUI>(true);
			Check(chat != null && chat.GetComponent<ScrollbarOnlyScrollRect>() != null, scenes[i] + " inherits common chat scroll component");
			ZoneChannelUI channel = UnityEngine.Object.FindObjectOfType<ZoneChannelUI>(true);
			Check(channel != null && channel.GetComponentInChildren<ScrollbarOnlyScrollRect>(true).verticalScrollbar != null, scenes[i] + " inherits common channel scrollbar");
		}
		EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
		SessionState.SetBool(Key, true); Register(); EditorApplication.EnterPlaymode();
	}
	static void Register()
	{
		EditorApplication.update -= Tick; EditorApplication.update += Tick;
		EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
		Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
	}
	static void Log(string text, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) m_failed = true; }
	static void Changed(PlayModeStateChange state)
	{
		if (state == PlayModeStateChange.EnteredPlayMode) { m_phase = 0; m_started = Time.time; }
		if (state != PlayModeStateChange.EnteredEditMode) return;
		SessionState.SetBool(Key, false); EditorApplication.Exit(m_failed ? 1 : 0);
	}
	static void Check(bool value, string message)
	{
		if (!value) throw new Exception(message); ++m_checks; Debug.Log("[ChatScrollTest] PASS " + message);
	}
	static void ReadChannels(int count)
	{
		byte[] bytes = new byte[12 + count * 25]; Buffer.BlockCopy(BitConverter.GetBytes((ulong)count), 0, bytes, 4, 8);
		for (int i=0;i<count;++i)
		{
			int offset = 12 + i * 25;
			Buffer.BlockCopy(BitConverter.GetBytes((ulong)i + 1), 0, bytes, offset, 8);
			Buffer.BlockCopy(BitConverter.GetBytes((ulong)i * 3), 0, bytes, offset + 8, 8);
			Buffer.BlockCopy(BitConverter.GetBytes(256UL), 0, bytes, offset + 16, 8);
			bytes[offset + 24] = 1;
		}
		m_root.GetComponentInChildren<ZoneChannelUI>(true).ReadList(bytes, 1);
	}
	static void ScrollTest(ScrollbarOnlyScrollRect scroll, string name)
	{
		Check(scroll.verticalScrollbar != null && scroll.verticalScrollbar.gameObject.activeInHierarchy, name + " visible pre-authored scrollbar");
		scroll.verticalScrollbar.value = .5f; Canvas.ForceUpdateCanvases();
		Check(Mathf.Abs(scroll.verticalNormalizedPosition - .5f) < .02f, name + " scrollbar controls list position");
		PointerEventData data = new PointerEventData(EventSystem.current); data.scrollDelta = new Vector2(0, 10);
		ExecuteEvents.Execute(scroll.gameObject, data, ExecuteEvents.scrollHandler);
		Check(Mathf.Abs(scroll.verticalNormalizedPosition - .5f) < .02f, name + " mouse wheel does not scroll");
		Scrollbar bar = scroll.verticalScrollbar;
		data.button = PointerEventData.InputButton.Left;
		data.pointerPressRaycast = new RaycastResult { module = scroll.GetComponentInParent<GraphicRaycaster>() };
		data.position = RectTransformUtility.WorldToScreenPoint(m_camera, bar.handleRect.position);
		bar.OnBeginDrag(data);
		data.position += Vector2.up * 700;
		bar.OnDrag(data); bar.OnPointerUp(data);
		Check(scroll.verticalNormalizedPosition > .95f, name + " actual scrollbar drag moves to top");
	}
	static void Capture()
	{
		RenderTexture target = new RenderTexture(1920,1080,24); m_camera.targetTexture = target; m_camera.Render(); RenderTexture.active = target;
		Texture2D image = new Texture2D(1920,1080,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
		File.WriteAllBytes("Build/Validation/ChatChannelScrollbars.png", image.EncodeToPNG());
		RenderTexture.active = null; m_camera.targetTexture = null; target.Release(); UnityEngine.Object.Destroy(image); UnityEngine.Object.Destroy(target);
	}
	static void Tick()
	{
		if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
		if (m_frame == Time.frameCount) return;
		m_frame = Time.frameCount;
		try
		{
			if (Time.time - m_started > 20) throw new Exception("Chat UI test timed out");
			if (m_phase == 0)
			{
				new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
				m_camera = new GameObject("Camera").AddComponent<Camera>(); m_camera.tag = "MainCamera"; m_camera.clearFlags = CameraClearFlags.SolidColor; m_camera.backgroundColor = new Color(.16f,.20f,.17f,1);
				m_root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MYFolder/Prefab/UI/ZoneInterface.prefab"));
				Canvas canvas = m_root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = m_camera; canvas.planeDistance = 1;
				m_root.GetComponent<ZoneGameplayUI>().SetLoading(false);
				m_root.transform.Find("ChannelUI/ChannelWindow").gameObject.SetActive(true);
				m_chat = m_root.GetComponentInChildren<ZoneChatUI>(true); m_history = m_chat.GetComponent<ScrollbarOnlyScrollRect>();
				m_channels = m_root.transform.Find("ChannelUI/ChannelWindow/Panel/Viewport").GetComponent<ScrollbarOnlyScrollRect>();
				m_text = (TMP_Text)typeof(ZoneChatUI).GetField("m_chatText",Flags).GetValue(m_chat);
				ReadChannels(10);
				for (int i=1;i<=200;++i) m_chat.AddChat("Dummy" + i.ToString("D4"), "채팅 " + i);
				Queue<string> chats = (Queue<string>)typeof(ZoneChatUI).GetField("m_chats",Flags).GetValue(m_chat);
				Check(chats.Count == 30 && m_text.text.Length == 0, "all 200 chats accepted in one frame, latest 30 queued, display update batched");
				m_phase = 1;
			}
			else if (m_phase == 1)
			{
				Check(m_text.text.Split('\n').Length == 30 && m_text.text.Contains("Dummy0171") && m_text.text.Contains("Dummy0200") && !m_text.text.Contains("Dummy0170"), "next display update renders all latest 30, not one line per frame");
				Check(m_history.verticalNormalizedPosition < .01f, "first burst follows latest chat");
				ScrollTest(m_history, "chat"); ScrollTest(m_channels, "channel");
				m_history.verticalScrollbar.value = .75f;
				m_chat.AddChat("Dummy0201", "새 채팅"); m_phase = 2;
			}
			else if (m_phase == 2)
			{
				Check(Mathf.Abs(m_history.verticalNormalizedPosition - .75f) < .02f, "new chat does not force a reader back to bottom");
				m_history.verticalScrollbar.value = 0; m_chat.AddChat("Dummy0202", "최신 채팅");
				ReadChannels(40); m_phase = 3;
			}
			else if (m_phase == 3)
			{
				Check(m_history.verticalNormalizedPosition < .01f && m_text.text.Contains("Dummy0202"), "bottom view continues following newest messages");
				ScrollTest(m_channels, "40-channel list"); ReadChannels(10); m_channels.verticalScrollbar.value = 0;
				m_phase = 4;
			}
			else if (m_phase == 4)
			{
				Canvas.ForceUpdateCanvases();
				Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(m_channels.viewport, m_channels.content);
				Debug.Log("[ChatScrollTest] final content=" + m_channels.content.rect + " pos=" + m_channels.content.anchoredPosition + " bounds=" + bounds + " viewport=" + m_channels.viewport.rect);
				int visible = 0;
				ZoneChannelRowUI[] rows = m_root.GetComponentsInChildren<ZoneChannelRowUI>();
				for(int i=0;i<rows.Length;++i)
				{
					Bounds row = RectTransformUtility.CalculateRelativeRectTransformBounds(m_channels.viewport, rows[i].transform);
					if(row.max.y > m_channels.viewport.rect.yMin && row.min.y < m_channels.viewport.rect.yMax) ++visible;
				}
				Check(rows.Length == 10 && visible >= 5, "ten-channel response has visible rows at bottom after shrinking from forty");
				Check(m_channels.verticalScrollbar.size > .5f, "handle size updates to ten-channel list height");
				Capture();
				Check(!m_failed, "no runtime errors");
				Debug.Log("[ChatScrollTest] COMPLETE checks=" + m_checks + ", production UI prefab, wheel ignored and scrollbars dragged, 1920x1080 capture");
				EditorApplication.ExitPlaymode();
			}
		}
		catch (Exception exception)
		{
			m_failed = true; Debug.LogError("[ChatScrollTest] FAIL " + exception); SessionState.SetBool(Key,false); EditorApplication.Exit(1);
		}
	}
}
