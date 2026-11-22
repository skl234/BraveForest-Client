using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Buffers;
using System;
public class CCircleQueue
{
	private readonly object m_lockObject;
	private byte[] m_buff;
	private int m_readPosition;
	private int m_writePosition;
	private int m_size;

	public CCircleQueue(int _capacity)
	{
		m_lockObject = new object();
		m_buff = new byte[_capacity];
		m_readPosition = 0;
		m_writePosition = 0;
		m_size = 0;
	}
	public void Clean()
	{
		lock (m_lockObject)
		{
			m_readPosition = 0;
			m_writePosition = 0;
			m_size = 0;
		}
	}
	public int GetSize()
	{
		int size = 0;
		lock (m_lockObject)
		{
			size = m_size;
		}
		return size;
	}
	public int GetWriteAbleSize()
	{
		int writeAbleSize = 0;
		lock (m_lockObject)
		{
			if (m_size == 0 || m_readPosition < m_writePosition)
			{
				writeAbleSize = m_buff.Length - m_writePosition;
			}
			else writeAbleSize = m_readPosition - m_writePosition;
		}

		return writeAbleSize;
	}
	public int GetReadAbleSize()
	{
		int readAbleSize = 0;
		lock (m_lockObject)
		{
			if (m_size == 0 || m_readPosition < m_writePosition)
			{
				readAbleSize = m_size;
			}
			else readAbleSize = m_buff.Length - m_readPosition;
		}
		return readAbleSize;
	}
	public int Push(byte[] _src)
	{
		return Push(_src, _src.Length);
	}
	public int Push(byte[] _src, int _length)
	{
		int pushSrcLength = 0;
		lock (m_lockObject)
		{
			if (m_size + _length > m_buff.Length)
			{
				pushSrcLength = 0;
			}
			else
			{
				if (m_writePosition + _length > m_buff.Length)
				{
					Buffer.BlockCopy(_src, 0, m_buff, m_writePosition, GetWriteAbleSize());
					Buffer.BlockCopy(_src, GetWriteAbleSize(), m_buff, 0, _length - GetWriteAbleSize());
				}
				else
				{
					Buffer.BlockCopy(_src, 0, m_buff, m_writePosition, _length);
				}

				m_size += _length;
				m_writePosition = (m_writePosition + _length) % m_buff.Length;

				pushSrcLength = _length;
			}
		}
		return pushSrcLength;
	}

	public byte[] Pop(int _length)
	{
		byte[] dst = new byte[_length];

		lock (m_lockObject)
		{
			if (m_size - _length < 0)
			{
				dst = null;
			}
			else
			{
				if (GetReadAbleSize() < _length)
				{
					Buffer.BlockCopy(m_buff, m_readPosition, dst, 0, GetReadAbleSize());
					Buffer.BlockCopy(m_buff, 0, dst, GetReadAbleSize(), _length - GetReadAbleSize());
				}
				else
				{
					Buffer.BlockCopy(m_buff, m_readPosition, dst, 0, _length);
				}

				m_size -= _length;
				m_readPosition = (m_readPosition + _length) % m_buff.Length;
			}
		}

		return dst;
	}

	public bool TryPopPacket(out byte[] _packet)
	{
		_packet = null;

		lock (m_lockObject)
		{
			if (m_size < sizeof(ushort)) return false;

			ushort packetSize;
			if (m_readPosition + sizeof(ushort) <= m_buff.Length)
			{
				packetSize = BitConverter.ToUInt16(m_buff, m_readPosition);
			}
			else
			{
				byte[] sizeBuffer = { m_buff[m_readPosition], m_buff[0] };
				packetSize = BitConverter.ToUInt16(sizeBuffer, 0);
			}

			if (packetSize < 4 || packetSize > m_buff.Length)
			{
				Clean();
				return false;
			}
			if (m_size < packetSize) return false;

			_packet = Pop(packetSize);
			return _packet != null;
		}
	}

	public bool GetWord(ref ushort _des)
	{
		bool result = false;

		lock (m_lockObject)
		{
			if (m_size < sizeof(ushort))
			{
				result = false;
			}
			else
			{
				if (GetReadAbleSize() < sizeof(ushort))
				{
					byte[] byteArray = new byte[2];
					byteArray[0] = m_buff[m_readPosition];
					byteArray[1] = m_buff[0];
					_des = BitConverter.ToUInt16(byteArray);
				}
				else
				{
					_des = BitConverter.ToUInt16(m_buff, m_readPosition);
				}

				result = true;
			}
		}

		return result;
	}
};
