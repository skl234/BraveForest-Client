using UnityEngine;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

public enum ePacketType : ushort
{
	// ===== Proxy (0 ~ 999)
	C2S_ProxyEcho = 0,
	S2C_ProxyEcho,
	C2S_ProxyMonitor,
	S2C_ProxyMonitor,

	// ===== LoginServer (1000 ~ 1999)
	C2S_Login = 1000,
	S2C_Login,
	C2S_Logout,
	S2C_Logout,
	C2S_CreateAccount,
	S2C_CreateAccount,
	// 캐릭터 생성·조회·선택은 World 처리. 서버와 같은 패킷 번호를 유지합니다.
	C2S_CharacterList,
	S2C_CharacterList,
	C2S_CreateCharacter,
	S2C_CreateCharacter,
	C2S_SelectCharacter,
	S2C_SelectCharacter,

	C2S_LoginEcho,
	S2C_LoginEcho,
	C2S_LoginMonitor,
	S2C_LoginMonitor,

	// ===== ZoneServer (2000 ~ 2999)
	C2S_EnterZone = 2000,
	S2C_EnterZone,
	C2S_LeaveZone,
	S2C_LeaveZone,
	C2S_Move,
	S2C_Move,
	C2S_AttackStart,
	S2C_AttackStart,
	C2S_AttackExecute,
	S2C_AttackResult,
	S2C_LevelUp,
	C2S_AttackCancel,
	C2S_UsePotion,
	S2C_UsePotion,
	C2S_ChannelList,
	S2C_ChannelList,
	C2S_ChangeChannel,
	S2C_ChangeChannel,
	C2S_ChangeZone,
	S2C_ChangeZone,
	S2C_FieldObjectEnter,
	S2C_FieldObjectLeave,
	S2C_MonsterState,
	// 2023은 삭제된 패킷 번호. 뒤의 패킷 번호는 그대로 유지
	S2C_MonsterMove = 2024,
	C2S_Chat,
	S2C_Chat,
	S2C_PlayerDead,
	C2S_Respawn,
	S2C_Respawn,
	S2C_PlayerHit,

	// ===== WorldServer (3000 ~ 3999)
	C2S_WorldChat = 3000,
	S2C_WorldChat,
	C2S_Whisper = 3023,
	S2C_Whisper,
	S2C_WhisperResult,
	C2S_CreateParty = 3030,
	C2S_InviteParty,
	C2S_AnswerPartyInvite,
	C2S_LeaveParty,
	C2S_KickParty,
	C2S_ChangePartyLeader,
	C2S_DisbandParty,
	C2S_PartyChat,
	S2C_PartyResult,
	S2C_PartyInvite,
	S2C_CancelPartyInvite,
	S2C_PartyInfo,
	S2C_PartyMemberStatus,
	S2C_PartyChat,

	C2S_PartyList = 3050,
	C2S_ApplyParty,
	C2S_CancelPartyApplication,
	C2S_AnswerPartyApplication,
	S2C_PartyList,
	S2C_PartyApplicationList,
	S2C_PartyApplicationState,

	// ===== DummyPacket (10000~)
	C2S_DummyPacket8 = 10000,
	C2S_DummyPacket16,
	C2S_DummyPacket32,
	C2S_DummyPacket64,

	S2C_DummyPacket,

	End = 20000,
}
public class NetClient : MonoBehaviour
{
	[SerializeField]
	string m_ip;
	[SerializeField]
	int m_port;

	TcpClient       m_client = null;
	NetworkStream   m_netStream = null;

	Thread          m_recvThread = null;
	volatile bool   m_running = false;
	readonly object m_stateLock = new object();
	string          m_disconnectMessage = null;

	CCircleQueue    m_recvQueue = new CCircleQueue(65535);
	readonly Queue<byte[]> m_packetQueue = new Queue<byte[]>();
	readonly ChatHistory m_chatHistory = new ChatHistory();
	readonly ChatInputState m_chatInputState = new ChatInputState();
	ChatNetwork m_chatNetwork;
	readonly PartyInfo m_partyInfo = new PartyInfo();
	PartyNetwork m_partyNetwork;

