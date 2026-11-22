using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreatePanelUI : MonoBehaviour
{
	[SerializeField] TMP_InputField m_nicknameField;
	[SerializeField] Button m_warriorButton;
	[SerializeField] Button m_archerButton;
	[SerializeField] TMP_Text m_warriorButtonText;
	[SerializeField] TMP_Text m_archerButtonText;
	[SerializeField] TMP_Text m_validationText;
	[SerializeField] Button m_confirmButton;
	[SerializeField] Button m_cancelButton;
	Action<ulong, string> m_create;
	int m_selectedJob = -1;

	public void Initialize(Action<ulong, string> _create)
	{
		m_create = _create;
		m_warriorButton.onClick.RemoveListener(SelectWarrior);
		m_archerButton.onClick.RemoveListener(SelectArcher);
		m_confirmButton.onClick.RemoveListener(ConfirmCreate);
		m_cancelButton.onClick.RemoveListener(Close);
		m_nicknameField.onValueChanged.RemoveListener(OnNicknameChanged);
		m_warriorButton.onClick.AddListener(SelectWarrior);
		m_archerButton.onClick.AddListener(SelectArcher);
		m_confirmButton.onClick.AddListener(ConfirmCreate);
		m_cancelButton.onClick.AddListener(Close);
		m_nicknameField.onValueChanged.AddListener(OnNicknameChanged);
	}

	public void Show()
	{
		m_selectedJob = -1;
		m_nicknameField.text = "";
		m_validationText.text = "";
		UpdateSelection();
		UpdateCreateButton();
		gameObject.SetActive(true);
		transform.SetAsLastSibling();
		m_nicknameField.Select();
	}

	public void Close() { gameObject.SetActive(false); }
	void SelectWarrior() { SelectJob(0); }
	void SelectArcher() { SelectJob(1); }

	public void SelectJob(int _job)
	{
		if (_job < 0 || _job > 1) return;
		m_selectedJob = _job;
		m_validationText.text = "";
		UpdateSelection();
		UpdateCreateButton();
	}

	void UpdateSelection()
	{
		UIButtonFeedback warrior = m_warriorButton.GetComponent<UIButtonFeedback>();
		UIButtonFeedback archer = m_archerButton.GetComponent<UIButtonFeedback>();
		if (warrior != null) warrior.SetSelected(m_selectedJob == 0);
		if (archer != null) archer.SetSelected(m_selectedJob == 1);
		m_warriorButtonText.text = "전사";
		m_archerButtonText.text = "궁수";
		if (m_selectedJob == 0)
		{
			m_warriorButtonText.text = "전사 (선택)";
		}
		if (m_selectedJob == 1)
		{
			m_archerButtonText.text = "궁수 (선택)";
		}
	}

	void OnNicknameChanged(string _nickname)
	{
		m_validationText.text = "";
		UpdateCreateButton();
	}

	void UpdateCreateButton()
	{
		string name = m_nicknameField.text.Trim();
		m_confirmButton.interactable = m_selectedJob >= 0 && name.Length > 0 && Encoding.UTF8.GetByteCount(name) <= 30;
	}

	public void ConfirmCreate()
	{
		UpdateCreateButton();
		if (!m_confirmButton.interactable) return;
		m_create?.Invoke((ulong)m_selectedJob, m_nicknameField.text.Trim());
		Close();
	}
}
