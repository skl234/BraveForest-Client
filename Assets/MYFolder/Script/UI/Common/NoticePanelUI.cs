using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 로그인/존 어디서든 같은 안내창을 사용함
public class NoticePanelUI : MonoBehaviour
{
	[SerializeField] TMP_Text	m_messageText;
	[SerializeField] TMP_Text	m_buttonText;
	[SerializeField] Button		m_button;
	Action						m_action;

	public void Initialize()
	{
		m_button.onClick.RemoveListener(OnButtonClick);
		m_button.onClick.AddListener(OnButtonClick);
		Hide();
	}

	public void Show(string _message, string _buttonText, Action _action)
	{
		m_button.onClick.RemoveListener(OnButtonClick);
		m_button.onClick.AddListener(OnButtonClick);
		m_messageText.text = _message;
		m_buttonText.text = _buttonText;
		m_action = _action;
		gameObject.SetActive(true);
		transform.SetAsLastSibling();
		m_button.Select();
	}

	public void Hide()
	{
		gameObject.SetActive(false);
		m_action = null;
	}

	void OnButtonClick()
	{
		Action action = m_action;
		Hide();
		if (action != null) action();
	}
}
