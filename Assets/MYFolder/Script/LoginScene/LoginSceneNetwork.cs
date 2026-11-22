using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// 소켓은 DontDestroyOnLoad의 NetClient 하나만 사용함
// 이 클래스는 로그인씬의 요청과 응답만 처리
public class LoginSceneNetwork : MonoBehaviour
{
	NetClient   m_client;
	AccountInfo m_accountInfo;
	LoginScene  m_scene;
	bool        m_waiting;
	bool        m_changingScene;

	public void Initialize(LoginScene _scene)
	{
		m_scene = _scene;
		m_client = FindObjectOfType<NetClient>();
		m_accountInfo = FindObjectOfType<AccountInfo>();
		if (m_client == null || m_accountInfo == null || !m_client.Connect())
		{
			m_scene.ShowDisconnected();
		}
	}
	void Update()
	{
		if (m_client == null || m_changingScene) return;
		string message;
		if (m_client.TryGetDisconnectMessage(out message))
		{
			Debug.LogError(message);
			m_waiting = false;
			m_scene.ShowDisconnected();
			return;
		}

		while (!m_changingScene)
		{
			byte[] packet = m_client.ReadPacket();
			if (packet == null) break;
			if (packet.Length < 5) continue;
			ePacketType type = (ePacketType)BitConverter.ToUInt16(packet, 2);
			switch (type)
			{
				case ePacketType.S2C_Login: OnLogin(packet); break;
				case ePacketType.S2C_CreateAccount:
					m_waiting = false;
					m_scene.ShowAccountResult(packet[4] != 0);
					break;
				case ePacketType.S2C_CharacterList: OnCharacterList(packet); break;
				case ePacketType.S2C_CreateCharacter: OnCreateCharacter(packet); break;
				case ePacketType.S2C_SelectCharacter: OnSelectCharacter(packet); break;
				case ePacketType.S2C_EnterZone: OnEnterZone(packet); break;
			}
		}
	}
	bool Send(PacketStream _stream)
	{
		if (m_client == null || m_waiting || m_changingScene) return false;
		if (!m_client.Write(_stream)) return false;
		m_waiting = true;
		return true;
	}
	public bool RequestLogin(string _id, string _pw)
	{
		return RequestAccount(ePacketType.C2S_Login, _id, _pw);
	}
	public bool RequestCreateAccount(string _id, string _pw)
	{
		return RequestAccount(ePacketType.C2S_CreateAccount, _id, _pw);
	}
	bool RequestAccount(ePacketType _type, string _id, string _pw)
	{
		// 계정 패킷은 기존 wchar_t 규약, 닉네임/채팅은 UTF-8 규약을 유지
		byte[] id = Encoding.Unicode.GetBytes(_id);
		byte[] pw = Encoding.Unicode.GetBytes(_pw);
		PacketStream stream = new PacketStream(12 + id.Length + pw.Length);
		stream.Write(_type);
		stream.Write(_id.Length);
		stream.Write(_pw.Length);
		stream.Write(id);
		stream.Write(pw);
		return Send(stream);
	}
	public void RequestCreateCharacter(ulong _jobClass, string _nickname)
	{
		byte[] nickname = Encoding.UTF8.GetBytes(_nickname);
		if (nickname.Length == 0 || nickname.Length > 30) return;
		PacketStream stream = new PacketStream(13 + nickname.Length);
		stream.Write(ePacketType.C2S_CreateCharacter);
		stream.Write(_jobClass);
		stream.Write((byte)nickname.Length);
		stream.Write(nickname);
		Send(stream);
	}
	public void RequestSelectCharacter(ulong _characterIndex)
	{
		PacketStream stream = new PacketStream(12);
		stream.Write(ePacketType.C2S_SelectCharacter);
		stream.Write(_characterIndex);
		Send(stream);
	}
	void OnLogin(byte[] _packet)
	{
		m_waiting = false;
		bool result = _packet[4] != 0;
		m_scene.ShowLoginResult(result);
		if (!result) return;

		PacketStream stream = new PacketStream(4);
		stream.Write(ePacketType.C2S_CharacterList);
		Send(stream);
	}
	void OnCharacterList(byte[] _packet)
	{
		m_waiting = false;
		int count = _packet[4];
		List<CharacterListInfo> characters = new List<CharacterListInfo>();
		for (int i = 0; i < count && i < 2; ++i)
		{
			CharacterListInfo character;
			if (!TryReadCharacter(_packet, 5 + 90 * i, out character)) break;
			characters.Add(character);
		}
		m_scene.ShowCharacters(characters, count);
	}
	void OnCreateCharacter(byte[] _packet)
	{
		m_waiting = false;
		CharacterListInfo character = null;
		if (_packet[4] != 0) TryReadCharacter(_packet, 5, out character);
		m_scene.ShowCreateResult(character);
	}
	bool TryReadCharacter(byte[] _packet, int _offset, out CharacterListInfo _character)
	{
		_character = null;
		if (_packet.Length < _offset + 90) return false;
		long nameSize = BitConverter.ToInt64(_packet, _offset + 8);
		if (nameSize < 0 || nameSize > 30) return false;
		string name = Encoding.UTF8.GetString(_packet, _offset + 16, (int)nameSize);
		_character = new CharacterListInfo(BitConverter.ToUInt64(_packet, _offset), name,
			BitConverter.ToUInt64(_packet, _offset + 46), BitConverter.ToUInt64(_packet, _offset + 54));
		return true;
	}
	void OnSelectCharacter(byte[] _packet)
	{
		m_waiting = false;
		if (_packet.Length < 85 || _packet[4] != 0)
		{
			m_scene.ShowNotice("캐릭터 정보를 불러오지 못했습니다.");
			return;
		}

		ulong characterIndex = BitConverter.ToUInt64(_packet, 5);
		ulong zoneId = BitConverter.ToUInt64(_packet, 61);
		m_accountInfo.SetCharacter(characterIndex, zoneId);

		PacketStream stream = new PacketStream(12);
		stream.Write(ePacketType.C2S_EnterZone);
		stream.Write(characterIndex);
		Send(stream);
	}
	void OnEnterZone(byte[] _packet)
	{
		m_waiting = false;
		if (_packet.Length < 45 || _packet[4] != 0)
		{
			m_scene.ShowNotice("Zone에 입장할 수 없습니다.");
			return;
		}
		ulong zoneId = m_accountInfo.GetZoneId();
		string sceneName;
		if (zoneId == 1) sceneName = "TownScene";
		else if (zoneId == 2) sceneName = "BeginnerZoneScene";
		else
		{
			m_scene.ShowNotice("아직 준비되지 않은 Zone입니다.");
			return;
		}

		m_accountInfo.SetEnterZonePacket(_packet);
		// 다음 씬이 받아야 할 패킷은 여기서 더 꺼내지 않음
		m_changingScene = true;
		LoadingPanelUI.ChangeScene(sceneName);
	}
	public void QuitClient()
	{
		if (m_client != null) m_client.Disconnect();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
