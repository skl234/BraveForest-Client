using TMPro;
using UnityEngine;

public class CreateAccountPanelUI : MonoBehaviour
{
	[SerializeField] TMP_InputField m_idField;
	[SerializeField] TMP_InputField m_pwField;
	LoginSceneNetwork m_network;

	public void Bind(LoginSceneNetwork _network) { m_network = _network; }
	public void Show() { gameObject.SetActive(true); m_idField.Select(); }

	public void Close()
	{
		m_idField.text = "";
		m_pwField.text = "";
		gameObject.SetActive(false);
	}

	public void CreateAccount()
	{
		if (!LoginPanelUI.IsValid(m_idField.text, 5) || !LoginPanelUI.IsValid(m_pwField.text, 4)) return;
		if (m_network != null && m_network.RequestCreateAccount(m_idField.text, m_pwField.text)) Close();
	}
}
