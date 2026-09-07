using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text;
using System;
public class PacketStream
{
    private MemoryStream m_stream = null;
    public PacketStream(int _size)
    {
        m_stream = new MemoryStream(_size);
        m_stream.Write(BitConverter.GetBytes((ushort)sizeof(ushort)));
    }
    public void Write(ePacketType _type)
    {
        m_stream.Write(BitConverter.GetBytes((ushort)_type));
        Resize();
    }
    public void Write(int _val)
    {
        m_stream.Write(BitConverter.GetBytes(_val));
        Resize();
    }
    public void Write(UInt64 _val)
    {
        m_stream.Write(BitConverter.GetBytes(_val));
        Resize();
    }
	public void Write(float _val)
	{
		m_stream.Write(BitConverter.GetBytes(_val));
		Resize();
	}
    public void Write(byte _val)
    {
        m_stream.WriteByte(_val);
        Resize();
    }
    public void Write(byte[] _val)
    {
        m_stream.Write(_val, 0, _val.Length);
        Resize();
    }
    public void Write(string _str)
    {
        m_stream.Write(Encoding.Unicode.GetBytes(_str));
        Resize();
    }
    public byte[] GetBuffer()
    {
        return m_stream.GetBuffer();
    }
    public int GetSize()
    {
        return (int)m_stream.Position;
    }
    private void Resize()
    {
        long positionTemp = m_stream.Position;
        m_stream.Position = 0;
        m_stream.Write(BitConverter.GetBytes((ushort)positionTemp));
        m_stream.Position = positionTemp;
    }
}
