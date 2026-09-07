using TMPro;
using UnityEngine;

public class ZoneGameplayUI : MonoBehaviour
{
	[SerializeField] CombatHUD m_hud;
	[SerializeField] GameObject m_loading;
	[SerializeField] TMP_Text m_loadingText;
	[SerializeField] NoticePanelUI m_noticePanel;
	PlayerCombat m_combat;
	float m_started;
	bool m_quitOnConfirm;

	public void Bind(LocalPlayerObject _player)
	{
		m_combat = _player.GetComponent<PlayerCombat>();
		m_hud.Bind(_player);
		SetLoading(false);
	}
	public void SetLoading(bool _loading)
	{
		if (_loading) CollapseMap();
		m_loading.SetActive(_loading);
		m_noticePanel.Hide();
		m_started = Time.unscaledTime;
	}
	public void ShowMessage(string _message, bool _quitOnConfirm = false)
	{
		CollapseMap();
		m_loading.SetActive(false);
		m_quitOnConfirm = _quitOnConfirm;
		m_noticePanel.Show(_message, "확인", ConfirmMessage);
	}
	public void ConfirmMessage()
	{
		if (m_quitOnConfirm)
		{
			Application.Quit();
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#endif
			return;
		}
		SetLoading(false);
	}
	public void ShowRespawn(string _message = "마을에서 부활합니다.")
	{
		CollapseMap();
		m_loading.SetActive(false);
		m_noticePanel.Show(_message, "확인", ConfirmRespawn);
	}
	void ConfirmRespawn()
	{
		if (ZoneSceneNetwork.Instance != null && ZoneSceneNetwork.Instance.RequestRespawn()) return;
		ShowRespawn("부활을 요청하지 못했습니다.\n확인을 누르면 다시 시도합니다.");
	}
	void CollapseMap()
	{
		ZoneMapUI map = FindObjectOfType<ZoneMapUI>();
		if (map != null) map.Collapse();
	}
	void Update()
	{
		if (!m_loading.activeSelf) return;
		int dots = (int)((Time.unscaledTime - m_started) * 2.0f) % 4;
		m_loadingText.text = "BRAVE FOREST\n\nLOADING" + new string('.', dots);
	}
	public void BasicAttack() { if (m_combat != null) m_combat.CommandTarget(m_combat.GetTarget(), true); }
	public void SkillA() { if (m_combat != null) m_combat.TryHeavy(); }
	public void SkillS() { if (m_combat != null) m_combat.TryArea(); }
	public void UsePotion() { if (m_combat != null) m_combat.TryPotion(); }
}
