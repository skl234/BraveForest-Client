using System;
using System.Collections.Generic;
using System.Text;

public class ChatHistory
{
	const int ChatCapacity = 30;
	readonly Queue<ChatMessage> m_chats = new Queue<ChatMessage>(ChatCapacity);
	uint m_revision;
	ulong m_nextIndex;

	public event Action<ChatMessage> ChatReceived;
	public uint GetRevision() { return m_revision; }
	public int GetCount() { return m_chats.Count; }
	public ulong GetFirstIndex() { return m_nextIndex - (ulong)m_chats.Count; }

	public string GetText(List<int> _offsets = null)
	{
		StringBuilder text = new StringBuilder();
		if (_offsets != null) _offsets.Clear();
		ChatMessage[] list = m_chats.ToArray();
		for (int i = 0; i < list.Length; ++i)
		{
			if (i > 0) text.Append('\n');
			if (_offsets != null) _offsets.Add(text.Length);
			text.Append(list[i].GetDisplayText());
		}
		return text.ToString();
	}

	public void Clear()
	{
		m_chats.Clear();
		m_nextIndex = 0;
		++m_revision;
	}

	public void AddChat(eChatChannel _channel, string _name, string _chat)
	{
		Add(new ChatMessage(_channel, _name, "", _chat));
	}

	public void AddSystem(string _text)
	{
		Add(new ChatMessage(eChatChannel.Region, "", "", _text, false, true));
	}

	public void Add(ChatMessage _message)
	{
		if (string.IsNullOrEmpty(_message.text)) return;
		if (m_chats.Count >= ChatCapacity) m_chats.Dequeue();
		m_chats.Enqueue(_message);
		++m_nextIndex;
		++m_revision;
		ChatReceived?.Invoke(_message);
	}
}
