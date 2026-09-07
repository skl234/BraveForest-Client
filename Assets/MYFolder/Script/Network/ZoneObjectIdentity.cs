using UnityEngine;

public class ZoneObjectIdentity : MonoBehaviour
{
	[SerializeField] ulong m_objectNum;
	[SerializeField] byte m_objectType;

	public void Initialize(byte _objectType, ulong _objectNum)
	{
		m_objectType = _objectType;
		m_objectNum = _objectNum;
	}

	public byte GetObjectType() { return m_objectType; }
	public ulong GetObjectNum() { return m_objectNum; }
}
