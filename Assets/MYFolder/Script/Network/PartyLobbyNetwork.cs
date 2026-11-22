using System;
using System.Collections.Generic;
using System.Text;

public class PartyLobbyNetwork : IPartyLobbyRequests
{
	readonly NetClient m_client;
	readonly PartyInfo m_info;
	readonly PartyNetwork m_partyNetwork;
	readonly List<PartyApplicationInfo> m_applications = new List<PartyApplicationInfo>();
	static readonly UTF8Encoding m_utf8 = new UTF8Encoding(false, true);
	ulong m_applicationParty;
	ulong m_applicationCreateNum;
	ulong m_applicationCount;

	public PartyLobbyNetwork(NetClient _client, PartyInfo _info, PartyNetwork _partyNetwork)
	{
		m_client = _client;
		m_info = _info;
		m_partyNetwork = _partyNetwork;
	}

	public bool Refresh(ulong _page)
	{
		if (!m_partyNetwork.CanRequest() || m_info.IsPending() || m_info.lobby.listPending) return false;
		PacketStream packet = new PacketStream(12);
		packet.Write(ePacketType.C2S_PartyList);
		packet.Write(_page);
		if (!m_client.Write(packet)) return false;
		m_info.lobby.BeginListRequest(_page);
		return true;
	}

	public bool Create(string _name)
	{
		if (m_info.partyNum != 0 || string.IsNullOrWhiteSpace(_name)) return false;
		_name = _name.Trim();
		int count = 0;
		for (int i = 0; i < _name.Length; ++i)
		{
			if (char.IsControl(_name[i]) || _name[i] == '<' || _name[i] == '>') return false;
			if (!char.IsLowSurrogate(_name[i])) ++count;
		}
		if (count > 15) return false;
		byte[] name;
		try { name = m_utf8.GetBytes(_name); }
		catch (EncoderFallbackException) { return false; }
		if (name.Length == 0 || name.Length > 60) return false;
		PacketStream packet = new PacketStream(5 + name.Length);
		packet.Write(ePacketType.C2S_CreateParty);
		packet.Write((byte)name.Length);
		packet.Write(name);
		return m_partyNetwork.Send(packet, ePacketType.C2S_CreateParty);
	}

	public bool Join(ulong _partyNum)
	{
		if (m_info.partyNum != 0 || m_info.lobby.requestedParty != 0 || m_info.lobby.listPending) return false;
		IReadOnlyList<PartyListingInfo> list = m_info.lobby.GetParties();
		for (int i = 0; i < list.Count; ++i)
		{
			if (list[i].partyNum != _partyNum || list[i].createNum == 0 || list[i].members.Count >= PartyInfo.MaxMembers) continue;
			PacketStream packet = new PacketStream(20);
			packet.Write(ePacketType.C2S_ApplyParty);
			packet.Write(_partyNum);
			packet.Write(list[i].createNum);
			return m_partyNetwork.Send(packet, ePacketType.C2S_ApplyParty, _partyNum);
		}
		return false;
	}

	public bool Cancel(ulong _partyNum)
	{
		if (_partyNum == 0 || m_info.lobby.requestedParty != _partyNum || m_info.lobby.requestNum == 0) return false;
		PacketStream packet = new PacketStream(12);
		packet.Write(ePacketType.C2S_CancelPartyApplication);
		packet.Write(m_info.lobby.requestNum);
		return m_partyNetwork.Send(packet, ePacketType.C2S_CancelPartyApplication, m_info.lobby.requestNum);
	}

	public bool Answer(ulong _requestNum, bool _accept)
	{
		if (m_info.partyNum == 0 || m_info.lobby.FindApplication(_requestNum) == null) return false;
		PacketStream packet = new PacketStream(13);
		packet.Write(ePacketType.C2S_AnswerPartyApplication);
		packet.Write(_requestNum);
		byte accept = 0;
		if (_accept) accept = 1;
		packet.Write(accept);
		return m_partyNetwork.Send(packet, ePacketType.C2S_AnswerPartyApplication, _requestNum);
	}

	public void Complete(ePacketType _type, ulong _number, byte _result, string _message)
	{
		if (_type == ePacketType.C2S_CancelPartyApplication && _result == (byte)ePartyResult.ApplicationNotFound && m_info.lobby.requestNum == _number) m_info.lobby.SetRequestedParty(0);
		ulong requestNum = 0;
		if (_type == ePacketType.C2S_AnswerPartyApplication) requestNum = _number;
		m_info.lobby.Complete(_message, requestNum);
	}

