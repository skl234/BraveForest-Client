using UnityEngine;
using System;

public class AccountInfo : MonoBehaviour
{
	UInt64 m_characterIndex;
	UInt64 m_zoneId;
	byte[] m_enterZonePacket;
	bool m_zoneTransferPending;
	bool m_respawnPending;

	void Awake()
	{
		DontDestroyOnLoad(gameObject);
	}
	public void SetCharacter(UInt64 _index, UInt64 _zoneId)
	{
		m_characterIndex = _index;
		m_zoneId = _zoneId;
	}
	public UInt64 GetCharacterIndex() { return m_characterIndex; }
	public UInt64 GetZoneId() { return m_zoneId; }
	public void SetEnterZonePacket(byte[] _packet) { m_enterZonePacket = _packet; }
	public byte[] GetEnterZonePacket() { return m_enterZonePacket; }
	public void BeginZoneTransfer(UInt64 _zoneId)
	{
		m_zoneId = _zoneId;
		m_enterZonePacket = null;
		m_zoneTransferPending = true;
	}
	public bool IsZoneTransferPending() { return m_zoneTransferPending; }
	public void BeginRespawnTransfer(UInt64 _zoneId)
	{
		BeginZoneTransfer(_zoneId);
		m_respawnPending = true;
	}
	public bool IsRespawnPending() { return m_respawnPending; }
	public void CompleteZoneTransfer() { m_zoneTransferPending = false; m_respawnPending = false; }
}
