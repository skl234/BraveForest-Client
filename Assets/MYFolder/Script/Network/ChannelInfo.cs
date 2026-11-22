using System;

public struct ChannelInfo
{
	public ulong channelId;
	public ulong playerCount;
	public ulong maxPlayerCount;
	public bool available;

	public static bool TryReadList(byte[] _packet, out ChannelInfo[] _list)
	{
		_list = null;
		if (_packet == null || _packet.Length < 12) return false;
		ulong count = BitConverter.ToUInt64(_packet, 4);
		if (count > (ulong)((_packet.Length - 12) / 25)) return false;
		_list = new ChannelInfo[(int)count];
		for (int i = 0; i < _list.Length; ++i)
		{
			int offset = 12 + i * 25;
			_list[i].channelId = BitConverter.ToUInt64(_packet, offset);
			_list[i].playerCount = BitConverter.ToUInt64(_packet, offset + 8);
			_list[i].maxPlayerCount = BitConverter.ToUInt64(_packet, offset + 16);
			_list[i].available = _packet[offset + 24] != 0;
		}
		return true;
	}
}
