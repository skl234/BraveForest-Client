using System;
using System.Text;

public class ChatNetwork
{
	readonly NetClient m_client;
	readonly ChatHistory m_history;
	readonly ChatInputState m_inputState;

	public ChatNetwork(NetClient _client, ChatHistory _history, ChatInputState _inputState)
	{
		m_client = _client;
		m_history = _history;
		m_inputState = _inputState;
	}

	public bool Send(eChatChannel _channel, string _targetName, string _text)
	{
		if (_channel != eChatChannel.Region && _channel != eChatChannel.World && _channel != eChatChannel.Whisper && _channel != eChatChannel.Party) return false;
		if (string.IsNullOrWhiteSpace(_text) || _text.StartsWith("/", StringComparison.Ordinal)) return false;
		if (ChatInputState.LimitInput(_text) != _text || m_client == null) return false;
		if (_channel == eChatChannel.Party) return m_client.GetPartyNetwork().SendChat(_text);
		byte[] text = Encoding.UTF8.GetBytes(_text);
		if (text.Length > byte.MaxValue) return false;
		if (_channel == eChatChannel.Whisper)
		{
			if (string.IsNullOrWhiteSpace(_targetName) || _targetName.IndexOf('\0') >= 0) return false;
			byte[] name = Encoding.UTF8.GetBytes(_targetName);
			if (name.Length > ChatInputState.MaxNameBytes) return false;
			PacketStream whisper = new PacketStream(6 + name.Length + text.Length);
			whisper.Write(ePacketType.C2S_Whisper);
			whisper.Write((byte)name.Length);
			whisper.Write((byte)text.Length);
			whisper.Write(name);
			whisper.Write(text);
			return m_client.Write(whisper);
		}
		ePacketType type = ePacketType.C2S_Chat;
		if (_channel == eChatChannel.World) type = ePacketType.C2S_WorldChat;
		PacketStream stream = new PacketStream(5 + text.Length);
		stream.Write(type);
		stream.Write((byte)text.Length);
		stream.Write(text);
		return m_client.Write(stream);
	}

	public bool ReadPacket(byte[] _packet)
	{
		if (_packet == null || _packet.Length < 4) return false;
		ePacketType type = (ePacketType)BitConverter.ToUInt16(_packet, 2);
		if (type == ePacketType.S2C_WhisperResult)
		{
			ReadWhisperResult(_packet);
			return true;
		}
		if (type != ePacketType.S2C_Chat && type != ePacketType.S2C_WorldChat && type != ePacketType.S2C_Whisper) return false;
		if (_packet.Length < 6 || BitConverter.ToUInt16(_packet, 0) != _packet.Length) return true;
		int nameSize = _packet[4];
		int chatSize = _packet[5];
		if (nameSize == 0 || chatSize == 0 || 6 + nameSize + chatSize != _packet.Length) return true;
		eChatChannel channel = eChatChannel.Region;
		if (type == ePacketType.S2C_WorldChat) channel = eChatChannel.World;
		if (type == ePacketType.S2C_Whisper) channel = eChatChannel.Whisper;
		string name = Encoding.UTF8.GetString(_packet, 6, nameSize);
		string text = Encoding.UTF8.GetString(_packet, 6 + nameSize, chatSize);
		m_history.AddChat(channel, name, text);
		return true;
	}

	void ReadWhisperResult(byte[] _packet)
	{
		if (_packet.Length < 7 || BitConverter.ToUInt16(_packet, 0) != _packet.Length) return;
		byte result = _packet[4];
		int nameSize = _packet[5];
		int chatSize = _packet[6];
		if (nameSize == 0 || nameSize > ChatInputState.MaxNameBytes || 7 + nameSize + chatSize != _packet.Length) return;
		if (result == 0)
		{
			if (chatSize == 0) return;
			string name = Encoding.UTF8.GetString(_packet, 7, nameSize);
			string text = Encoding.UTF8.GetString(_packet, 7 + nameSize, chatSize);
			// 응답이 오는 동안 바꾼 채팅 모드와 입력 내용은 그대로 유지
			m_inputState.RememberWhisper(name);
			m_history.Add(new ChatMessage(eChatChannel.Whisper, "", name, text, true));
			return;
		}
		if (chatSize != 0) return;
		if (result == 1) m_history.AddSystem("해당 캐릭터가 존재하지 않습니다.");
		if (result == 2) m_history.AddSystem("지금은 귓속말을 보낼 수 없습니다.");
	}
}
