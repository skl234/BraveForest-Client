using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ZoneObjectManager
{
	GameObject m_localWarriorPrefab;
	GameObject m_localArcherPrefab;
	GameObject m_remoteWarriorPrefab;
	GameObject m_remoteArcherPrefab;
	GameObject[] m_monsterPrefabList;
	Transform m_objectRoot;
	readonly Dictionary<string, GameObject> m_objectMap = new Dictionary<string, GameObject>();

	public void Initialize(GameObject _localWarrior, GameObject _localArcher, GameObject _remoteWarrior, GameObject _remoteArcher, GameObject[] _monsters, Transform _root)
	{
		m_localWarriorPrefab = _localWarrior;
		m_localArcherPrefab = _localArcher;
		m_remoteWarriorPrefab = _remoteWarrior;
		m_remoteArcherPrefab = _remoteArcher;
		m_monsterPrefabList = _monsters;
		m_objectRoot = _root;
	}

	public bool TryFind(byte _type, ulong _number, out GameObject _object)
	{
		return m_objectMap.TryGetValue(Key(_type, _number), out _object) && _object != null;
	}

	public GameObject Create(byte[] _packet, int _offset, bool _local, ulong _experience = 0, ulong _requiredExperience = 0)
	{
		byte objectType = _packet[_offset];
		ulong objectNum = BitConverter.ToUInt64(_packet, _offset + 1);
		ulong dataIndex = BitConverter.ToUInt64(_packet, _offset + 9);
		ulong jobClass = BitConverter.ToUInt64(_packet, _offset + 17);
		ulong level = BitConverter.ToUInt64(_packet, _offset + 25);
		ulong maxHP = BitConverter.ToUInt64(_packet, _offset + 33);
		ulong hp = BitConverter.ToUInt64(_packet, _offset + 41);
		Vector3 position = new Vector3(BitConverter.ToSingle(_packet, _offset + 49), BitConverter.ToSingle(_packet, _offset + 53), BitConverter.ToSingle(_packet, _offset + 57));
		float rotationY = BitConverter.ToSingle(_packet, _offset + 61);
		int nameSize = _packet[_offset + 66];
		if (nameSize > 30) nameSize = 30;
		string objectName = "";
		if (nameSize > 0) objectName = Encoding.UTF8.GetString(_packet, _offset + 67, nameSize);
		GameObject prefab = null;
		if (objectType == 0)
		{
			if (_local)
			{
				prefab = m_localWarriorPrefab;
				if (jobClass != 0) prefab = m_localArcherPrefab;
			}
			else
			{
				prefab = m_remoteWarriorPrefab;
				if (jobClass != 0) prefab = m_remoteArcherPrefab;
			}
		}
		else if (dataIndex < (ulong)m_monsterPrefabList.Length) prefab = m_monsterPrefabList[(int)dataIndex];
		if (prefab == null) return null;
		string objectKey = Key(objectType, objectNum);
		bool created = !m_objectMap.TryGetValue(objectKey, out GameObject instance) || instance == null;
		if (created)
		{
			instance = UnityEngine.Object.Instantiate(prefab, ResolveHeight(position), Quaternion.Euler(0.0f, rotationY, 0.0f), m_objectRoot);
			MonsterWander wander = instance.GetComponent<MonsterWander>();
			if (wander != null) wander.enabled = false;
			MonsterObject serverMonster = instance.GetComponent<MonsterObject>();
			if (serverMonster != null) serverMonster.InitializeServer();
			ZoneObjectIdentity identity = instance.GetComponent<ZoneObjectIdentity>();
			if (identity == null) identity = instance.AddComponent<ZoneObjectIdentity>();
			identity.Initialize(objectType, objectNum);
			m_objectMap[objectKey] = instance;
		}
		// 이미 보이는 대상은 외형/경로/애니메이션/타겟을 유지하고 정보만 갱신
		if (objectType == 0)
		{
			PlayerInfo playerInfo = instance.GetComponent<PlayerInfo>();
			if (playerInfo == null) playerInfo = instance.AddComponent<PlayerInfo>();
			if (!created && !_local)
			{
				_experience = playerInfo.GetExperience();
				_requiredExperience = playerInfo.GetRequiredExperience();
			}
			playerInfo.Initialize(objectName, (int)level, (int)maxHP, (int)hp, _experience, _requiredExperience);
		}
		else
		{
			MonsterCombatStatus monsterStatus = instance.GetComponent<MonsterCombatStatus>();
			if (monsterStatus != null) monsterStatus.InitializeServerData((eMonsterType)dataIndex, (uint)level, (uint)maxHP, (uint)hp);
			if (created)
			{
				MonsterObject monster = instance.GetComponent<MonsterObject>();
				if (monster != null && _packet[_offset + 65] == 5) monster.SetState(eMonsterState.Dead);
			}
		}
		return instance;
	}

	public void Remove(byte _type, ulong _num, LocalPlayerObject _localPlayer)
	{
		string key = Key(_type, _num);
		if (!m_objectMap.TryGetValue(key, out GameObject instance)) return;
		if (instance != null && _localPlayer != null && instance == _localPlayer.gameObject) return;
		m_objectMap.Remove(key);
		if (instance == null) return;
		if (_localPlayer != null)
		{
			PlayerCombat combat = _localPlayer.GetComponent<PlayerCombat>();
			if (combat != null && combat.GetTarget() != null && combat.GetTarget().transform.IsChildOf(instance.transform)) combat.ClearTarget();
		}
		// Destroy는 프레임 끝에 처리됨. 그 전에 화면/클릭 판정에서 제외
		instance.SetActive(false);
		UnityEngine.Object.Destroy(instance);
	}

	public void Clear()
	{
		Dictionary<string, GameObject>.Enumerator iter = m_objectMap.GetEnumerator();
		while (iter.MoveNext())
		{
			KeyValuePair<string, GameObject> pair = iter.Current;
			if (pair.Value == null) continue;
			pair.Value.SetActive(false);
			UnityEngine.Object.Destroy(pair.Value);
		}
		m_objectMap.Clear();
	}

	public void ShowChatBubble(ChatMessage _message)
	{
		if (!_message.CanShowBubble()) return;
		Dictionary<string, GameObject>.Enumerator iter = m_objectMap.GetEnumerator();
		while (iter.MoveNext())
		{
			KeyValuePair<string, GameObject> pair = iter.Current;
			if (pair.Value == null) continue;
			PlayerInfo info = pair.Value.GetComponent<PlayerInfo>();
			if (info == null || info.GetName() != _message.name) continue;
			PlayerWorldUI worldUI = pair.Value.GetComponentInChildren<PlayerWorldUI>(true);
			if (worldUI != null) worldUI.ShowChat(_message.text);
			return;
		}
	}

	public static Vector3 ResolveHeight(Vector3 _position)
	{
		Terrain terrain = Terrain.activeTerrain;
		if (terrain != null) _position.y = terrain.SampleHeight(_position) + terrain.transform.position.y;
		return _position;
	}

	string Key(byte _type, ulong _num) { return _type.ToString() + ":" + _num.ToString(); }
}
