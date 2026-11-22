using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public enum ePartyResult : byte
{
	Success,
	InvalidState,
	NotFound,
	AlreadyInParty,
	NotLeader,
	Full,
	InvitePending,
	InvalidTarget,
	Canceled,
	Declined,
	InvalidName,
	ApplicationPending,
	ApplicationNotFound,
}

public class PartyNetwork : IPartyUIActions
{
	const int StatusSize = 57;
	public const int MemberSize = 96;
	readonly NetClient m_client;
	readonly PartyInfo m_info;
	readonly ChatHistory m_history;
	readonly ChatInputState m_chatInput;
	static readonly UTF8Encoding m_utf8 = new UTF8Encoding(false, true);
	readonly PartyLobbyNetwork m_lobbyNetwork;

	public PartyNetwork(NetClient _client, PartyInfo _info, ChatHistory _history, ChatInputState _chatInput)
	{
		m_client = _client;
		m_info = _info;
		m_history = _history;
		m_chatInput = _chatInput;
		m_lobbyNetwork = new PartyLobbyNetwork(_client, _info, this);
	}

	public IPartyLobbyRequests GetLobbyNetwork() { return m_lobbyNetwork; }

	public bool CanRequest()
	{
		return m_client != null && ZoneSceneNetwork.Instance != null && !ZoneSceneNetwork.Instance.IsTransitioning();
	}

	internal bool Send(PacketStream _packet, ePacketType _type, ulong _number = 0)
	{
		if (!CanRequest() || m_info.IsPending() || !m_client.Write(_packet)) return false;
		m_info.BeginRequest(_type, _number);
		return true;
	}

	public bool Invite(string _name)
	{
		if (string.IsNullOrWhiteSpace(_name) || _name.IndexOf('\0') >= 0 || m_info.partyNum == 0) return false;
		byte[] name = Encoding.UTF8.GetBytes(_name.Trim());
		if (name.Length == 0 || name.Length > ChatInputState.MaxNameBytes) return false;
		PacketStream packet = new PacketStream(13 + name.Length);
		packet.Write(ePacketType.C2S_InviteParty);
		packet.Write(m_info.partyNum);
		packet.Write((byte)name.Length);
		packet.Write(name);
		return Send(packet, ePacketType.C2S_InviteParty, m_info.partyNum);
	}

	public bool Answer(ulong _inviteNum, bool _accept)
	{
		if (_inviteNum == 0 || _inviteNum != m_info.inviteNum) return false;
		PacketStream packet = new PacketStream(13);
		packet.Write(ePacketType.C2S_AnswerPartyInvite);
		packet.Write(_inviteNum);
		byte accept = 0;
		if (_accept) accept = 1;
		packet.Write(accept);
		return Send(packet, ePacketType.C2S_AnswerPartyInvite, _inviteNum);
	}

	public bool Manage(ePacketType _type, ulong _partyNum, ulong _sessionId = 0, ulong _characterIndex = 0)
	{
		if (_partyNum == 0 || _partyNum != m_info.partyNum) return false;
		int size = 12;
		bool target = _type == ePacketType.C2S_KickParty || _type == ePacketType.C2S_ChangePartyLeader;
		if (target) size = 28;
		else if (_type != ePacketType.C2S_LeaveParty && _type != ePacketType.C2S_DisbandParty) return false;
		PacketStream packet = new PacketStream(size);
		packet.Write(_type);
		packet.Write(_partyNum);
		if (target)
		{
			packet.Write(_sessionId);
			packet.Write(_characterIndex);
		}
		return Send(packet, _type, _partyNum);
	}

	public bool SendChat(string _text)
	{
		if (!CanRequest() || m_info.partyNum == 0 || string.IsNullOrWhiteSpace(_text) || ChatInputState.LimitInput(_text) != _text) return false;
		byte[] text = Encoding.UTF8.GetBytes(_text);
		if (text.Length > byte.MaxValue) return false;
		PacketStream packet = new PacketStream(13 + text.Length);
		packet.Write(ePacketType.C2S_PartyChat);
		packet.Write(m_info.partyNum);
		packet.Write((byte)text.Length);
		packet.Write(text);
		return m_client.Write(packet);
	}

	public bool ReadPacket(byte[] _packet)
	{
		if (_packet == null || _packet.Length < 4) return false;
		ePacketType type = (ePacketType)BitConverter.ToUInt16(_packet, 2);
		bool lobby = type >= ePacketType.S2C_PartyList && type <= ePacketType.S2C_PartyApplicationState;
		if (!lobby && (type < ePacketType.S2C_PartyResult || type > ePacketType.S2C_PartyChat)) return false;
		if (BitConverter.ToUInt16(_packet, 0) != _packet.Length) return true;
		try
		{
			if (lobby) m_lobbyNetwork.Read(type, _packet);
			else Read(type, _packet);
		}
		catch (DecoderFallbackException) { Debug.LogWarning("Party packet: invalid UTF-8"); }
		return true;
	}

