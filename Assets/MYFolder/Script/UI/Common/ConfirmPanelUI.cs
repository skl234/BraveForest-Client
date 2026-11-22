using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ConfirmPanelUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_title;
	[SerializeField] TMP_Text m_message;
	[SerializeField] Button m_confirm;
	[SerializeField] Button m_cancel;
	Action m_action;

	void Awake()
	{
		m_confirm.onClick.AddListener(Confirm);
		m_cancel.onClick.AddListener(Close);
	}

	public void Show(string _title, string _message, Action _action)
	{
		m_title.text = _title;
		m_message.text = _message;
		m_action = _action;
		gameObject.SetActive(true);
		transform.SetAsLastSibling();
	}

	void Confirm()
	{
		Action action = m_action;
		Close();
		if (action != null) action();
	}

	public void Close()
	{
		m_action = null;
		if (EventSystem.current != null)
		{
			GameObject selected = EventSystem.current.currentSelectedGameObject;
			if (selected != null && selected.transform.IsChildOf(transform)) EventSystem.current.SetSelectedGameObject(null);
		}
		gameObject.SetActive(false);
	}
}
