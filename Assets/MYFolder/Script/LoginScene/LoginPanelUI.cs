using TMPro;
using UnityEngine;

public class LoginPanelUI : MonoBehaviour
{
	[SerializeField] TMP_InputField m_idField;
	[SerializeField] TMP_InputField m_pwField;
	[SerializeField] CreateAccountPanelUI m_createPanel;
	LoginSceneNetwork m_network;

	public void Bind(LoginSceneNetwork _network) { m_network = _network; }
	public void OpenCreateAccount() { m_createPanel.Show(); }

	public void Login()
	{
		if (!IsValid(m_idField.text, 1) || !IsValid(m_pwField.text, 1)) return;
		if (m_network != null) m_network.RequestLogin(m_idField.text, m_pwField.text);
	}

	public static bool IsValid(string _text, int _minLength)
	{
		if (string.IsNullOrEmpty(_text) || _text.Length < _minLength || _text.Length > 15) return false;
		for (int i = 0; i < _text.Length; ++i)
		{
			if (!char.IsLetterOrDigit(_text[i])) return false;
		}
		return true;
	}
}