	public PartyInfo GetPartyInfo() { return m_partyInfo; }
	public PartyNetwork GetPartyNetwork()
	{
		if (m_partyNetwork == null) m_partyNetwork = new PartyNetwork(this, m_partyInfo, m_chatHistory, m_chatInputState);
		return m_partyNetwork;
	}

	public ChatHistory GetChatHistory() { return m_chatHistory; }
	public ChatInputState GetChatInputState() { return m_chatInputState; }
	public ChatNetwork GetChatNetwork()
	{
		if (m_chatNetwork == null) m_chatNetwork = new ChatNetwork(this, m_chatHistory, m_chatInputState);
		return m_chatNetwork;
	}

	void Update()
	{
		ReadReceivedPackets();
	}

	void ReadReceivedPackets()
	{
		byte[] packet;
		while (m_recvQueue.TryPopPacket(out packet))
		{
			// 채팅은 씬 전환 중에도 보관. 나머지 패킷은 씬에서 순서대로 처리
			if (!GetPartyNetwork().ReadPacket(packet) && !GetChatNetwork().ReadPacket(packet)) m_packetQueue.Enqueue(packet);
		}
	}

	void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
	public bool Connect()
	{
		Disconnect();
		lock (m_stateLock)
		{
			m_disconnectMessage = null;
		}

		try
		{
			m_client = new TcpClient();
			m_client.Connect(m_ip, m_port);
			m_netStream = m_client.GetStream();

			m_running = true;
			m_recvThread = new Thread(RecvThreadFunc);
			m_recvThread.IsBackground = true;
			m_recvThread.Start();
			return true;
		}
		catch (Exception exception)
		{
			SetDisconnectMessage("서버 연결 실패: " + exception.Message);
			CloseSocket();
			return false;
		}
	}
	public void Disconnect()
	{
		m_running = false;
		CloseSocket();

		if (m_recvThread != null && m_recvThread != Thread.CurrentThread)
		{
			m_recvThread.Join(1000);
		}

		m_recvThread = null;
		m_recvQueue.Clean();
		m_packetQueue.Clear();
		m_chatHistory.Clear();
		m_chatInputState.Clear();
		m_partyInfo.Clear();
	}
	public byte[] ReadPacket()
	{
		ReadReceivedPackets();
		if (m_packetQueue.Count == 0) return null;
		return m_packetQueue.Dequeue();
	}
	public bool Write(PacketStream _stream)
	{
		if (!m_running || m_netStream == null) return false;

		try
		{
			m_netStream.Write(_stream.GetBuffer(), 0, _stream.GetSize());
			return true;
		}
		catch (Exception exception)
		{
			SetDisconnectMessage("패킷 전송 실패: " + exception.Message);
			m_running = false;
			CloseSocket();
			return false;
		}
	}
	public bool TryGetDisconnectMessage(out string _message)
	{
		lock (m_stateLock)
		{
			_message = m_disconnectMessage;
			m_disconnectMessage = null;
			return _message != null;
		}
	}
	void RecvThreadFunc()
	{
		byte[]  byteArray = new byte[65535];
		try
		{
			while (m_running)
			{
				int readSize = m_netStream.Read(byteArray);
				if (readSize <= 0) break;

				while(m_running)
				{
					if (m_recvQueue.Push(byteArray, readSize) > 0) break;
					Thread.Sleep(1);
				}
			}
		}
		catch (Exception exception)
		{
			if (m_running) SetDisconnectMessage("서버 수신 오류: " + exception.Message);
		}
		finally
		{
			if (m_running) SetDisconnectMessage("서버와의 연결이 종료되었습니다.");
			m_running = false;
			CloseSocket();
		}
	}
	void SetDisconnectMessage(string _message)
	{
		lock (m_stateLock)
		{
			if (m_disconnectMessage == null) m_disconnectMessage = _message;
		}
	}
	void CloseSocket()
	{
		lock (m_stateLock)
		{
			m_netStream?.Close();
			m_client?.Close();
			m_netStream = null;
			m_client = null;
		}
	}
	void OnDestroy()
	{
		Disconnect();
	}
	void OnApplicationQuit()
	{
		Disconnect();
	}
}
