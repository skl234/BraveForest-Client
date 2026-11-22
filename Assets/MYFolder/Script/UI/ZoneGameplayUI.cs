using TMPro;
using UnityEngine;

public class ZoneGameplayUI : MonoBehaviour
{
	[SerializeField] CombatHUD m_hud;
	[SerializeField] NoticePanelUI m_noticePanel;
	[SerializeField] ZoneMenuUI m_menu;
	PlayerCombat m_combat;
	bool m_quitOnConfirm;

	public void Bind(LocalPlayerObject _player)
	{
		m_combat = _player.GetComponent<PlayerCombat>();
		m_hud.Bind(_player);
		if (m_menu != null) m_menu.Bind(_player);
		SetLoading(false);
	}
	public void SetLoading(bool _loading)
	{
		if (_loading) CollapseMap();
		if (_loading) LoadingPanelUI.Show();
		else LoadingPanelUI.Hide();
		m_noticePanel.Hide();
	}
	public void ShowMessage(string _message, bool _quitOnConfirm = false)
	{
		CollapseMap();
		LoadingPanelUI.Hide();
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
		LoadingPanelUI.Hide();
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
	public void BasicAttack() { if (m_combat != null) m_combat.CommandTarget(m_combat.GetTarget(), true); }
	public void SkillA() { if (m_combat != null) m_combat.TryHeavy(); }
	public void SkillS() { if (m_combat != null) m_combat.TryArea(); }
	public void UsePotion() { if (m_combat != null) m_combat.TryPotion(); }
}
