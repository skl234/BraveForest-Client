using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneChatUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_chatText;
	[SerializeField] ChatInputUI m_inputUI;
	[SerializeField] ScrollRect m_scrollRect;
	ChatHistory m_history;
	ChatInputState m_inputState;
	readonly List<int> m_textOffsets = new List<int>();
	uint m_revision;
	ulong m_firstIndex;
	bool m_refresh = true;

	void OnEnable()
	{
		NetClient client = FindObjectOfType<NetClient>();
		if (client != null)
		{
			m_history = client.GetChatHistory();
			m_inputState = client.GetChatInputState();
		}
		else
		{
			if (m_history == null) m_history = new ChatHistory();
			if (m_inputState == null) m_inputState = new ChatInputState();
		}
		m_refresh = true;
		m_chatText.richText = false;
		if (m_inputUI != null) m_inputUI.Bind(m_inputState, m_history, SendChat);
	}

	bool SendChat(eChatChannel _channel, string _targetName, string _text)
	{
		if (ZoneSceneNetwork.Instance == null) return false;
		return ZoneSceneNetwork.Instance.SendChat(_channel, _targetName, _text);
	}

	public void ChangeChannel()
	{
		if (m_inputUI != null) m_inputUI.ChangeChannel();
	}

	public void AddChat(string _name, string _chat)
	{
		m_history.AddChat(eChatChannel.Region, _name, _chat);
	}

	void LateUpdate()
	{
		if (m_history == null || (!m_refresh && m_revision == m_history.GetRevision())) return;
		m_refresh = false;
		m_revision = m_history.GetRevision();
		bool followLatest = m_chatText.text.Length == 0 || m_scrollRect.content.rect.height <= m_scrollRect.viewport.rect.height || m_scrollRect.verticalNormalizedPosition <= 0.01f;
		float position = m_scrollRect.content.anchoredPosition.y;
		ulong firstIndex = m_history.GetFirstIndex();
		if (firstIndex > m_firstIndex)
		{
			ulong removed = firstIndex - m_firstIndex;
			if (removed < (ulong)m_textOffsets.Count)
			{
				int offset = m_textOffsets[(int)removed];
				TMP_TextInfo info = m_chatText.textInfo;
				for (int i = 0; i < info.characterCount && info.lineCount > 0; ++i)
				{
					if (info.characterInfo[i].index < offset) continue;
					int line = info.characterInfo[i].lineNumber;
					position -= info.lineInfo[0].ascender - info.lineInfo[line].ascender;
					break;
				}
			}
			else position = 0;
		}
		m_firstIndex = firstIndex;
		// 몰려온 채팅은 프레임당 한 번만 반영. 이전 채팅을 읽는 중이면 위치 유지
		m_chatText.text = m_history.GetText(m_textOffsets);
		Canvas.ForceUpdateCanvases();
		if (followLatest) m_scrollRect.verticalNormalizedPosition = 0.0f;
		else
		{
			m_scrollRect.StopMovement();
			Vector2 offset = m_scrollRect.content.anchoredPosition;
			offset.y = Mathf.Clamp(position, 0, Mathf.Max(0, m_scrollRect.content.rect.height - m_scrollRect.viewport.rect.height));
			m_scrollRect.content.anchoredPosition = offset;
		}
	}
}
