using System;
using System.Globalization;
using System.Text;

public class ChatInputState
{
	public const int MaxTextLength = 15;
	public const int MaxNameBytes = 30;
	eChatChannel m_channel = eChatChannel.Region;
	string m_whisperName = "";
	string m_lastWhisperName = "";
	bool m_hasParty;

	public void SetParty(bool _hasParty)
	{
		m_hasParty = _hasParty;
		if (!_hasParty && m_channel == eChatChannel.Party) m_channel = eChatChannel.Region;
	}

	public eChatChannel GetChannel() { return m_channel; }
	public string GetWhisperName() { return m_whisperName; }
	public string GetLastWhisperName() { return m_lastWhisperName; }

	public void ChangeChannel()
	{
		if (m_channel == eChatChannel.Region) m_channel = eChatChannel.World;
		else if (m_channel == eChatChannel.World && m_hasParty) m_channel = eChatChannel.Party;
		else if ((m_channel == eChatChannel.World || m_channel == eChatChannel.Party) && m_lastWhisperName.Length > 0)
		{
			m_channel = eChatChannel.Whisper;
			m_whisperName = m_lastWhisperName;
		}
		else m_channel = eChatChannel.Region;
	}

	public bool SelectWhisper(string _name)
	{
		if (string.IsNullOrWhiteSpace(_name)) return false;
		string name = _name.Trim();
		if (Encoding.UTF8.GetByteCount(name) > MaxNameBytes) return false;
		for (int i = 0; i < name.Length; ++i)
		{
			if (char.IsControl(name[i])) return false;
		}
		m_whisperName = name;
		m_channel = eChatChannel.Whisper;
		return true;
	}

	public void RememberWhisper(string _name)
	{
		if (!string.IsNullOrWhiteSpace(_name)) m_lastWhisperName = _name;
	}

	public string GetPrefix()
	{
		if (m_channel == eChatChannel.Region) return "지역 :";
		if (m_channel == eChatChannel.World) return "전체 :";
		if (m_channel == eChatChannel.Party) return "파티 :";
		return m_whisperName + "에게 :";
	}

	public void Clear()
	{
		m_channel = eChatChannel.Region;
		m_whisperName = "";
		m_lastWhisperName = "";
		m_hasParty = false;
	}

	public static string LimitInput(string _value)
	{
		if (string.IsNullOrEmpty(_value)) return "";
		string value = _value.Replace("\r", "").Replace("\n", "").Replace("\t", "");
		if (value.StartsWith("/", StringComparison.Ordinal))
		{
			string name = value.Substring(1);
			int[] nameElements = StringInfo.ParseCombiningCharacters(name);
			for (int i = nameElements.Length; i > 0; --i)
			{
				int length = name.Length;
				if (i < nameElements.Length) length = nameElements[i];
				string part = name.Substring(0, length);
				if (Encoding.UTF8.GetByteCount(part) <= MaxNameBytes) return "/" + part;
			}
			return "/";
		}
		int[] elements = StringInfo.ParseCombiningCharacters(value);
		if (elements.Length > MaxTextLength) value = value.Substring(0, elements[MaxTextLength]);
		return value;
	}
}
