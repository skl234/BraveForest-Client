using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CreateAccountOkButton : MonoBehaviour
{
	[SerializeReference]
	TMP_InputField m_idField;
	[SerializeReference]
	TMP_InputField m_pwField;
	[SerializeReference]
	GameObject m_accountCreatePanel;
	[SerializeReference]
	LoginSceneNetwork m_network;

	public void OnClick()
	{
		string id = m_idField.text;
		string pw = m_pwField.text;

		if (!IsValid(id, 5) || !IsValid(pw, 4))
		{
			Debug.Log("가입 양식을 확인해 주시기 바랍니다.");
			return;
		}

		if (!m_network.RequestCreateAccount(id, pw)) return;

		m_idField.text = "";
		m_pwField.text = "";
		m_accountCreatePanel.SetActive(false);
	}
	private bool IsValid(string _str, int _minLength)
	{
		if (_str.Length < _minLength || _str.Length > 15) return false;

		foreach (char c in _str)
		{
			if (!(char.IsLetterOrDigit(c))) // 영문자 또는 숫자가 아니면 false
				return false;
		}
		return true;
	}
}
