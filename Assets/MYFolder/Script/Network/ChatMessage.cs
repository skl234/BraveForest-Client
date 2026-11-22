public enum eChatChannel
{
	Region,
	World,
	Whisper,
	Party,
}

public struct ChatMessage
{
	public readonly eChatChannel channel;
	public readonly string name;
	public readonly string targetName;
	public readonly string text;
	public readonly bool outgoing;
	public readonly bool system;

	public ChatMessage(eChatChannel _channel, string _name, string _targetName, string _text, bool _outgoing = false, bool _system = false)
	{
		channel = _channel;
		name = _name;
		targetName = _targetName;
		text = _text;
		outgoing = _outgoing;
		system = _system;
	}

	public bool CanShowBubble()
	{
		return !system && (channel == eChatChannel.Region || channel == eChatChannel.World);
	}

	public string GetDisplayText()
	{
		string body = text.Replace('\r', ' ').Replace('\n', ' ');
		if (system) return body;
		if (channel == eChatChannel.Region) return "[지역] " + name + " : " + body;
		if (channel == eChatChannel.World) return "[전체] " + name + " : " + body;
		if (channel == eChatChannel.Party) return "[파티] " + name + " : " + body;
		if (outgoing) return "[귓속말] " + targetName + "에게 : " + body;
		return "[귓속말] " + name + "에게서 : " + body;
	}
}