	void Read(ePacketType _type, byte[] _packet)
	{
		int size = _packet.Length;
		if (_type == ePacketType.S2C_PartyResult && size == 15)
		{
			string message = GetResultText(_packet[6]);
			ePacketType request = (ePacketType)BitConverter.ToUInt16(_packet, 4);
			ulong number = BitConverter.ToUInt64(_packet, 7);
			if (request == ePacketType.C2S_PartyList)
			{
				if (m_info.lobby.listPending && number == m_info.lobby.requestedPage) m_info.lobby.CompleteList(message);
				return;
			}
			if (!m_info.CompleteRequest(request, message, number)) return;
			m_lobbyNetwork.Complete(request, number, _packet[6], message);
			if (_packet[6] != 0) m_history.AddSystem(message);
		}
		if (_type == ePacketType.S2C_PartyInvite && size >= 21)
		{
			int nameSize = _packet[20];
			if (nameSize == 0 || nameSize > 30 || size != 21 + nameSize) return;
			m_info.SetInvite(BitConverter.ToUInt64(_packet, 4), m_utf8.GetString(_packet, 21, nameSize));
		}
		if (_type == ePacketType.S2C_CancelPartyInvite && size == 13) m_info.CancelInvite(BitConverter.ToUInt64(_packet, 4));
		if (_type == ePacketType.S2C_PartyInfo && size >= 30)
		{
			ulong number = BitConverter.ToUInt64(_packet, 4);
			ulong createNum = BitConverter.ToUInt64(_packet, 12);
			int count = _packet[28];
			int nameSize = _packet[29];
			if (count > PartyInfo.MaxMembers || nameSize > 60 || size != 30 + nameSize + count * MemberSize || (number == 0) != (count == 0)) return;
			if (number != 0 && (nameSize == 0 || createNum == 0)) return;
			string name = m_utf8.GetString(_packet, 30, nameSize);
			List<PartyMemberInfo> members = new List<PartyMemberInfo>();
			for (int i = 0; i < count; ++i)
			{
				PartyMemberInfo member = ReadMember(_packet, 30 + nameSize + i * MemberSize);
				if (member == null) return;
				members.Add(member);
			}
			m_info.Apply(number, BitConverter.ToUInt64(_packet, 20), members, name, createNum);
			m_chatInput.SetParty(number != 0);
		}
		if (_type == ePacketType.S2C_PartyMemberStatus && size == 12 + StatusSize) m_info.ApplyStatus(BitConverter.ToUInt64(_packet, 4), ReadStatus(_packet, 12));
		if (_type == ePacketType.S2C_PartyChat && size >= 14)
		{
			int nameSize = _packet[12];
			int chatSize = _packet[13];
			if (nameSize == 0 || nameSize > 30 || chatSize == 0 || size != 14 + nameSize + chatSize) return;
			m_history.AddChat(eChatChannel.Party, m_utf8.GetString(_packet, 14, nameSize), m_utf8.GetString(_packet, 14 + nameSize, chatSize));
		}
	}

	internal static PartyMemberInfo ReadMember(byte[] _packet, int _offset)
	{
		if (_offset < 0 || _offset + MemberSize > _packet.Length) return null;
		int size = _packet[_offset + StatusSize + 8];
		if (size == 0 || size > 30) return null;
		PartyMemberInfo member = ReadStatus(_packet, _offset);
		member.jobClass = BitConverter.ToUInt64(_packet, _offset + StatusSize);
		member.name = m_utf8.GetString(_packet, _offset + StatusSize + 9, size);
		return member;
	}

	static PartyMemberInfo ReadStatus(byte[] _packet, int _offset)
	{
		PartyMemberInfo info = new PartyMemberInfo();
		info.sessionId = BitConverter.ToUInt64(_packet, _offset);
		info.characterIndex = BitConverter.ToUInt64(_packet, _offset + 8);
		info.level = BitConverter.ToUInt64(_packet, _offset + 16);
		info.zoneId = BitConverter.ToUInt64(_packet, _offset + 24);
		info.channelId = BitConverter.ToUInt64(_packet, _offset + 32);
		info.state = (ePartyMemberState)_packet[_offset + 40];
		info.hp = BitConverter.ToUInt64(_packet, _offset + 41);
		info.maxHP = BitConverter.ToUInt64(_packet, _offset + 49);
		return info;
	}

	public static string GetResultText(byte _result)
	{
		string[] messages = { "완료했습니다.", "이동이 끝난 후 다시 시도해주세요.", "해당 파티 또는 캐릭터가 없습니다.", "이미 파티에 가입되어 있습니다.",
			"파티장만 사용할 수 있습니다.", "파티 인원이 가득 찼습니다.", "상대방이 다른 초대에 응답 중입니다.", "선택한 대상을 확인해주세요.", "요청이 취소되었습니다.", "요청을 거절했습니다.",
			"파티명은 1~15글자, UTF-8 60바이트 이내로 입력해주세요.", "이미 다른 파티에 가입 신청 중입니다.", "이미 처리되었거나 취소된 가입 신청입니다." };
		if (_result >= messages.Length) return "요청을 처리하지 못했습니다.";
		return messages[_result];
	}
}
