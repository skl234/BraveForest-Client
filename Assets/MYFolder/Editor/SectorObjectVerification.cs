using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class SectorObjectVerification
{
	const string Key = "SectorObjectVerification.Running";
	const string Root = "Assets/MYFolder/Prefab/Object/";
	static ZoneSceneNetwork m_network;
	static Dictionary<string, GameObject> m_objects;
	static GameObject m_monster;
	static GameObject m_remote;
	static GameObject m_local;
	static GameObject m_removed;
	static int m_phase;
	static float m_started;
	static bool m_failed;
	static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

	static SectorObjectVerification() { if (SessionState.GetBool(Key, false)) Register(); }
	public static void Run()
	{
		SessionState.SetBool(Key, true);
		EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
		Register();
		EditorApplication.EnterPlaymode();
	}
	static void Register()
	{
		EditorApplication.update -= Tick;
		EditorApplication.update += Tick;
		EditorApplication.playModeStateChanged -= Changed;
		EditorApplication.playModeStateChanged += Changed;
		Application.logMessageReceived -= Log;
		Application.logMessageReceived += Log;
	}
	static void Changed(PlayModeStateChange _state)
	{
		if (_state == PlayModeStateChange.EnteredPlayMode) { m_phase = 0; m_started = Time.time; }
		if (_state == PlayModeStateChange.EnteredEditMode)
		{
			SessionState.SetBool(Key, false);
			int result = 0;
			if (m_failed) result = 1;
			EditorApplication.Exit(result);
		}
	}
	static void Log(string _message, string _stack, LogType _type)
	{
		if (_type == LogType.Error || _type == LogType.Exception) m_failed = true;
	}
	static void Set(string _name, object _value) { typeof(ZoneSceneNetwork).GetField(_name, Flags).SetValue(m_network, _value); }
	static void Read(byte[] _packet) { typeof(ZoneSceneNetwork).GetMethod("ReadPacket", Flags).Invoke(m_network, new object[] { _packet }); }
	static void Write(byte[] _packet, int _offset, byte[] _value) { Buffer.BlockCopy(_value, 0, _packet, _offset, _value.Length); }
	static byte[] Enter(byte _type, ulong _num, ulong _hp = 90, byte _state = 0)
	{
		byte[] packet = new byte[101];
		Write(packet, 0, BitConverter.GetBytes((ushort)packet.Length));
		Write(packet, 2, BitConverter.GetBytes((ushort)ePacketType.S2C_FieldObjectEnter));
		packet[4] = _type;
		Write(packet, 5, BitConverter.GetBytes(_num));
		ulong dataIndex = 3;
		if (_type == 0) dataIndex = _num + 100;
		Write(packet, 13, BitConverter.GetBytes(dataIndex));
		Write(packet, 29, BitConverter.GetBytes(1UL));
		Write(packet, 37, BitConverter.GetBytes(100UL));
		Write(packet, 45, BitConverter.GetBytes(_hp));
		Write(packet, 53, BitConverter.GetBytes(2.0f));
		Write(packet, 61, BitConverter.GetBytes(2.0f));
		packet[69] = _state;
		packet[70] = 4;
		Write(packet, 71, Encoding.UTF8.GetBytes("Test"));
		return packet;
	}
	static byte[] Leave(byte _type, ulong _num)
	{
		byte[] packet = new byte[13];
		Write(packet, 0, BitConverter.GetBytes((ushort)13));
		Write(packet, 2, BitConverter.GetBytes((ushort)ePacketType.S2C_FieldObjectLeave));
		packet[4] = _type;
		Write(packet, 5, BitConverter.GetBytes(_num));
		return packet;
	}
	static GameObject Prefab(string _name) { return AssetDatabase.LoadAssetAtPath<GameObject>(Root + _name + ".prefab"); }
	static void Check(bool _condition, string _message)
	{
		if (!_condition) throw new Exception(_message);
		Debug.Log("[SectorClientTest] PASS " + _message);
	}
	static void Tick()
	{
		if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
		try
		{
			if (Time.time - m_started > 20) throw new Exception("test timeout");
			if (m_phase == 0)
			{
				m_network = new GameObject("PacketReceiver").AddComponent<ZoneSceneNetwork>();
				m_network.enabled = false;
				Set("m_objectRoot", new GameObject("ReceivedObjects").transform);
				Set("m_localWarriorPrefab", Prefab("Player/LocalPlayerObject_Warrior"));
				Set("m_remoteWarriorPrefab", Prefab("Player/RemotePlayerObject_Warrior"));
				Set("m_monsterPrefabList", new GameObject[] { Prefab("Monster/MonsterObject_Ghost"), Prefab("Monster/MonsterObject_Lizard"), Prefab("Monster/MonsterObject_Knight"), Prefab("Monster/MonsterObject_Rat") });
				m_objects = (Dictionary<string, GameObject>)typeof(ZoneSceneNetwork).GetField("m_objectMap", Flags).GetValue(m_network);
				typeof(ZoneSceneNetwork).GetMethod("CreateObject", Flags).Invoke(m_network, new object[] { Enter(0, 7), 4, true, 42UL, 100UL });
				m_local = m_objects["0:7"];
				m_local.GetComponent<LocalPlayerObject>().enabled = false;
				Read(Enter(0, 2));
				Read(Enter(1, 2));
				m_remote = m_objects["0:2"];
				m_monster = m_objects["1:2"];
				m_remote.GetComponent<RemotePlayerObject>().ApplyMove(new Vector3(2, 0, 2), new Vector3(20, 0, 2), 90);
				m_monster.GetComponent<MonsterObject>().SetServerPath(new Vector3(2, 0, 2), 90, 1, 0, new List<Vector3> { new Vector3(20, 0, 2) });
				PlayerCombat combat = m_local.GetComponent<PlayerCombat>();
				typeof(PlayerCombat).GetMethod("SelectTarget", Flags).Invoke(combat, new object[] { m_monster.GetComponentInChildren<MonsterHitbox>() });
				m_phase = 1;
				m_started = Time.time;
			}
			else if (m_phase == 1 && Time.time - m_started > .5f)
			{
				Vector3 monsterPosition = m_monster.transform.position;
				Vector3 remotePosition = m_remote.transform.position;
				Animator animator = m_monster.GetComponent<MonsterObject>().GetAnimator();
				float animationTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
				Check(monsterPosition.x > 2.05f && remotePosition.x > 2.05f, "real prefabs are moving before refresh");
				for (int i = 0; i < 100; ++i) { Read(Enter(0, 2, 70)); Read(Enter(1, 2, 70)); }
				Read(Enter(0, 7, 70));
				Check(m_objects.Count == 3 && m_objects["1:2"] == m_monster && m_objects["0:2"] == m_remote, "100 duplicate enters retain exact instances and type-separated IDs");
				Check(m_monster.transform.position == monsterPosition && m_remote.transform.position == remotePosition, "duplicate snapshot does not rewind position");
				Check(animator == m_monster.GetComponent<MonsterObject>().GetAnimator() && animator.GetCurrentAnimatorStateInfo(0).normalizedTime == animationTime, "animator instance and playback time retained");
				Check(m_monster.GetComponent<MonsterObject>().GetState() == eMonsterState.Move && m_remote.GetComponent<PlayerObject>().GetState() == ePlayerObjectState.Move, "moving states retained");
				Check(m_local.GetComponent<PlayerInfo>().GetExperience() == 42 && m_monster.GetComponent<MonsterCombatStatus>().GetHP() == 70, "stats refreshed without clearing local experience");
				Check(m_local.GetComponent<PlayerCombat>().GetTarget() == m_monster.GetComponentInChildren<MonsterHitbox>(), "target reference retained");
				Read(Leave(1, 2));
				Check(!m_objects.ContainsKey("1:2") && !m_monster.activeSelf, "leave removes only its target immediately");
				Check(m_local.GetComponent<PlayerCombat>().GetTarget() == null, "leaving attack target cleared");
				Read(Leave(1, 2));
				Read(Leave(0, 7));
				Check(m_objects["0:2"] == m_remote && m_objects["0:7"] == m_local, "unrelated and local objects kept");
				m_removed = m_monster;
				Read(Enter(1, 2));
				m_monster = m_objects["1:2"];
				Check(m_monster != m_removed && m_monster.activeSelf && !m_removed.activeSelf, "same-frame leave/reenter has only one active instance");
				Read(Enter(1, 4, 0, 5));
				Check(m_objects["1:4"].GetComponent<MonsterObject>().GetState() == eMonsterState.Dead, "new dead monster uses received state");
				m_phase = 2;
				m_started = Time.time;
			}
			else if (m_phase == 2 && Time.time - m_started > .1f)
			{
				Check(m_removed == null, "departed object destroyed after frame");
				Check(!m_failed, "no runtime errors");
				Debug.Log("[SectorClientTest] COMPLETE (isolated Play Mode; no servers or saved scene changes)");
				m_phase = 3;
				EditorApplication.ExitPlaymode();
			}
		}
		catch (Exception error)
		{
			m_failed = true;
			Debug.LogException(error);
			m_phase = 3;
			EditorApplication.ExitPlaymode();
		}
	}
}
