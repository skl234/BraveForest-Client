using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerWorldUI : MonoBehaviour
{
	[SerializeField] TMP_Text m_nameText;
	[SerializeField] TMP_Text m_levelText;
	[SerializeField] Image m_hpFill;
	[SerializeField] ChatBubbleUI m_chatBubble;
	PlayerInfo m_playerInfo;

	public void Bind(PlayerInfo _playerInfo)
	{
		if (m_playerInfo != _playerInfo && m_chatBubble != null) m_chatBubble.Hide();
		m_playerInfo = _playerInfo;
		Refresh();
	}

	public void Refresh()
	{
		if (m_playerInfo == null) return;
		if (m_nameText != null) m_nameText.text = m_playerInfo.GetName();
		if (m_levelText != null) m_levelText.text = "Lv. " + m_playerInfo.GetLevel();
		if (m_hpFill != null) m_hpFill.fillAmount = (float)m_playerInfo.GetHP() / m_playerInfo.GetMaxHP();
	}

	public void ShowChat(string _chat)
	{
		if (m_chatBubble != null) m_chatBubble.Show(_chat);
	}

	void LateUpdate()
	{
		Camera camera = Camera.main;
		if (camera != null) transform.rotation = camera.transform.rotation;
	}
}
