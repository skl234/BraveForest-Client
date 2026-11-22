public interface IPartyUIActions
{
	bool CanRequest();
	bool Invite(string _name);
	bool Answer(ulong _inviteNum, bool _accept);
	bool Manage(ePacketType _type, ulong _partyNum, ulong _sessionId = 0, ulong _characterIndex = 0);
}
