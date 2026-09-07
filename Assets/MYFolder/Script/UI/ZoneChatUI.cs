using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ZoneChatUI : MonoBehaviour
{
	const int ChatCapacity = 30;
	[SerializeField] TMP_Text m_chatText;
	[SerializeField] TMP_InputField m_inputField;
	[SerializeField] ScrollRect m_scrollRect;
	Queue<string> m_chats = new Queue<string>(ChatCapacity);
	int m_submitFrame = -1;
	bool m_historyChanged;

	void Awake()
	{
		m_inputField.characterLimit = 15;
		m_inputField.onSubmit.AddListener(OnSubmit);
	}

	void OnDestroy()
	{
		m_inputField.onSubmit.RemoveListener(OnSubmit);
	}

	void Update()
	{
		if (m_inputField.isFocused && Input.GetKeyDown(KeyCode.Escape))
		{
			m_inputField.DeactivateInputField();
			EventSystem.current.SetSelectedGameObject(null);
		}
		else if (!m_inputField.isFocused && m_submitFrame != Time.frameCount && Input.GetKeyDown(KeyCode.Return))
		{
			m_inputField.ActivateInputField();
		}
	}

	void OnSubmit(string _value)
	{
		m_submitFrame = Time.frameCount;
		if (string.IsNullOrWhiteSpace(_value)) return;
		if (ZoneSceneNetwork.Instance == null || !ZoneSceneNetwork.Instance.SendChat(_value)) return;
		m_inputField.text = "";
		m_inputField.DeactivateInputField();
		EventSystem.current.SetSelectedGameObject(null);
	}

	public void AddChat(string _name, string _chat)
	{
		string line = "[" + DateTime.Now.ToString("HH:mm") + "][" + _name + "] : " + _chat;
		// 30개가 차면 가장 오래된 채팅부터 제거
		if (m_chats.Count >= ChatCapacity) m_chats.Dequeue();
		m_chats.Enqueue(line);
		m_historyChanged = true;
	}

	void LateUpdate()
	{
		if (!m_historyChanged) return;
		m_historyChanged = false;
		bool followLatest = m_chatText.text.Length == 0 || m_scrollRect.content.rect.height <= m_scrollRect.viewport.rect.height || m_scrollRect.verticalNormalizedPosition <= 0.01f;
		float position = m_scrollRect.verticalNormalizedPosition;
		// 몰려온 채팅은 프레임당 한 번만 반영. 이전 채팅을 읽는 중이면 위치 유지
		m_chatText.text = string.Join("\n", m_chats);
		Canvas.ForceUpdateCanvases();
		if (followLatest) m_scrollRect.verticalNormalizedPosition = 0.0f;
		else m_scrollRect.verticalNormalizedPosition = position;
	}
}
