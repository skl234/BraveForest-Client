using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PlayerDeathVerification
{
	const string Key = "PlayerDeathVerification.Running";
	const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
	static int m_phase;
	static float m_started;
	static bool m_failed;
	static PlayerObject[] m_players;
	static ZoneSceneNetwork m_network;
	static ZoneGameplayUI m_ui;
	static AccountInfo m_account;
	static NetClient m_client;
	static TcpListener m_listener;
	static TcpClient m_peer;
	static NoticePanelUI m_notice;
	static float m_wait;
	static float[] m_headHeight;

	static PlayerDeathVerification() { if (SessionState.GetBool(Key, false)) Register(); }
	public static void Run()
	{
		SessionState.SetBool(Key, true);
		EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
		Register();
		EditorApplication.EnterPlaymode();
	}
	static void Register()
	{
		EditorApplication.update -= Tick; EditorApplication.update += Tick;
		EditorApplication.playModeStateChanged -= Changed; EditorApplication.playModeStateChanged += Changed;
		Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
	}
	static void Log(string _text, string _stack, LogType _type) { if (_type == LogType.Error || _type == LogType.Exception) m_failed = true; }
	static void Changed(PlayModeStateChange _state)
	{
		if (_state == PlayModeStateChange.EnteredPlayMode) { m_phase = 0; m_started = Time.time; }
		if (_state != PlayModeStateChange.EnteredEditMode) return;
		SessionState.SetBool(Key, false);
		int result = 0; if (m_failed) result = 1;
		EditorApplication.Exit(result);
	}
	static void Set(object _target, string _name, object _value) { _target.GetType().GetField(_name, Flags).SetValue(_target, _value); }
	static void Read(byte[] _packet) { typeof(ZoneSceneNetwork).GetMethod("ReadPacket", Flags).Invoke(m_network, new object[] { _packet }); }
	static void Write(byte[] _packet, int _offset, byte[] _value) { Buffer.BlockCopy(_value, 0, _packet, _offset, _value.Length); }
	static byte[] Packet(ePacketType _type, int _size)
	{
		byte[] packet = new byte[_size]; Write(packet,0,BitConverter.GetBytes((ushort)_size)); Write(packet,2,BitConverter.GetBytes((ushort)_type)); return packet;
	}
	static byte[] Death(ulong _num)
	{
		byte[] packet = Packet(ePacketType.S2C_PlayerDead,28); Write(packet,4,BitConverter.GetBytes(_num)); return packet;
	}
	static void Check(bool _condition, string _message) { if (!_condition) throw new Exception(_message); Debug.Log("[DeathClientTest] PASS " + _message); }
	static void CheckRequest(ePacketType _type, int _size)
	{
		byte[] bytes = new byte[_size]; int read = 0;
		while (read < bytes.Length) { int size = m_peer.GetStream().Read(bytes,read,bytes.Length-read); if (size == 0) throw new Exception("EOF"); read += size; }
		Check(BitConverter.ToUInt16(bytes,0)==_size && BitConverter.ToUInt16(bytes,2)==(ushort)_type, "actual NetClient sends " + _type + " size=" + _size);
	}
	static void Tick()
	{
		if (!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
		try
		{
			if (Time.time - m_started > 30) throw new Exception("timeout");
			if (m_phase == 0)
			{
				m_network = new GameObject("DeathPacketReceiver").AddComponent<ZoneSceneNetwork>(); m_network.enabled = false;
				typeof(ZoneSceneNetwork).GetProperty("Instance").SetValue(null,m_network);
				m_account = new GameObject("TestAccount").AddComponent<AccountInfo>(); m_account.SetCharacter(10,0,6,321,2,Vector3.zero);
				m_client = new GameObject("TestNetClient").AddComponent<NetClient>();
				m_listener = new TcpListener(IPAddress.Loopback,0); m_listener.Start();
				Set(m_client,"m_ip","127.0.0.1"); Set(m_client,"m_port",((IPEndPoint)m_listener.LocalEndpoint).Port);
				Check(m_client.Connect(), "isolated loopback connection"); m_peer = m_listener.AcceptTcpClient(); m_peer.ReceiveTimeout=2000;
				GameObject uiObject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MYFolder/Prefab/UI/ZoneInterface.prefab"));
				m_ui=uiObject.GetComponent<ZoneGameplayUI>(); if(m_ui==null) m_ui=uiObject.GetComponentInChildren<ZoneGameplayUI>(true);
				m_ui.SetLoading(false); m_notice=(NoticePanelUI)typeof(ZoneGameplayUI).GetField("m_noticePanel",Flags).GetValue(m_ui);
				Set(m_network,"m_client",m_client); Set(m_network,"m_accountInfo",m_account); Set(m_network,"m_gameplayUI",m_ui);
				Set(m_network,"m_channelUI",uiObject.GetComponentInChildren<ZoneChannelUI>(true));
				string playerRoot="Assets/MYFolder/Prefab/Object/Player/";
				Set(m_network,"m_localWarriorPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(playerRoot+"LocalPlayerObject_Warrior.prefab"));
				Set(m_network,"m_localArcherPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(playerRoot+"LocalPlayerObject_Archer.prefab"));
				Set(m_network,"m_remoteWarriorPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(playerRoot+"RemotePlayerObject_Warrior.prefab"));
				Set(m_network,"m_remoteArcherPrefab",AssetDatabase.LoadAssetAtPath<GameObject>(playerRoot+"RemotePlayerObject_Archer.prefab"));
				Dictionary<string,GameObject> objects=(Dictionary<string,GameObject>)typeof(ZoneSceneNetwork).GetField("m_objectMap",Flags).GetValue(m_network);
				string[] names={"LocalPlayerObject_Warrior","LocalPlayerObject_Archer","RemotePlayerObject_Warrior","RemotePlayerObject_Archer"};
				m_players=new PlayerObject[4]; m_headHeight=new float[4]; m_wait=1;
				for(int i=0;i<4;++i)
				{
					GameObject actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MYFolder/Prefab/Object/Player/"+names[i]+".prefab"));
					m_players[i]=actor.GetComponent<PlayerObject>(); objects["0:"+(100+i)]=actor;
					PlayerInfo info=actor.GetComponent<PlayerInfo>(); if(info==null) info=actor.AddComponent<PlayerInfo>();
					info.Initialize(names[i],6,200,200,321,1000);
					m_players[i].GetAnimator().cullingMode=AnimatorCullingMode.AlwaysAnimate;
					m_players[i].GetAnimator().Update(0);
					m_headHeight[i]=m_players[i].GetAnimator().GetBoneTransform(HumanBodyBones.Head).position.y;
					AnimationClip[] clips=m_players[i].GetAnimator().runtimeAnimatorController.animationClips;
					bool found=false;
					for(int j=0;j<clips.Length;++j) if(clips[j].name.StartsWith("Die_")) { found=true; Check(!clips[j].isLooping,"non-looping death clip: "+names[i]); m_wait=Mathf.Max(m_wait,clips[j].length+.5f); }
					Check(found,"death animation assigned: "+names[i]);
				}
				Set(m_network,"m_localPlayer",m_players[0]); Set(m_network,"m_localObjectNum",100UL);
				Check(!m_network.RequestRespawn(),"living local player cannot request respawn");
				for(int i=0;i<4;++i) Read(Death((ulong)(100+i)));
				Check(m_notice.gameObject.activeSelf && ((TMP_Text)typeof(NoticePanelUI).GetField("m_messageText",Flags).GetValue(m_notice)).text=="마을에서 부활합니다.","shared notice displays respawn message");
				Check(m_network.IsWaiting() && !((LocalPlayerObject)m_players[0]).MoveTo(Vector3.one) && !m_players[0].GetComponent<PlayerCombat>().TryArea(),"dead local gameplay blocked");
				m_phase=1; m_started=Time.time;
			}
			else if(m_phase==1 && Time.time-m_started>m_wait)
			{
				for(int i=0;i<4;++i)
				{
					Animator animator=m_players[i].GetAnimator();
					Check(m_players[i].GetState()==ePlayerObjectState.Dead && animator.speed==0 && animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=1,"death finishes and freezes: "+i);
					Check(animator.GetBoneTransform(HumanBodyBones.Head).position.y<m_headHeight[i]-.3f,"character actually falls down: "+i);
					float before=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
					for(int j=0;j<100;++j) Read(Death((ulong)(100+i)));
					m_players[i].SetState(ePlayerObjectState.Idle); m_players[i].PlayAttack(ePlayerObjectState.Attack,"Attack01_Bow");
					Check(m_players[i].GetState()==ePlayerObjectState.Dead && animator.GetCurrentAnimatorStateInfo(0).normalizedTime==before,"duplicate death and late attack do not restart pose: "+i);
				}
				Check(m_peer.Available==0,"no gameplay packets sent while dead");
				Button confirm=(Button)typeof(NoticePanelUI).GetField("m_button",Flags).GetValue(m_notice);
				confirm.onClick.Invoke(); CheckRequest(ePacketType.C2S_Respawn,4);
				Check(!m_network.RequestRespawn(),"double confirmation blocked while waiting");
				byte[] fail=Packet(ePacketType.S2C_Respawn,25); fail[4]=1; Read(fail);
				Check(m_notice.gameObject.activeSelf && m_players[0].GetState()==ePlayerObjectState.Dead,"failure keeps corpse and offers retry");
				m_account.BeginRespawnTransfer(1,new Vector3(50,0,20));
				byte[] full=Packet(ePacketType.S2C_EnterZone,45); full[4]=2; Read(full);
				confirm.onClick.Invoke(); CheckRequest(ePacketType.C2S_EnterZone,12);
				Check(m_account.GetZoneId()==1 && m_account.GetPosition()==new Vector3(50,0,20) && m_account.GetExperience()==321,"respawn transfer preserves town position and experience");
				GameObject camera=new GameObject("TestCamera",typeof(Camera)); camera.tag="MainCamera";
				typeof(ZoneSceneNetwork).GetMethod("ClearWorldObjects",Flags).Invoke(m_network,null);
				Read(File.ReadAllBytes("Build/Validation/RespawnEnterPacket.bin"));
				LocalPlayerObject respawned=m_network.GetLocalPlayer();
				PlayerInfo respawnInfo=respawned.GetComponent<PlayerInfo>();
				Check(!m_account.IsRespawnPending() && !m_network.IsWaiting(),"actual C++ enter packet clears wait and respawn state");
				Check(respawned.GetState()==ePlayerObjectState.Idle && respawnInfo.GetHP()==respawnInfo.GetMaxHP() && respawnInfo.GetLevel()==6 && respawnInfo.GetExperience()==321,"actual C++ packet recreates alive local player with full HP and progression");
				Check(respawned.transform.position.x==50 && respawned.transform.position.z==20,"actual C++ packet places client at town creation position");
				Check(!m_failed,"no Unity runtime errors");
				Debug.Log("[DeathClientTest] COMPLETE (real prefabs + loopback NetClient; no production servers or saved scene changes)");
				Finish();
			}
		}
		catch(Exception error) { m_failed=true; Debug.LogException(error); Finish(); }
	}
	static void Finish()
	{
		m_phase=2;
		if(m_client!=null) m_client.Disconnect();
		if(m_peer!=null) m_peer.Close();
		if(m_listener!=null) m_listener.Stop();
		EditorApplication.ExitPlaymode();
	}
}
