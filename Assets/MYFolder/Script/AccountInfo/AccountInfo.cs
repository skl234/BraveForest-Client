using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class AccountInfo : MonoBehaviour
{
    UInt64 m_accountIndex = 0;
    UInt64  m_token = 0;
	UInt64 m_characterIndex;
	UInt64 m_jobClass;
	UInt64 m_level;
	UInt64 m_experience;
	UInt64 m_zoneId;
	Vector3 m_position;
	byte[] m_enterZonePacket;
	bool m_zoneTransferPending;
	bool m_respawnPending;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
    public void SetAccountIndex(UInt64 _index)
    {
        m_accountIndex = _index;
    }
    public UInt64 GetAccountIndex()
    {
        return m_accountIndex;
    }
    public void SetToken(UInt64 _token)
    {
        m_token = _token;
    }    
    public UInt64 GetToken()
    {
        return m_token;
    }
	public void SetCharacter(UInt64 _index, UInt64 _jobClass, UInt64 _level,
		UInt64 _experience, UInt64 _zoneId, Vector3 _position)
	{
		m_characterIndex = _index; m_jobClass = _jobClass; m_level = _level;
		m_experience = _experience; m_zoneId = _zoneId; m_position = _position;
	}
	public UInt64 GetCharacterIndex() { return m_characterIndex; }
	public UInt64 GetJobClass() { return m_jobClass; }
	public UInt64 GetLevel() { return m_level; }
	public UInt64 GetExperience() { return m_experience; }
	public void SetProgress(UInt64 _level, UInt64 _experience)
	{
		m_level = _level;
		m_experience = _experience;
	}
	public UInt64 GetZoneId() { return m_zoneId; }
	public Vector3 GetPosition() { return m_position; }
	public void SetEnterZonePacket(byte[] _packet) { m_enterZonePacket = _packet; }
	public byte[] GetEnterZonePacket() { return m_enterZonePacket; }
	public void BeginZoneTransfer(UInt64 _zoneId, Vector3 _position)
	{
		m_zoneId = _zoneId;
		m_position = _position;
		m_enterZonePacket = null;
		m_zoneTransferPending = true;
	}
	public bool IsZoneTransferPending() { return m_zoneTransferPending; }
	public void BeginRespawnTransfer(UInt64 _zoneId, Vector3 _position)
	{
		BeginZoneTransfer(_zoneId, _position);
		m_respawnPending = true;
	}
	public bool IsRespawnPending() { return m_respawnPending; }
	public void CompleteZoneTransfer() { m_zoneTransferPending = false; m_respawnPending = false; }
}
