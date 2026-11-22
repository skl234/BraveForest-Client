public enum ePartyMemberState : byte
{
	Ready,
	Moving,
	Dead,
	Offline,
}

public class PartyMemberInfo
{
	public ulong sessionId;
	public ulong characterIndex;
	public ulong level;
	public ulong zoneId;
	public ulong channelId;
	public ePartyMemberState state;
	public ulong hp;
	public ulong maxHP;
	public ulong jobClass;
	public string name = "";
}