	public void Read(ePacketType _type, byte[] _packet)
	{
		if (_type == ePacketType.S2C_PartyList) ReadList(_packet);
		else if (_type == ePacketType.S2C_PartyApplicationList) ReadApplications(_packet);
		else if (_type == ePacketType.S2C_PartyApplicationState) ReadApplicationState(_packet);
	}

	void ReadList(byte[] _packet)
	{
		if (_packet.Length < 21 || !m_info.lobby.listPending) return;
		ulong page = BitConverter.ToUInt64(_packet, 4);
		ulong total = BitConverter.ToUInt64(_packet, 12);
		int count = _packet[20];
		if (count > 5 || (ulong)count > total || total > 100000) return;
		if ((total == 0 && page != 0) || (total != 0 && page > (total - 1) / 5)) return;
		List<PartyListingInfo> list = new List<PartyListingInfo>();
		int offset = 21;
		for (int i = 0; i < count; ++i)
		{
			if (offset + 26 > _packet.Length) return;
			PartyListingInfo entry = new PartyListingInfo();
			entry.partyNum = BitConverter.ToUInt64(_packet, offset);
			entry.createNum = BitConverter.ToUInt64(_packet, offset + 8);
			ulong leader = BitConverter.ToUInt64(_packet, offset + 16);
			int members = _packet[offset + 24];
			int nameSize = _packet[offset + 25];
			offset += 26;
			if (entry.partyNum == 0 || entry.createNum == 0 || members == 0 || members > PartyInfo.MaxMembers || nameSize == 0 || nameSize > 60 || offset + nameSize + members * PartyNetwork.MemberSize > _packet.Length) return;
			entry.name = m_utf8.GetString(_packet, offset, nameSize);
			offset += nameSize;
			for (int j = 0; j < members; ++j)
			{
				PartyMemberInfo member = PartyNetwork.ReadMember(_packet, offset);
				if (member == null) return;
				entry.members.Add(member);
				if (member.characterIndex == leader) entry.leaderName = member.name;
				offset += PartyNetwork.MemberSize;
			}
			list.Add(entry);
		}
		if (offset != _packet.Length) return;
		m_info.lobby.SetParties(list, page, total);
	}

	void ReadApplications(byte[] _packet)
	{
		if (_packet.Length < 38) return;
		ulong party = BitConverter.ToUInt64(_packet, 4);
		ulong create = BitConverter.ToUInt64(_packet, 12);
		ulong total = BitConverter.ToUInt64(_packet, 20);
		ulong start = BitConverter.ToUInt64(_packet, 28);
		int count = BitConverter.ToUInt16(_packet, 36);
		if (party == 0 || party != m_info.partyNum || create != m_info.createNum || total > 100000 || start > total || (ulong)count > total - start || count > 128 || _packet.Length != 38 + count * 55) return;
		if (start == 0)
		{
			m_applications.Clear();
			m_applicationParty = party;
			m_applicationCreateNum = create;
			m_applicationCount = total;
		}
		if (m_applicationParty != party || m_applicationCreateNum != create || m_applicationCount != total || (ulong)m_applications.Count != start) return;
		List<PartyApplicationInfo> page = new List<PartyApplicationInfo>();
		for (int i = 0; i < count; ++i)
		{
			int offset = 38 + i * 55;
			int nameSize = _packet[offset + 24];
			if (nameSize == 0 || nameSize > 30) return;
			page.Add(new PartyApplicationInfo { requestNum = BitConverter.ToUInt64(_packet, offset), level = BitConverter.ToUInt64(_packet, offset + 8), jobClass = BitConverter.ToUInt64(_packet, offset + 16), name = m_utf8.GetString(_packet, offset + 25, nameSize) });
		}
		m_applications.AddRange(page);
		if ((ulong)m_applications.Count == total) m_info.lobby.SetApplications(m_applications);
	}

	void ReadApplicationState(byte[] _packet)
	{
		if (_packet.Length != 30 || _packet[28] > 1) return;
		ulong request = BitConverter.ToUInt64(_packet, 4);
		ulong party = BitConverter.ToUInt64(_packet, 12);
		if (_packet[28] != 0)
		{
			if (m_info.partyNum != 0) return;
			m_info.lobby.SetRequestedParty(party, request);
			m_info.lobby.Complete("가입 신청을 보냈습니다.");
		}
		else if (m_info.lobby.requestNum == request)
		{
			m_info.lobby.SetRequestedParty(0);
			m_info.lobby.Complete(PartyNetwork.GetResultText(_packet[29]));
		}
	}
}
