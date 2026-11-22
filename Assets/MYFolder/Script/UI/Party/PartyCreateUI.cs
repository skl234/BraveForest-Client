using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyCreateUI : MonoBehaviour
{
	[SerializeField] TMP_InputField m_name;
	[SerializeField] TMP_Text m_message;
	[SerializeField] Button m_confirm;
	[SerializeField] Button m_cancel;
	PartyUI m_owner;
	string m_error = "";
	void Awake()
	{
		m_name.onValueChanged.AddListener(ChangeName);
		m_confirm.onClick.AddListener(Create);
		m_cancel.onClick.AddListener(Close);
	}
	public void Open(PartyUI _owner)
	{
		m_owner = _owner;
		m_error = "";
		m_name.SetTextWithoutNotify("");
		gameObject.SetActive(true);
		Refresh();
	}
	public void Close() { gameObject.SetActive(false); }
	void ChangeName(string _value)
	{
		if (Input.compositionString.Length > 0) return;
		m_error = "";
		int[] starts = StringInfo.ParseCombiningCharacters(_value);
		if (starts.Length > 15) m_name.SetTextWithoutNotify(_value.Substring(0, starts[15]));
		Refresh();
	}
	void Update() { Refresh(); }
	void Refresh()
	{
		if (m_owner == null || m_owner.GetInfo() == null) { m_confirm.interactable = false; return; }
		PartyInfo info = m_owner.GetInfo();
		bool connected = m_owner.GetLobbyNetwork() != null && m_owner.GetNetwork() != null && m_owner.GetNetwork().CanRequest();
		m_name.interactable = connected && !info.IsPending();
		m_message.text = "파티명은 1~15글자로 입력해주세요.";
		if (!connected) m_message.text = "서버에 연결되면 파티를 개설할 수 있습니다.";
		bool ready = connected && !info.IsPending() && info.partyNum == 0;
		if (string.IsNullOrWhiteSpace(m_name.text)) ready = false;
		m_confirm.interactable = ready;
		if (info.IsPending()) m_message.text = "파티 개설 중...";
		else if (!string.IsNullOrEmpty(m_error)) m_message.text = m_error;
		else if (!string.IsNullOrEmpty(info.resultText)) m_message.text = info.resultText;
	}
	void Create()
	{
		if (!m_confirm.interactable || Input.compositionString.Length > 0) return;
		m_error = "";
		bool sent = m_owner.GetLobbyNetwork().Create(m_name.text.Trim());
		if (!sent) m_error = "파티를 개설하지 못했습니다.";
		Refresh();
	}
}
