using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ZoneMenuUI : MonoBehaviour
{
	[SerializeField] GameObject m_buttons;
	[SerializeField] Button m_toggle;
	[SerializeField] TMP_Text m_toggleText;
	[SerializeField] Button m_playerButton;
	[SerializeField] Button m_partyButton;
	[SerializeField] Button m_settingsButton;
	[SerializeField] PlayerInfoPanelUI m_playerWindow;
	[SerializeField] SettingsPanelUI m_settingsWindow;
	[SerializeField] PartyUI m_party;
	void Awake()
	{
		ZoneGameplayUI gameplay = GetComponentInParent<ZoneGameplayUI>();
		if (m_party == null && gameplay != null) m_party = gameplay.GetComponentInChildren<PartyUI>(true);
		m_toggle.onClick.AddListener(Toggle);
		m_playerButton.onClick.AddListener(OpenPlayer);
		m_partyButton.onClick.AddListener(OpenParty);
		m_settingsButton.onClick.AddListener(OpenSettings);
		m_buttons.SetActive(false);
	}
	public void Bind(LocalPlayerObject _player) { m_playerWindow.Bind(_player.GetComponent<PlayerInfo>(), _player.GetPlayerClass()); }
	public void Toggle()
	{
		bool open = !m_buttons.activeSelf;
		m_buttons.SetActive(open);
		m_toggleText.text = "메뉴  ▼";
		if (open) m_toggleText.text = "메뉴  ▲";
	}
	void OpenPlayer()
	{
		if (UIInputBlocker.IsBlocked()) return;
		ZoneMapUI map = FindObjectOfType<ZoneMapUI>();
		if (map != null) map.Collapse();
		m_playerWindow.Open();
	}
	void OpenParty() { if (m_party != null) m_party.Toggle(); }

	void OpenSettings()
	{
		if (UIInputBlocker.IsBlocked()) return;
		ZoneMapUI map = FindObjectOfType<ZoneMapUI>();
		if (map != null) map.Collapse();
		m_settingsWindow.Open();
	}
}
