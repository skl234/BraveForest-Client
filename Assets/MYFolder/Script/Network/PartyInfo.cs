using System.Collections.Generic;

// 필드 오브젝트와 분리된 표시용 정보. 씬이 바뀌어도 NetClient가 보관
public class PartyInfo
{
	public readonly PartyLobbyInfo lobby;
	public const int MaxMembers = 4;
	readonly List<PartyMemberInfo> m_members = new List<PartyMemberInfo>();
	public ulong partyNum { get; private set; }
	public ulong createNum { get; private set; }
	public string partyName = "모험가 파티";
	public ulong leaderCharacterIndex { get; private set; }
	public uint revision { get; private set; }
	public ulong inviteNum { get; private set; }
	public string inviterName { get; private set; } = "";
	public ePacketType pendingRequest { get; private set; } = ePacketType.End;
	public ulong pendingNumber { get; private set; }
	public string resultText { get; private set; } = "";
	public IReadOnlyList<PartyMemberInfo> GetMembers() { return m_members; }
	public bool IsPending() { return pendingRequest != ePacketType.End; }
	public PartyInfo() { lobby = new PartyLobbyInfo(); }

	public PartyMemberInfo Find(ulong _sessionId, ulong _characterIndex)
	{
		for (int i = 0; i < m_members.Count; ++i)
		{
			if (m_members[i].sessionId == _sessionId && m_members[i].characterIndex == _characterIndex) return m_members[i];
		}
		return null;
	}

	public PartyMemberInfo FindCharacter(ulong _characterIndex)
	{
		for (int i = 0; i < m_members.Count; ++i)
		{
			if (m_members[i].characterIndex == _characterIndex) return m_members[i];
		}
		return null;
	}

	public void Apply(ulong _partyNum, ulong _leader, List<PartyMemberInfo> _members, string _name = null, ulong _createNum = 0)
	{
		if (partyNum != _partyNum || createNum != _createNum)
		{
			if (partyNum != 0) lobby.Clear();
			else lobby.ClearRequests();
			if (_partyNum != 0) CancelInvite(inviteNum);
		}
		else if (leaderCharacterIndex != _leader) lobby.ClearApplications();
		partyNum = _partyNum;
		createNum = _createNum;
		if (_name != null) partyName = _name;
		if (_partyNum == 0) partyName = "모험가 파티";
		leaderCharacterIndex = _leader;
		m_members.Clear();
		m_members.AddRange(_members);
		++revision;
	}

	public void ApplyStatus(ulong _partyNum, PartyMemberInfo _status)
	{
		if (_partyNum == 0 || _partyNum != partyNum) return;
		PartyMemberInfo member = Find(_status.sessionId, _status.characterIndex);
		if (member == null) return;
		member.level = _status.level;
		member.zoneId = _status.zoneId;
		member.channelId = _status.channelId;
		member.state = _status.state;
		member.hp = _status.hp;
		member.maxHP = _status.maxHP;
		++revision;
	}

	public void SetInvite(ulong _number, string _name)
	{
		inviteNum = _number;
		inviterName = _name;
		++revision;
	}

	public void CancelInvite(ulong _number)
	{
		if (_number != inviteNum) return;
		inviteNum = 0;
		inviterName = "";
		++revision;
	}

	public void BeginRequest(ePacketType _type, ulong _number = 0)
	{
		pendingRequest = _type;
		pendingNumber = _number;
		resultText = "요청 중...";
		++revision;
	}

	public bool CompleteRequest(ePacketType _type, string _message, ulong _number = 0)
	{
		if (pendingRequest != _type || pendingNumber != _number) return false;
		pendingRequest = ePacketType.End;
		pendingNumber = 0;
		resultText = _message;
		++revision;
		return true;
	}

	public void Clear()
	{
		lobby.Clear();
		partyNum = 0;
		createNum = 0;
		partyName = "모험가 파티";
		leaderCharacterIndex = 0;
		m_members.Clear();
		inviteNum = 0;
		inviterName = "";
		pendingRequest = ePacketType.End;
		pendingNumber = 0;
		resultText = "";
		++revision;
	}
}
