using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ZoneSceneNetwork : MonoBehaviour
{
	public static ZoneSceneNetwork Instance { get; private set; }
	const int FieldObjectSize = 97;
	[SerializeField] GameObject m_localWarriorPrefab;
	[SerializeField] GameObject m_localArcherPrefab;
	[SerializeField] GameObject m_remoteWarriorPrefab;
	[SerializeField] GameObject m_remoteArcherPrefab;
	[SerializeField] GameObject[] m_monsterPrefabList;
	[SerializeField] Transform m_objectRoot;
	NetClient m_client;
	AccountInfo m_accountInfo;
	LocalPlayerObject m_localPlayer;
	readonly Dictionary<string, GameObject> m_objectMap = new Dictionary<string, GameObject>();
	float m_nextMoveSend;
	Vector3 m_lastDestination;
	bool m_hasMoveDestination;
	ulong m_localObjectNum;
	ZoneChatUI m_chatUI;
	bool m_waitingZoneChange;
	bool m_waitingChannelChange;
	bool m_waitingRespawn;
	ulong m_channelId;
	ZoneGameplayUI m_gameplayUI;
	ZoneChannelUI m_channelUI;

	public bool IsWaiting() { return m_waitingZoneChange || m_waitingChannelChange || m_waitingRespawn || m_localPlayer == null || m_localPlayer.GetState() == ePlayerObjectState.Dead; }
	public ulong GetChannelId() { return m_channelId; }

	void Start()
	{
		Instance = this;
		m_client = FindObjectOfType<NetClient>();
		m_accountInfo = FindObjectOfType<AccountInfo>();
		m_chatUI = FindObjectOfType<ZoneChatUI>(true);
		m_gameplayUI = FindObjectOfType<ZoneGameplayUI>(true);
		m_channelUI = FindObjectOfType<ZoneChannelUI>(true);
		if (m_client == null || m_accountInfo == null)
		{
			SceneManager.LoadScene("LoginScene");
			enabled = false;
			return;
		}
		OfflineCombatSession offlineSession = FindObjectOfType<OfflineCombatSession>();
		if (offlineSession != null) offlineSession.gameObject.SetActive(false);
		MonsterSpawnPoint[] previewList = FindObjectsOfType<MonsterSpawnPoint>();
		for (int i = 0; i < previewList.Length; ++i) previewList[i].gameObject.SetActive(false);
		if (m_accountInfo.IsZoneTransferPending())
		{
			PacketStream stream = new PacketStream(12);
			stream.Write(ePacketType.C2S_EnterZone);
			stream.Write(m_accountInfo.GetCharacterIndex());
			m_client.Write(stream);
		}
		else ReadEnterPacket(m_accountInfo.GetEnterZonePacket());
	}

	void OnDestroy() { if (Instance == this) Instance = null; }

	public void SendAttackStart(byte _attackType, MonsterHitbox _target)
	{
		if (m_client == null || IsWaiting()) return;
		ZoneObjectIdentity identity = null;
		if (_target != null) identity = _target.GetComponentInParent<ZoneObjectIdentity>();
		PacketStream stream = new PacketStream(30);
		stream.Write(ePacketType.C2S_AttackStart);
		stream.Write(_attackType);
		stream.Write((byte)1);
		stream.Write(identity == null ? 0UL : identity.GetObjectNum());
		WriteVector(stream, m_localPlayer.transform.position);
		stream.Write(m_localPlayer.transform.eulerAngles.y);
		m_client.Write(stream);
	}

	public void SendAttackExecute(byte _hitIndex = 0)
	{
		if (m_client == null || IsWaiting()) return;
		PacketStream stream = new PacketStream(21);
		stream.Write(ePacketType.C2S_AttackExecute);
		WriteVector(stream, m_localPlayer.transform.position);
		stream.Write(m_localPlayer.transform.eulerAngles.y);
		stream.Write(_hitIndex);
		m_client.Write(stream);
	}

	public void SendAttackCancel()
	{
		if (m_client == null || IsWaiting()) return;
		PacketStream stream = new PacketStream(4);
		stream.Write(ePacketType.C2S_AttackCancel);
		m_client.Write(stream);
	}

	public bool SendPotion()
	{
		if (m_client == null || IsWaiting()) return false;
		PacketStream stream = new PacketStream(20);
		stream.Write(ePacketType.C2S_UsePotion);
		WriteVector(stream, m_localPlayer.transform.position);
		stream.Write(m_localPlayer.transform.eulerAngles.y);
		return m_client.Write(stream);
	}

	public bool SendChat(string _chat)
	{
		if (m_client == null || IsWaiting()) return false;
		byte[] text = Encoding.UTF8.GetBytes(_chat);
		if (text.Length == 0 || _chat.Length > 15) return false;
		PacketStream stream = new PacketStream(5 + text.Length);
		stream.Write(ePacketType.C2S_Chat);
		stream.Write((byte)text.Length);
		stream.Write(text);
		return m_client.Write(stream);
	}

	public LocalPlayerObject GetLocalPlayer() { return m_localPlayer; }

	public bool RequestChannelList()
	{
		if (m_client == null || IsWaiting()) return false;
		PacketStream stream = new PacketStream(4);
		stream.Write(ePacketType.C2S_ChannelList);
		return m_client.Write(stream);
	}

	public bool RequestChangeChannel(ulong _channelId)
	{
		if (m_client == null || IsWaiting()) return false;
		m_localPlayer.GetComponent<PlayerCombat>().StopCombat();
		m_localPlayer.StopMove();
		SendMove();
		PacketStream stream = new PacketStream(12);
		stream.Write(ePacketType.C2S_ChangeChannel);
		stream.Write(_channelId);
		if (!m_client.Write(stream)) return false;
		m_waitingChannelChange = true;
		m_gameplayUI.SetLoading(true);
		return true;
	}

	public bool RequestChangeZone(ulong _portalId)
	{
		if (m_client == null || IsWaiting()) return false;
		m_localPlayer.GetComponent<PlayerCombat>().StopCombat();
		m_localPlayer.StopMove();
		SendMove();
		m_waitingZoneChange = true;
		m_gameplayUI.SetLoading(true);
		PacketStream stream = new PacketStream(12);
		stream.Write(ePacketType.C2S_ChangeZone);
		stream.Write(_portalId);
		if (m_client.Write(stream)) return true;
		m_waitingZoneChange = false;
		m_gameplayUI.SetLoading(false);
		return false;
	}

	public bool RequestRespawn()
	{
		if (m_client == null || m_accountInfo == null || m_waitingRespawn || m_waitingZoneChange || m_waitingChannelChange) return false;
		bool retryEnter = m_accountInfo.IsRespawnPending();
		if (!retryEnter && (m_localPlayer == null || m_localPlayer.GetState() != ePlayerObjectState.Dead)) return false;
		PacketStream stream;
		if (retryEnter)
		{
			// 기존 존에서 이미 이탈한 뒤 마을이 가득 찬 경우에는 입장만 재시도
			stream = new PacketStream(12);
			stream.Write(ePacketType.C2S_EnterZone);
			stream.Write(m_accountInfo.GetCharacterIndex());
		}
		else
		{
			stream = new PacketStream(4);
			stream.Write(ePacketType.C2S_Respawn);
		}
		if (!m_client.Write(stream)) return false;
		m_waitingRespawn = true;
		m_gameplayUI.SetLoading(true);
		return true;
	}

	void Update()
	{
		if (m_client.TryGetDisconnectMessage(out string message))
		{
			m_gameplayUI.ShowMessage("서버와의 연결이 끊겼습니다.\n확인을 누르면 게임을 종료합니다.", true);
			m_waitingZoneChange = true;
			return;
		}
		byte[] packet = m_client.ReadPacket();
		while (packet != null)
		{
			ReadPacket(packet);
			if (m_waitingZoneChange && m_accountInfo.IsZoneTransferPending()) return;
			packet = m_client.ReadPacket();
		}
		if (IsWaiting() || Time.time < m_nextMoveSend) return;
		m_nextMoveSend = Time.time + 0.2f;
		SendMove();
	}

	void ReadEnterPacket(byte[] _packet)
	{
		if (_packet == null || _packet.Length < 45 || _packet[4] != 0) return;
		ulong localObjectNum = BitConverter.ToUInt64(_packet, 13);
		m_localObjectNum = localObjectNum;
		m_channelId = BitConverter.ToUInt64(_packet, 5);
		if (m_channelUI != null) m_channelUI.SetChannel(m_channelId);
		ulong experience = BitConverter.ToUInt64(_packet, 21);
		ulong requiredExperience = BitConverter.ToUInt64(_packet, 29);
		ulong objectCount = BitConverter.ToUInt64(_packet, 37);
		int offset = 45;
		for (ulong i = 0; i < objectCount; ++i)
		{
			if (offset + FieldObjectSize > _packet.Length) break;
			bool local = _packet[offset] == 0 && BitConverter.ToUInt64(_packet, offset + 1) == localObjectNum;
			CreateObject(_packet, offset, local, experience, requiredExperience);
			offset += FieldObjectSize;
		}
		if (m_localPlayer != null)
		{
			// 채널/존마다 새 Player. 이전 채널의 전송 캐시도 같이 버림
			m_hasMoveDestination = false;
			m_nextMoveSend = 0;
			m_waitingZoneChange = false;
			m_waitingChannelChange = false;
			m_waitingRespawn = false;
			m_gameplayUI.Bind(m_localPlayer);
			PlayerFollowCamera camera = Camera.main.GetComponent<PlayerFollowCamera>();
			if (camera != null) camera.SetTarget(m_localPlayer.transform);
			if (m_localPlayer.GetState() == ePlayerObjectState.Dead) m_gameplayUI.ShowRespawn();
		}
	}

	void CreateObject(byte[] _packet, int _offset, bool _local, ulong _experience = 0, ulong _requiredExperience = 0)
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
			if (_local) prefab = jobClass == 0 ? m_localWarriorPrefab : m_localArcherPrefab;
			else prefab = jobClass == 0 ? m_remoteWarriorPrefab : m_remoteArcherPrefab;
		}
		else if (dataIndex < (ulong)m_monsterPrefabList.Length) prefab = m_monsterPrefabList[(int)dataIndex];
		if (prefab == null) return;
		string objectKey = Key(objectType, objectNum);
		bool created = !m_objectMap.TryGetValue(objectKey, out GameObject instance) || instance == null;
		if (created)
		{
			instance = Instantiate(prefab, ResolveHeight(position), Quaternion.Euler(0.0f, rotationY, 0.0f), m_objectRoot);
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
		if (_local) m_localPlayer = instance.GetComponent<LocalPlayerObject>();
		if (objectType == 0 && (hp == 0 || _packet[_offset + 65] == 5)) ApplyPlayerDead(objectNum, created);
	}

	void ReadPacket(byte[] _packet)
	{
		if (_packet.Length < 4) return;
		ePacketType type = (ePacketType)BitConverter.ToUInt16(_packet, 2);
		if (type == ePacketType.S2C_FieldObjectEnter && _packet.Length >= 4 + FieldObjectSize) CreateObject(_packet, 4, false);
		if (type == ePacketType.S2C_EnterZone)
		{
			if (_packet.Length < 45 || _packet[4] != 0)
			{
				if (m_accountInfo.IsRespawnPending())
				{
					m_waitingRespawn = false;
					m_waitingZoneChange = false;
					m_gameplayUI.ShowRespawn("마을에 입장하지 못했습니다.\n확인을 누르면 다시 시도합니다.");
					return;
				}
				m_gameplayUI.ShowMessage("지역에 입장하지 못했습니다.\n게임을 다시 실행해주세요.", true);
				return;
			}
			m_accountInfo.SetEnterZonePacket(_packet);
			m_accountInfo.CompleteZoneTransfer();
			ReadEnterPacket(_packet);
		}
		if (type == ePacketType.S2C_ChangeZone || type == ePacketType.S2C_Respawn)
		{
			if (_packet.Length < 25 || _packet[4] != 0)
			{
				m_waitingZoneChange = false;
				m_waitingRespawn = false;
				if (type == ePacketType.S2C_Respawn) m_gameplayUI.ShowRespawn("마을 부활을 처리하지 못했습니다.\n확인을 누르면 다시 시도합니다.");
				else m_gameplayUI.ShowMessage("해당 지역으로 이동할 수 없습니다.");
				return;
			}
			ulong zoneId = BitConverter.ToUInt64(_packet, 5);
			Vector3 position = new Vector3(BitConverter.ToSingle(_packet, 13), BitConverter.ToSingle(_packet, 17), BitConverter.ToSingle(_packet, 21));
			if (type == ePacketType.S2C_Respawn) m_accountInfo.BeginRespawnTransfer(zoneId, position);
			else m_accountInfo.BeginZoneTransfer(zoneId, position);
			m_waitingZoneChange = true;
			string sceneName = "";
			if (zoneId == 1) sceneName = "TownScene";
			if (zoneId == 2) sceneName = "BeginnerZoneScene";
			if (sceneName.Length == 0) { m_waitingZoneChange = false; return; }
			SceneManager.LoadScene(sceneName);
			return;
		}
		if (type == ePacketType.S2C_ChangeChannel)
		{
			m_waitingChannelChange = false;
			m_gameplayUI.SetLoading(false);
			if (_packet.Length < 45 || _packet[4] != 0) { m_channelUI.ShowResult(false); return; }
			ClearWorldObjects();
			ReadEnterPacket(_packet);
			m_channelUI.ShowResult(true);
		}
		if (type == ePacketType.S2C_ChannelList) m_channelUI.ReadList(_packet, m_channelId);
		if (type == ePacketType.S2C_FieldObjectLeave && _packet.Length >= 13)
		{
			RemoveObject(_packet[4], BitConverter.ToUInt64(_packet, 5));
		}
		if (type == ePacketType.S2C_PlayerDead && _packet.Length == 28)
		{
			ulong objectNum = BitConverter.ToUInt64(_packet, 4);
			UpdateTransform(0, objectNum, _packet, 12, BitConverter.ToSingle(_packet, 24), true);
			ApplyPlayerDead(objectNum);
		}
		if (type == ePacketType.S2C_Move && _packet.Length >= 40)
		{
			ulong objectNum = BitConverter.ToUInt64(_packet, 4);
			if (m_objectMap.TryGetValue(Key(0, objectNum), out GameObject instance))
			{
				RemotePlayerObject remote = instance.GetComponent<RemotePlayerObject>();
				Vector3 position = new Vector3(BitConverter.ToSingle(_packet, 12), 0.0f, BitConverter.ToSingle(_packet, 20));
				Vector3 destination = new Vector3(BitConverter.ToSingle(_packet, 24), 0.0f, BitConverter.ToSingle(_packet, 32));
				if (remote != null) remote.ApplyMove(ResolveHeight(position), destination, BitConverter.ToSingle(_packet, 36));
			}
		}
		if (type == ePacketType.S2C_AttackStart && _packet.Length >= 37)
		{
			ulong playerObjectNum = BitConverter.ToUInt64(_packet, 4);
			// 내가 이미 수행한 입력은 서버 통지로 위치/모션을 되감지 않음
			if (playerObjectNum == m_localObjectNum) return;
			if (m_objectMap.TryGetValue(Key(0, playerObjectNum), out GameObject playerGameObject))
			{
				RemotePlayerObject remote = playerGameObject.GetComponent<RemotePlayerObject>();
				if (remote != null)
				{
					remote.transform.rotation = Quaternion.Euler(0, BitConverter.ToSingle(_packet, 33), 0);
					m_objectMap.TryGetValue(Key(1, BitConverter.ToUInt64(_packet, 13)), out GameObject target);
					remote.BeginServerAttack(_packet[12], target);
				}
			}
		}
		if (type == ePacketType.S2C_AttackResult && _packet.Length >= 46 && _packet[4] == 0)
		{
			ulong damage = BitConverter.ToUInt64(_packet, 22);
			ulong targetNum = BitConverter.ToUInt64(_packet, 14);
			ulong playerObjectNum = BitConverter.ToUInt64(_packet, 5);
			if (m_objectMap.TryGetValue(Key(_packet[13], targetNum), out GameObject target))
			{
				MonsterCombatStatus status = target.GetComponent<MonsterCombatStatus>();
				if (status != null) status.ApplyServerHP((uint)BitConverter.ToUInt64(_packet, 30));
				// 내 공격은 이미 예측 연출했으므로 중복 재생하지 않음
				if (playerObjectNum != m_localObjectNum)
				{
					RemotePlayerObject remote = null;
					if (m_objectMap.TryGetValue(Key(0, playerObjectNum), out GameObject attacker)) remote = attacker.GetComponent<RemotePlayerObject>();
					int displayedDamage = (int)Math.Min(damage, (ulong)int.MaxValue);
					if (remote != null) remote.PlayServerHit(target, displayedDamage);
					else
					{
						// 공격자는 범위 밖이어도 보이는 몬스터의 피격은 표시
						CombatHitFeedback feedback = target.GetComponent<CombatHitFeedback>();
						if (feedback != null) feedback.Play(displayedDamage);
					}
				}
			}
			ulong gainedExperience = BitConverter.ToUInt64(_packet, 38);
			if (playerObjectNum == m_localObjectNum && m_localPlayer != null && gainedExperience > 0)
			{
				PlayerInfo playerInfo = m_localPlayer.GetComponent<PlayerInfo>();
				if (playerInfo != null) playerInfo.AddExperience(gainedExperience);
			}
		}
		if (type == ePacketType.S2C_LevelUp && _packet.Length >= 52)
		{
			ulong playerObjectNum = BitConverter.ToUInt64(_packet, 4);
			if (m_objectMap.TryGetValue(Key(0, playerObjectNum), out GameObject playerGameObject))
			{
				PlayerInfo playerInfo = playerGameObject.GetComponent<PlayerInfo>();
				if (playerInfo != null)
				{
					int level = (int)BitConverter.ToUInt64(_packet, 12);
					ulong experience = BitConverter.ToUInt64(_packet, 20);
					ulong requiredExperience = BitConverter.ToUInt64(_packet, 28);
					int maxHP = (int)BitConverter.ToUInt64(_packet, 36);
					int hp = (int)BitConverter.ToUInt64(_packet, 44);
					playerInfo.ApplyLevelUp(level, experience, requiredExperience, maxHP, hp);
					if (playerObjectNum == m_localObjectNum) m_accountInfo.SetProgress((ulong)level, experience);
				}
			}
		}
		if (type == ePacketType.S2C_MonsterState && _packet.Length >= 57)
		{
			ulong monsterObjectNum = BitConverter.ToUInt64(_packet, 4);
			if (m_objectMap.TryGetValue(Key(1, monsterObjectNum), out GameObject monsterObject))
			{
				MonsterCombatStatus status = monsterObject.GetComponent<MonsterCombatStatus>();
				if (status != null) status.ApplyServerHP((uint)BitConverter.ToUInt64(_packet, 49));
				MonsterObject monster = monsterObject.GetComponent<MonsterObject>();
				if (monster != null)
				{
					Vector3 position = new Vector3(BitConverter.ToSingle(_packet, 13), 0, BitConverter.ToSingle(_packet, 21));
					monster.ApplyServerState(position, BitConverter.ToSingle(_packet, 37), _packet[12]);
				}
			}
		}
		if (type == ePacketType.S2C_PlayerHit && _packet.Length == 36)
		{
			ulong playerNum = BitConverter.ToUInt64(_packet, 4);
			ulong monsterNum = BitConverter.ToUInt64(_packet, 12);
			if (!m_objectMap.TryGetValue(Key(0, playerNum), out GameObject player)) return;
			PlayerInfo info = player.GetComponent<PlayerInfo>();
			if (info != null) info.ApplyServerHP((int)BitConverter.ToUInt64(_packet, 28));
			CombatHitFeedback feedback = player.GetComponent<CombatHitFeedback>();
			float hitDelay = 0;
			if (m_objectMap.TryGetValue(Key(1, monsterNum), out GameObject source))
			{
				MonsterObject monster = source.GetComponent<MonsterObject>();
				if (monster != null) hitDelay = monster.PlayServerAttack(player.transform);
			}
			// 몬스터가 가시 범위 밖이라 모션을 볼 수 없다면 즉시 피격 표시
			if (feedback != null) feedback.PlayDelayed((int)BitConverter.ToUInt64(_packet, 20), hitDelay);
			if (info != null && info.GetHP() == 0) ApplyPlayerDead(playerNum);
		}
		if (type == ePacketType.S2C_UsePotion && _packet.Length >= 37 && _packet[4] == 0 && m_localPlayer != null)
		{
			if (m_objectMap.TryGetValue(Key(0, BitConverter.ToUInt64(_packet, 5)), out GameObject player))
			{
				PlayerInfo info = player.GetComponent<PlayerInfo>();
				if (info != null) info.ApplyServerHP((int)BitConverter.ToUInt64(_packet, 21));
			}
		}
		if (type == ePacketType.S2C_MonsterMove && _packet.Length >= 48)
		{
			ulong monsterObjectNum = BitConverter.ToUInt64(_packet, 4);
			Vector3 position = new Vector3(BitConverter.ToSingle(_packet, 12), 0.0f, BitConverter.ToSingle(_packet, 20));
			float rotationY = BitConverter.ToSingle(_packet, 24);
			float moveSpeed = BitConverter.ToSingle(_packet, 28);
			ulong pathIndex = BitConverter.ToUInt64(_packet, 32);
			ulong pathCount = BitConverter.ToUInt64(_packet, 40);
			if (pathCount > (ulong)((_packet.Length - 48) / 12)) return;
			List<Vector3> path = new List<Vector3>((int)pathCount);
			for (ulong i = 0; i < pathCount; ++i)
			{
				int offset = 48 + (int)i * 12;
				path.Add(new Vector3(BitConverter.ToSingle(_packet, offset), 0.0f, BitConverter.ToSingle(_packet, offset + 8)));
			}
			if (m_objectMap.TryGetValue(Key(1, monsterObjectNum), out GameObject monsterGameObject))
			{
				MonsterObject monster = monsterGameObject.GetComponent<MonsterObject>();
				if (monster != null) monster.SetServerPath(position, rotationY, moveSpeed, (int)pathIndex, path);
			}
		}
		if (type == ePacketType.S2C_Chat && _packet.Length >= 7)
		{
			int nameSize = _packet[4];
			int chatSize = _packet[5];
			if (6 + nameSize + chatSize > _packet.Length) return;
			string name = Encoding.UTF8.GetString(_packet, 6, nameSize);
			string chat = Encoding.UTF8.GetString(_packet, 6 + nameSize, chatSize);
			if (m_chatUI != null) m_chatUI.AddChat(name, chat);
			ShowWorldChat(name, chat);
		}
	}

	void ShowWorldChat(string _name, string _chat)
	{
		foreach (KeyValuePair<string, GameObject> pair in m_objectMap)
		{
			PlayerInfo info = pair.Value.GetComponent<PlayerInfo>();
			if (info == null || info.GetName() != _name) continue;
			PlayerWorldUI worldUI = pair.Value.GetComponentInChildren<PlayerWorldUI>(true);
			if (worldUI != null) worldUI.ShowChat(_chat);
			return;
		}
	}

	void ApplyPlayerDead(ulong _objectNum, bool _finished = false)
	{
		if (!m_objectMap.TryGetValue(Key(0, _objectNum), out GameObject instance)) return;
		PlayerObject player = instance.GetComponent<PlayerObject>();
		if (player == null) return;
		PlayerInfo info = instance.GetComponent<PlayerInfo>();
		if (info != null) info.ApplyServerHP(0);
		bool alreadyDead = player.GetState() == ePlayerObjectState.Dead;
		player.ApplyDeath(_finished);
		if (!alreadyDead && player == m_localPlayer && m_gameplayUI != null)
		{
			CombatHitFeedback feedback = player.GetComponent<CombatHitFeedback>();
			float delay = 0;
			if (feedback != null && !_finished) delay = feedback.GetPendingHitDelay();
			StartCoroutine(ShowRespawnAfterHit(player, delay));
		}
	}

	IEnumerator ShowRespawnAfterHit(PlayerObject _player, float _delay)
	{
		if (_delay > 0) yield return new WaitForSeconds(_delay);
		if (_player != null && _player.gameObject.activeInHierarchy && _player == m_localPlayer && _player.GetState() == ePlayerObjectState.Dead)
			m_gameplayUI.ShowRespawn();
	}

	void RemoveObject(byte _type, ulong _num)
	{
		string key = Key(_type, _num);
		if (!m_objectMap.TryGetValue(key, out GameObject instance)) return;
		if (instance != null && m_localPlayer != null && instance == m_localPlayer.gameObject) return;
		m_objectMap.Remove(key);
		if (instance == null) return;
		if (m_localPlayer != null)
		{
			PlayerCombat combat = m_localPlayer.GetComponent<PlayerCombat>();
			if (combat != null && combat.GetTarget() != null && combat.GetTarget().transform.IsChildOf(instance.transform)) combat.ClearTarget();
		}
		// Destroy는 프레임 끝에 처리됨. 그 전에 화면/클릭 판정에서 제외
		instance.SetActive(false);
		Destroy(instance);
	}

	void ClearWorldObjects()
	{
		foreach (KeyValuePair<string, GameObject> pair in m_objectMap)
		{
			if (pair.Value == null) continue;
			pair.Value.SetActive(false);
			Destroy(pair.Value);
		}
		m_objectMap.Clear();
		m_localPlayer = null;
		m_hasMoveDestination = false;
		m_lastDestination = Vector3.zero;
		m_nextMoveSend = 0;
	}

	void UpdateTransform(byte _type, ulong _num, byte[] _packet, int _positionOffset, float _rotationY, bool _allowDead = false)
	{
		if (!m_objectMap.TryGetValue(Key(_type, _num), out GameObject gameObject)) return;
		PlayerObject player = gameObject.GetComponent<PlayerObject>();
		if (!_allowDead && player != null && player.GetState() == ePlayerObjectState.Dead) return;
		Vector3 position = new Vector3(BitConverter.ToSingle(_packet, _positionOffset), BitConverter.ToSingle(_packet, _positionOffset + 4), BitConverter.ToSingle(_packet, _positionOffset + 8));
		gameObject.transform.position = ResolveHeight(position);
		gameObject.transform.rotation = Quaternion.Euler(0.0f, _rotationY, 0.0f);
	}

	void SendMove()
	{
		List<Vector3> path = m_localPlayer.GetPath();
		Vector3 destination = m_localPlayer.transform.position;
		if (path.Count > 0) destination = path[path.Count - 1];
		bool destinationChanged = !m_hasMoveDestination || destination != m_lastDestination;
		PacketStream stream = new PacketStream(33);
		stream.Write(ePacketType.C2S_Move);
		WriteVector(stream, m_localPlayer.transform.position);
		WriteVector(stream, destination);
		stream.Write(m_localPlayer.transform.eulerAngles.y);
		stream.Write((byte)(destinationChanged ? 1 : 0));
		if (m_client.Write(stream))
		{
			m_lastDestination = destination;
			m_hasMoveDestination = true;
		}
	}

	public void NotifyMoveChanged()
	{
		if (m_client == null || IsWaiting()) return;
		List<Vector3> path = m_localPlayer.GetPath();
		Vector3 destination = m_localPlayer.transform.position;
		if (path.Count > 0) destination = path[path.Count - 1];
		if (m_hasMoveDestination && destination == m_lastDestination) return;
		SendMove();
		m_nextMoveSend = Time.time + 0.2f;
	}

	Vector3 ResolveHeight(Vector3 _position)
	{
		Terrain terrain = Terrain.activeTerrain;
		if (terrain != null) _position.y = terrain.SampleHeight(_position) + terrain.transform.position.y;
		return _position;
	}

	void WriteVector(PacketStream _stream, Vector3 _position)
	{
		_stream.Write(_position.x); _stream.Write(0.0f); _stream.Write(_position.z);
	}

	string Key(byte _type, ulong _num) { return _type.ToString() + ":" + _num.ToString(); }
}
