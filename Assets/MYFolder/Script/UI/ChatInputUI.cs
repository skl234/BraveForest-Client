using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChatInputUI : MonoBehaviour
{
	[SerializeField] TMP_InputField m_inputField;
	[SerializeField] TMP_Text m_prefixText;
	[SerializeField] RectTransform m_textArea;
	ChatInputState m_state;
	ChatHistory m_history;
	Func<eChatChannel, string, string, bool> m_send;
	Coroutine m_refocus;
	int m_submitFrame = -1;
	string m_prefix = "";

	public TMP_InputField GetInputField() { return m_inputField; }
	public ChatInputState GetState() { return m_state; }

	public void Bind(ChatInputState _state, ChatHistory _history, Func<eChatChannel, string, string, bool> _send)
	{
		m_state = _state;
		m_history = _history;
		m_send = _send;
		UpdatePrefix();
	}

	void OnEnable()
	{
		m_inputField.characterLimit = 0;
		m_inputField.lineType = TMP_InputField.LineType.SingleLine;
		m_inputField.richText = false;
		m_inputField.textComponent.richText = false;
		UnityEngine.UI.Navigation navigation = m_inputField.navigation;
		navigation.mode = UnityEngine.UI.Navigation.Mode.None;
		m_inputField.navigation = navigation;
		m_inputField.onValueChanged.AddListener(OnTextChanged);
		m_inputField.onSubmit.AddListener(OnSubmit);
	}

	void OnDisable()
	{
		m_inputField.onValueChanged.RemoveListener(OnTextChanged);
		m_inputField.onSubmit.RemoveListener(OnSubmit);
		if (m_refocus != null) StopCoroutine(m_refocus);
		m_refocus = null;
	}

	void Update()
	{
		if (m_state == null) return;
		UpdatePrefix();
		if (UIInputBlocker.IsBlocked()) return;
		if (m_inputField.isFocused)
		{
			if (Input.GetKeyDown(KeyCode.Tab) && Input.compositionString.Length == 0) ChangeChannel();
			if (Input.GetKeyDown(KeyCode.Escape)) CloseInput();
			return;
		}
		if (m_submitFrame == Time.frameCount || !Input.GetKeyDown(KeyCode.Return)) return;
		GameObject selected = null;
		if (EventSystem.current != null) selected = EventSystem.current.currentSelectedGameObject;
		if (selected != null && selected != m_inputField.gameObject && selected.activeInHierarchy)
		{
			UnityEngine.UI.Selectable control = selected.GetComponent<UnityEngine.UI.Selectable>();
			if (control != null && control.IsActive() && control.IsInteractable()) return;
		}
		m_inputField.ActivateInputField();
		UIInputBlocker.BlockCurrentFrame();
	}

	public void ChangeChannel()
	{
		if (m_state == null || !m_inputField.isFocused || UIInputBlocker.IsBlocked()) return;
		m_state.ChangeChannel();
		UpdatePrefix();
		UIInputBlocker.BlockCurrentFrame();
	}

	void OnTextChanged(string _text)
	{
		if (Input.compositionString.Length > 0) return;
		string text = ChatInputState.LimitInput(_text);
		if (text == _text) return;
		int caret = Mathf.Min(m_inputField.stringPosition, text.Length);
		m_inputField.SetTextWithoutNotify(text);
		m_inputField.stringPosition = caret;
	}

	void OnSubmit(string _text) { Submit(_text); }

	public bool Submit(string _text)
	{
		if (m_state == null || UIInputBlocker.IsBlocked() || Input.compositionString.Length > 0) return false;
		m_submitFrame = Time.frameCount;
		UIInputBlocker.BlockCurrentFrame();
		string text = ChatInputState.LimitInput(_text);
		m_inputField.SetTextWithoutNotify(text);
		if (text.StartsWith("/", StringComparison.Ordinal))
		{
			if (m_state.SelectWhisper(text.Substring(1)))
			{
				m_inputField.SetTextWithoutNotify("");
				UpdatePrefix();
			}
			else m_history.AddSystem("귓속말을 보낼 닉네임을 입력해주세요.");
			Refocus();
			return false;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			Refocus();
			return false;
		}
		eChatChannel channel = m_state.GetChannel();
		string targetName = m_state.GetWhisperName();
		if (m_send == null || !m_send(channel, targetName, text))
		{
			m_history.AddSystem("채팅을 보내지 못했습니다.");
			Refocus();
			return false;
		}
		m_inputField.SetTextWithoutNotify("");
		CloseInput();
		return true;
	}

	void Refocus()
	{
		if (m_refocus != null) StopCoroutine(m_refocus);
		m_refocus = StartCoroutine(RestoreFocus());
	}

	IEnumerator RestoreFocus()
	{
		yield return null;
		m_refocus = null;
		if (UIInputBlocker.IsBlocked()) yield break;
		m_inputField.ActivateInputField();
		m_inputField.MoveTextEnd(false);
	}

	void CloseInput()
	{
		m_inputField.DeactivateInputField();
		if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == m_inputField.gameObject)
			EventSystem.current.SetSelectedGameObject(null);
		UIInputBlocker.BlockCurrentFrame();
	}

	void UpdatePrefix()
	{
		if (m_prefixText == null || m_state == null) return;
		string prefix = m_state.GetPrefix();
		if (m_prefix == prefix) return;
		m_prefix = prefix;
		m_prefixText.text = prefix;
		m_prefixText.richText = false;
		m_prefixText.raycastTarget = false;
		m_prefixText.color = new Color(1, .9f, .08f);
		if (m_state.GetChannel() == eChatChannel.World) m_prefixText.color = new Color(1, .9f, .08f);
		if (m_state.GetChannel() == eChatChannel.Party) m_prefixText.color = new Color(.65f, .82f, 1);
		if (m_state.GetChannel() == eChatChannel.Whisper) m_prefixText.color = new Color(.92f, .38f, 1);
		float width = Mathf.Clamp(m_prefixText.GetPreferredValues(prefix).x + 4, 48, 150);
		m_prefixText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
		Vector2 offset = m_textArea.offsetMin;
		offset.x = width + 16;
		m_textArea.offsetMin = offset;
	}
}
