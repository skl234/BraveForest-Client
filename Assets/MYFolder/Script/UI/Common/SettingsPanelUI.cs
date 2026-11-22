using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SettingsPanelUI : MonoBehaviour
{
	[SerializeField] Button m_fullHD;
	[SerializeField] Button m_HD;
	[SerializeField] Button m_apply;
	[SerializeField] Button m_close;
	[SerializeField] TMP_Text m_selection;
	int m_resolutionIndex;

	void Awake()
	{
		m_fullHD.onClick.AddListener(SelectFullHD);
		m_HD.onClick.AddListener(SelectHD);
		m_apply.onClick.AddListener(Apply);
		m_close.onClick.AddListener(Close);
	}

	public void Open()
	{
		gameObject.SetActive(true);
		m_resolutionIndex = GameScreen.GetResolutionIndex();
		Refresh();
	}

	public void Close()
	{
		if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
			EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
			EventSystem.current.SetSelectedGameObject(null);
		gameObject.SetActive(false);
	}

	void SelectFullHD()
	{
		m_resolutionIndex = 0;
		Refresh();
	}

	void SelectHD()
	{
		m_resolutionIndex = 1;
		Refresh();
	}

	void Apply()
	{
		GameScreen.SetResolution(m_resolutionIndex);
		Refresh();
	}

	void Refresh()
	{
		Vector2Int size = GameScreen.GetResolution(m_resolutionIndex);
		m_selection.text = "선택한 해상도 : " + size.x + " × " + size.y;
		m_fullHD.interactable = m_resolutionIndex != 0;
		m_HD.interactable = m_resolutionIndex != 1;
		m_apply.interactable = m_resolutionIndex != GameScreen.GetResolutionIndex();
	}
}
