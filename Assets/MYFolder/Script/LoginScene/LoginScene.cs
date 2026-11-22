using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.UI;

public class LoginScene : MonoBehaviour
{
	[SerializeField] GameObject m_loginPanel;
	[SerializeField] GameObject m_characterPanel;
	[SerializeField] GameObject m_warriorPrefab;
	[SerializeField] GameObject m_archerPrefab;
	[SerializeField] NoticePanelUI m_noticePanel;
	[SerializeField] CharacterSelectionPanel m_characterSelectionPanel;
	[SerializeField] LoginSceneNetwork m_network;
	[SerializeField] LoginPanelUI m_accountPanel;
	[SerializeField] CreateAccountPanelUI m_createAccountPanel;

	void Start()
	{
		m_accountPanel.Bind(m_network);
		m_createAccountPanel.Bind(m_network);
		m_noticePanel.Initialize();
		m_characterSelectionPanel.Initialize(m_warriorPrefab, m_archerPrefab, m_network.RequestCreateCharacter, m_network.RequestSelectCharacter);
		m_loginPanel.SetActive(true);
		m_characterPanel.SetActive(false);

		m_network.Initialize(this);
		LoadingPanelUI.Hide();
	}

	// 씬은 화면 변경만 하고, 패킷 처리는 LoginSceneNetwork에서 함
	public void ShowLoginResult(bool _result)
	{
		if (!_result)
		{
			ShowNotice("ID 및 PW를 확인하세요");
			return;
		}

		m_loginPanel.SetActive(false);
		m_characterPanel.SetActive(true);
		m_characterSelectionPanel.ShowWaiting();
	}
	public void ShowAccountResult(bool _result)
	{
		if (_result) ShowNotice("회원가입이 완료되었습니다.");
		else ShowNotice("회원가입에 실패했습니다. 가입 정보를 확인하세요.");
	}
	public void ShowCharacters(List<CharacterListInfo> _characters, int _count)
	{
		m_characterSelectionPanel.SetCharacters(_characters, _count);
	}
	public void ShowCreateResult(CharacterListInfo _character)
	{
		if (_character == null)
		{
			m_characterSelectionPanel.ShowCreateFailed();
			ShowNotice("캐릭터 생성에 실패했습니다.");
			return;
		}

		m_characterSelectionPanel.AddCharacter(_character);
		ShowNotice("캐릭터가 생성되었습니다.");
	}
	public void ShowNotice(string _message)
	{
		m_noticePanel.Show(_message, "확인", null);
	}
	public void ShowDisconnected()
	{
		m_loginPanel.SetActive(true);
		m_characterPanel.SetActive(false);
		m_noticePanel.Show("서버와 연결이 되지않았습니다.", "확인", m_network.QuitClient);
	}
}


