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
using UnityEngine.AI;

[InitializeOnLoad]
public static class CombatSyncClientVerification
{
    const string Key = "CombatSyncClientVerification.Running";
    const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    static int m_phase;
    static float m_started;
    static bool m_failed;
    static ZoneSceneNetwork m_network;
    static LocalPlayerObject m_archer;
    static RemotePlayerObject m_remote;
    static MonsterObject[] m_monsters;
    static NetClient m_client;
    static TcpListener m_listener;
    static TcpClient m_peer;
    static Vector3 m_previous;
    static float m_previousTime;
    static int m_frames;
    static int m_expectedHP;
    static int m_checked;
    static int m_remoteHitCount;
    static NavMeshDataInstance m_nav;
    static Camera m_camera;
    static System.Collections.IEnumerator m_remoteTest;

    static CombatSyncClientVerification() { if(SessionState.GetBool(Key,false)) Register(); }
    public static void Run()
    {
        SessionState.SetBool(Key,true); EditorSceneManager.NewScene(NewSceneSetup.EmptyScene); Register(); EditorApplication.EnterPlaymode();
    }
    static void Register()
    {
        EditorApplication.update-=Tick; EditorApplication.update+=Tick;
        EditorApplication.playModeStateChanged-=Changed; EditorApplication.playModeStateChanged+=Changed;
        Application.logMessageReceived-=Log; Application.logMessageReceived+=Log;
    }
    static void Log(string t,string s,LogType type) { if(type==LogType.Error || type==LogType.Exception) m_failed=true; }
    static void Changed(PlayModeStateChange state)
    {
        if(state==PlayModeStateChange.EnteredPlayMode) { m_phase=0; m_started=Time.time; }
        if(state!=PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetBool(Key,false); EditorApplication.Exit(m_failed?1:0);
    }
    static void Set(object obj,string name,object value) { obj.GetType().GetField(name,Flags).SetValue(obj,value); }
    static void Check(bool value,string message) { if(!value) throw new Exception(message); ++m_checked; Debug.Log("[CombatClientTest] PASS "+message); }
    static void Read(byte[] bytes) { typeof(ZoneSceneNetwork).GetMethod("ReadPacket",Flags).Invoke(m_network,new object[]{bytes}); }
    static byte[] Packet(ePacketType type,int size) { byte[] b=new byte[size]; Put(b,0,BitConverter.GetBytes((ushort)size)); Put(b,2,BitConverter.GetBytes((ushort)type)); return b; }
    static void Put(byte[] b,int offset,byte[] value) { Buffer.BlockCopy(value,0,b,offset,value.Length); }
    static void Replay(string path)
    {
        byte[] bytes=File.ReadAllBytes(path); int offset=0;
        while(offset<bytes.Length)
        {
            int size=BitConverter.ToUInt16(bytes,offset); byte[] packet=new byte[size]; Buffer.BlockCopy(bytes,offset,packet,0,size); Read(packet); offset+=size;
        }
    }
    static void Capture(string name)
    {
        RenderTexture rt=new RenderTexture(1920,1080,24); m_camera.targetTexture=rt; m_camera.Render();
        RenderTexture.active=rt; Texture2D image=new Texture2D(1920,1080,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,1920,1080),0,0); image.Apply();
        File.WriteAllBytes("Build/Validation/"+name+".png",image.EncodeToPNG()); RenderTexture.active=null; m_camera.targetTexture=null; UnityEngine.Object.Destroy(image); rt.Release(); UnityEngine.Object.Destroy(rt);
    }
    static GameObject Prefab(string path,Vector3 position)
    {
        return UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MYFolder/Prefab/Object/"+path+".prefab"),position,Quaternion.identity);
    }
    static void Tick()
    {
        if(!EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        try
        {
            if(Time.time-m_started>30) throw new Exception("timeout");
            if(m_phase==0)
            {
                GameObject ground=GameObject.CreatePrimitive(PrimitiveType.Plane); ground.transform.position=new Vector3(20,0,20); ground.transform.localScale=Vector3.one*8; ground.layer=3;
                Material material=new Material(Shader.Find("Standard")); material.color=new Color(.24f,.38f,.23f); ground.GetComponent<Renderer>().material=material;
                NavMeshBuildSource source=new NavMeshBuildSource {shape=NavMeshBuildSourceShape.Box, size=new Vector3(80,.1f,80), transform=Matrix4x4.TRS(new Vector3(20,-.05f,20),Quaternion.identity,Vector3.one),area=0};
                NavMeshData data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),new List<NavMeshBuildSource>{source},new Bounds(new Vector3(20,0,20),new Vector3(80,5,80)),Vector3.zero,Quaternion.identity); m_nav=NavMesh.AddNavMeshData(data);
                m_camera=new GameObject("Main Camera").AddComponent<Camera>(); m_camera.tag="MainCamera"; m_camera.transform.position=new Vector3(14,12,-6); m_camera.transform.rotation=Quaternion.Euler(45,0,0); m_camera.fieldOfView=45;
                Light sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.transform.rotation=Quaternion.Euler(48,-32,0); sun.intensity=1.2f;
                m_network=new GameObject("PacketReceiver").AddComponent<ZoneSceneNetwork>(); m_network.enabled=false; typeof(ZoneSceneNetwork).GetProperty("Instance").SetValue(null,m_network);
                m_client=new GameObject("NetClient").AddComponent<NetClient>(); m_listener=new TcpListener(IPAddress.Loopback,0); m_listener.Start(); Set(m_client,"m_ip","127.0.0.1"); Set(m_client,"m_port",((IPEndPoint)m_listener.LocalEndpoint).Port);
                Check(m_client.Connect(),"real NetClient connected to isolated loopback"); m_peer=m_listener.AcceptTcpClient(); m_peer.ReceiveTimeout=2000; Set(m_network,"m_client",m_client);
                m_archer=Prefab("Player/LocalPlayerObject_Archer",new Vector3(14,0,5)).GetComponent<LocalPlayerObject>(); m_archer.enabled=false;
                m_archer.GetComponent<WorldPointerInput>().enabled=false;
                m_archer.GetComponent<PlayerInfo>().Initialize("궁수 검증",6,200,200);
                m_remote=Prefab("Player/RemotePlayerObject_Warrior",new Vector3(10,0,8)).GetComponent<RemotePlayerObject>();
                PlayerInfo remoteInfo=m_remote.GetComponent<PlayerInfo>(); if(remoteInfo==null) remoteInfo=m_remote.gameObject.AddComponent<PlayerInfo>(); remoteInfo.Initialize("전사 검증",6,300,300);
                Set(m_network,"m_localPlayer",m_archer); Set(m_network,"m_localObjectNum",0UL);
                Dictionary<string,GameObject> map=(Dictionary<string,GameObject>)typeof(ZoneSceneNetwork).GetField("m_objectMap",Flags).GetValue(m_network); map["0:0"]=m_archer.gameObject; map["0:1"]=m_remote.gameObject;
                string[] names={"Rat","Lizard","Ghost","Knight"}; m_monsters=new MonsterObject[4];
                for(int i=0;i<4;++i)
                {
                    m_monsters[i]=Prefab("Monster/MonsterObject_"+names[i],new Vector3(12+i*2,0,8)).GetComponent<MonsterObject>(); m_monsters[i].InitializeServer();
                    m_monsters[i].GetComponent<MonsterCombatStatus>().InitializeServerData(m_monsters[i].GetMonsterType(),1,10000,10000);
                    ZoneObjectIdentity id=m_monsters[i].gameObject.AddComponent<ZoneObjectIdentity>(); id.Initialize(1,(ulong)i); map["1:"+i]=m_monsters[i].gameObject;
                    Check(m_monsters[i].GetComponent<CombatHitFeedback>()!=null && !m_monsters[i].GetComponent<NavMeshAgent>().enabled,"shared feedback and server-only movement: "+names[i]);
                }
                string[] formulas=File.ReadAllLines("Build/Validation/CombatFormula.txt");
                for(int i=0;i<formulas.Length;++i) { string[] f=formulas[i].Split(' '); int result=CombatDamageCalculator.Calculate((ePlayerClass)int.Parse(f[0]),int.Parse(f[1]),(eCombatAttackType)(int.Parse(f[2])-1)); if(result!=int.Parse(f[3])) throw new Exception("damage formula differs: "+formulas[i]); }
                Check(formulas.Length==160,"C++ and Unity formulas identical for both classes, levels 1-20, four attacks");
                EntryMovementClientVerification.RunCases(m_monsters[0]);
                m_phase=1; m_started=Time.time;
            }
            else if(m_phase==1 && Time.time-m_started>.15f)
            {
                MonsterCombatStatus status=m_monsters[0].GetComponent<MonsterCombatStatus>(); status.PlayPredictedHit(38);
                Check(status.GetHP()==10000,"predicted monster damage number does not change HP");
                Replay("Build/Validation/CombatHitPackets.bin");
                Check(m_archer.GetComponent<PlayerInfo>().GetHP()<200,"C++ monster hit packet applies actual player HP");
                DamageNumberEffect[] numbers=UnityEngine.Object.FindObjectsOfType<DamageNumberEffect>(); bool red=false,yellow=false;
                for(int i=0;i<numbers.Length;++i) { Color c=numbers[i].GetComponent<TMP_Text>().color; if(c.g<.4f) red=true; if(c.g>.6f) yellow=true; }
                Check(!red && yellow,"monster predicted hit appears immediately, incoming player hit waits for attack impact");
                Check(m_archer.GetComponent<CombatHitFeedback>().GetPendingHitDelay()>0,"incoming hit scheduled from actual monster attack clip timing");
                for(int i=0;i<4;++i) { m_monsters[i].SetState(eMonsterState.Attack); m_monsters[i].PlayServerAttack(m_archer.transform); m_monsters[i].GetAnimator().Update(.2f); Check(m_monsters[i].GetAnimator().GetCurrentAnimatorStateInfo(0).IsName("Attack"),"real monster attack clip plays: "+i); }
                Vector3 old=m_archer.transform.position; byte[] echo=Packet(ePacketType.S2C_AttackStart,37); Put(echo,4,BitConverter.GetBytes(0UL)); Put(echo,21,BitConverter.GetBytes(999f)); Read(echo);
                Check(m_archer.transform.position==old,"local attack echo cannot rewind or teleport local player");
                m_phase=2; m_started=Time.time;
            }
            else if(m_phase==2 && Time.time-m_started>.05f && m_archer.GetComponent<CombatHitFeedback>().GetPendingHitDelay()==0)
            {
                DamageNumberEffect[] incoming=UnityEngine.Object.FindObjectsOfType<DamageNumberEffect>(); bool sawRed=false;
                for(int i=0;i<incoming.Length;++i) if(incoming[i].GetComponent<TMP_Text>().color.g<.4f) sawRed=true;
                Check(sawRed,"red player damage appears at delayed attack impact");
                Check(m_archer.transform.childCount>1,"player impact particles remain attached to victim");
                Capture("CombatFeedback");
                Replay("Build/Validation/CombatRapidPackets.bin");
                Check(m_monsters[0].GetComponent<MonsterCombatStatus>().GetHP()<10000,"actual C++ rapid results update monster HP");
                m_monsters[0].transform.position=new Vector3(11,0,7);
                m_monsters[0].SetServerPath(new Vector3(11,0,7),90,4,0,new List<Vector3>{new Vector3(15,0,7),new Vector3(15,0,11)});
                Check(m_monsters[0].transform.position==new Vector3(11,0,7),"new path changes no displayed position");
                m_previous=m_monsters[0].transform.position; m_previousTime=Time.time; m_frames=0;
                m_phase=3; m_started=Time.time;
            }
            else if(m_phase==3)
            {
                if(Time.time<=m_previousTime) return;
                float distance=Vector3.Distance(m_monsters[0].transform.position,m_previous);
                if(distance>4*(Time.time-m_previousTime)+.025f) throw new Exception("monster sped up or snapped: "+distance);
                ++m_frames; m_previous=m_monsters[0].transform.position; m_previousTime=Time.time;
                // 속사에 해당하는 잦은 HP/상태 스냅샷, 이동 좌표는 고의로 오래된 값
                m_monsters[0].ApplyServerState(new Vector3(11,0,7),0,2);
                if(m_monsters[0].transform.position!=m_previous) throw new Exception("state snapshot rewound movement");
                if(Time.time-m_started<2.6f) return;
                Check(m_frames>20 && Vector3.Distance(m_monsters[0].transform.position,new Vector3(15,0,11))<.01f,"corner route arrives at exact destination, fixed speed, no correction across "+m_frames+" frames");
                m_remote.transform.position=new Vector3(10,0,8); m_remote.ApplyMove(new Vector3(9,0,8),new Vector3(15,0,8),90);
                Check(m_remote.transform.position==new Vector3(10,0,8),"remote position report does not rewind movement");
                m_remote.PlayServerAttack(ePlayerObjectState.Attack,"NormalAttack01_SwordShield"); m_remote.ApplyMove(new Vector3(9,0,8),new Vector3(9,0,8),90);
                Check(m_remote.GetState()==ePlayerObjectState.Attack,"periodic stationary report does not interrupt remote attack");
                MonsterHitbox target=m_monsters[0].GetComponentInChildren<MonsterHitbox>(); PlayerCombat combat=m_archer.GetComponent<PlayerCombat>(); combat.CommandTarget(target,true); combat.Tick();
                Check(combat.IsAttacking(),"online archer starts basic attack against live target");
                Check(combat.TryHeavy(),"Skill A can replace basic before impact");
                // loopback으로 보낸 순서 검사: Cancel 뒤 Start(Skill A)
                byte[] sent=new byte[4096]; int length=0; while(m_peer.Available>0) length+=m_peer.GetStream().Read(sent,length,sent.Length-length);
                bool cancelled=false,replaced=false; int at=0;
                while(at+4<=length) { int size=BitConverter.ToUInt16(sent,at); ePacketType type=(ePacketType)BitConverter.ToUInt16(sent,at+2); if(type==ePacketType.C2S_AttackCancel) cancelled=true; if(type==ePacketType.C2S_AttackStart && sent[at+4]==2 && cancelled) replaced=true; at+=size; }
                Check(replaced,"actual outgoing Cancel then SkillA Start order");
                combat.StopCombat(); m_phase=4; m_started=Time.time;
            }
            else if(m_phase==4 && Time.time-m_started>1.2f)
            {
                Check(UnityEngine.Object.FindObjectsOfType<DamageNumberEffect>().Length==0,"damage number lifetime cleanup");
                CombatHitFeedback remoteFeedback=m_remote.GetComponent<CombatHitFeedback>();
                m_remoteHitCount=(int)typeof(CombatHitFeedback).GetField("m_numberIndex",Flags).GetValue(remoteFeedback);
                byte[] remoteHit=Packet(ePacketType.S2C_PlayerHit,36);
                Put(remoteHit,4,BitConverter.GetBytes(1UL));
                Put(remoteHit,12,BitConverter.GetBytes(0UL));
                Put(remoteHit,20,BitConverter.GetBytes(9UL));
                Put(remoteHit,28,BitConverter.GetBytes(291UL));
                Read(remoteHit);
                Check(m_remote.GetComponent<PlayerInfo>().GetHP()==291 && remoteFeedback.GetPendingHitDelay()>0,"observer applies remote victim HP and schedules the same timed hit feedback");
                Vector3 facing=m_remote.transform.position-m_monsters[0].transform.position; facing.y=0;
                Check(Vector3.Dot(m_monsters[0].transform.forward,facing.normalized)>.999f,"observer sees monster face the actual remote victim");
                CombatHitFeedback feedback=m_archer.GetComponent<CombatHitFeedback>();
                feedback.PlayDelayed(200,.35f);
                m_archer.GetComponent<PlayerInfo>().ApplyServerHP(0);
                m_archer.ApplyDeath();
                Check(m_archer.GetState()==ePlayerObjectState.Dead,"lethal hit immediately blocks player state and input");
                Check(!m_archer.GetAnimator().GetCurrentAnimatorStateInfo(0).IsName("Die_Bow"),"death pose waits for incoming impact instead of preceding it");
                m_phase=5; m_started=Time.time;
            }
            else if(m_phase==5 && Time.time-m_started>.5f && m_remote.GetComponent<CombatHitFeedback>().GetPendingHitDelay()==0)
            {
                CombatHitFeedback remoteFeedback=m_remote.GetComponent<CombatHitFeedback>();
                int hits=(int)typeof(CombatHitFeedback).GetField("m_numberIndex",Flags).GetValue(remoteFeedback);
                Check(hits==m_remoteHitCount+1 && m_remote.transform.childCount>1,"observer sees exactly one remote-player damage number and attached impact effect");
                Check(m_archer.GetAnimator().GetCurrentAnimatorStateInfo(0).IsName("Die_Bow"),"death animation starts once at lethal impact");
                VerifySharedPresentation();
                m_remoteTest = RemoteAttackPresentationVerification.Run(m_network, m_camera);
                m_phase = 6; m_started = Time.time;
            }
            else if(m_phase == 6 && !m_remoteTest.MoveNext())
            {
                Check(!m_failed,"no Unity runtime errors");
                Debug.Log("[CombatClientTest] COMPLETE checks="+m_checked+" (actual prefabs, production C++ packets, loopback, no saved scene changes)");
                m_client.Disconnect(); m_peer.Close(); m_listener.Stop(); m_nav.Remove(); EditorApplication.ExitPlaymode();
            }
        }
        catch(Exception ex)
        {
            m_failed=true; Debug.LogError("[CombatClientTest] FAIL "+ex); SessionState.SetBool(Key,false); if(m_client!=null) m_client.Disconnect(); if(m_peer!=null) m_peer.Close(); if(m_listener!=null) m_listener.Stop(); EditorApplication.Exit(1);
        }
    }

    static int HitCount(CombatHitFeedback _feedback) { return (int)typeof(CombatHitFeedback).GetField("m_numberIndex", Flags).GetValue(_feedback); }
    static void Impact(PlayerCombat _combat, float _normalized)
    {
        float duration = (float)typeof(PlayerCombat).GetField("m_attackDuration", Flags).GetValue(_combat);
        Set(_combat, "m_attackStart", Time.time - duration * _normalized);
        typeof(PlayerCombat).GetMethod("UpdateAttack", Flags).Invoke(_combat, null);
    }

    static Texture2D ReadFrame()
    {
        Canvas.ForceUpdateCanvases();
        m_camera.Render();
        RenderTexture.active = m_camera.targetTexture;
        Texture2D result = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); result.Apply();
        RenderTexture.active = null;
        return result;
    }

    static void VerifyImpactPixels(MonsterObject _monster, string _name)
    {
        ParticleSystem[] particles = _monster.GetComponentsInChildren<ParticleSystem>();
        List<Renderer> renderers = new List<Renderer>();
        int count = 0;
        for (int i = 0; i < particles.Length; ++i)
        {
            if (!particles[i].transform.IsChildOf(_monster.transform)) continue;
            particles[i].Simulate(.08f, false, true, true);
            count += particles[i].particleCount;
            Renderer renderer = particles[i].GetComponent<Renderer>();
            if (renderer == null) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int j = 0; j < materials.Length; ++j)
                if (materials[j] != null) Check(materials[j].shader != null && materials[j].shader.isSupported, _name + " hit material shader supported");
            renderers.Add(renderer); renderer.enabled = false;
        }
        Texture2D before = ReadFrame();
        for (int i = 0; i < renderers.Count; ++i) renderers[i].enabled = true;
        Texture2D after = ReadFrame();
        Color32[] a = before.GetPixels32(); Color32[] b = after.GetPixels32(); int changed = 0;
        for (int i = 0; i < a.Length; ++i)
            if (Math.Abs(a[i].r - b[i].r) + Math.Abs(a[i].g - b[i].g) + Math.Abs(a[i].b - b[i].b) > 20) ++changed;
        Check(count > 0 && changed > 100, _name + " original hit particles visibly rendered, changed pixels=" + changed);
        File.WriteAllBytes("Build/Validation/SharedHit_" + _name + ".png", after.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(before); UnityEngine.Object.DestroyImmediate(after);
    }

    static void VerifySharedPresentation()
    {
        const string root = "Assets/MYFolder/";
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Script/Effect/HitEffect.prefab").GetComponent<HitEffect>().m_hitEffectPrefab;
        string[] names = { "Monster/MonsterObject_Rat", "Monster/MonsterObject_Lizard", "Monster/MonsterObject_Ghost", "Monster/MonsterObject_Knight",
            "Player/LocalPlayerObject_Warrior", "Player/LocalPlayerObject_Archer", "Player/RemotePlayerObject_Warrior", "Player/RemotePlayerObject_Archer" };
        for (int i = 0; i < names.Length; ++i)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefab/Object/" + names[i] + ".prefab");
            Check(new SerializedObject(prefab.GetComponent<CombatHitFeedback>()).FindProperty("m_hitEffectPrefab").objectReferenceValue == original, "same legacy HitEffect asset: " + names[i]);
            MonsterSpawnPoint spawn = prefab.GetComponent<MonsterSpawnPoint>();
            if (spawn != null) Check(spawn.GetMaxAggroRange() >= 30, "prefab leash >= 30: " + names[i]);
        }
        typeof(ZoneSceneNetwork).GetProperty("Instance").SetValue(null, null);
        m_camera.transform.position = new Vector3(40, 9, 22); m_camera.transform.rotation = Quaternion.Euler(45, 0, 0);
        RenderTexture rt = new RenderTexture(1920, 1080, 24); m_camera.targetTexture = rt;
        for (int c = 0; c < 2; ++c)
        {
            string name = c == 0 ? "Warrior" : "Archer";
            GameObject player = Prefab("Player/LocalPlayerObject_" + name, new Vector3(40, 0, 30));
            player.GetComponent<LocalPlayerObject>().enabled = false; player.GetComponent<WorldPointerInput>().enabled = false;
            player.GetComponent<PlayerInfo>().Initialize(name, 6, 300, 300);
            MonsterObject target = Prefab("Monster/MonsterObject_Rat", new Vector3(40, 0, 32)).GetComponent<MonsterObject>();
            target.InitializeServer();
            MonsterCombatStatus status = target.GetComponent<MonsterCombatStatus>(); status.InitializeServerData(eMonsterType.Rat, 1, 10000, 10000);
            CombatHitFeedback feedback = target.GetComponent<CombatHitFeedback>();
            PlayerCombat combat = player.GetComponent<PlayerCombat>();
            MonsterHitbox box = target.GetComponentInChildren<MonsterHitbox>();
            Physics.SyncTransforms();
            combat.CommandTarget(box, true); combat.Tick();
            Check(combat.IsAttacking(), name + " real basic attack started");
            Impact(combat, .55f);
            Check(HitCount(feedback) == 1 && status.GetHP() == 10000, name + " basic shows predicted hit without changing HP");
            string clip = "NormalAttack01_SwordShield"; if (c == 1) clip = "Attack01_Bow";
            player.GetComponent<LocalPlayerObject>().GetAnimator().Play(clip, 0, .45f); player.GetComponent<LocalPlayerObject>().GetAnimator().Update(0);
            VerifyImpactPixels(target, name);
            Check(combat.TryHeavy(), name + " A accepted"); Impact(combat, .50f);
            int expected = 2;
            if (c == 0) { Impact(combat, .75f); expected = 3; }
            Check(HitCount(feedback) == expected, name + " A impact count (warrior two swings, archer one)");
            combat.StopCombat(); combat.CommandTarget(box, true);
            Check(combat.TryArea(), name + " S accepted"); Impact(combat, .80f);
            Check(HitCount(feedback) == expected + 1 && status.GetHP() == 10000, name + " S uses the same predicted impact");
            combat.StopCombat();
            if (c == 0)
            {
                GameObject npcPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + "Prefab/Object/NPC/TownGuardNPC.prefab");
                ChatBubbleUI prefabBubble = npcPrefab.GetComponentInChildren<ChatBubbleUI>(true);
                Check(prefabBubble != null && AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(prefabBubble)) == root + "Prefab/UI/Chat/ChatBubble.prefab", "NPC nests the exact player ChatBubble prefab");
                GameObject npc = UnityEngine.Object.Instantiate(npcPrefab, new Vector3(37, 0, 31), Quaternion.Euler(0, 180, 0));
                typeof(NPCObject).GetMethod("Start", Flags).Invoke(npc.GetComponent<NPCObject>(), null);
                ChatBubbleUI bubble = npc.GetComponentInChildren<ChatBubbleUI>(true);
                Set(bubble, "m_startTime", Time.unscaledTime - 10); typeof(ChatBubbleUI).GetMethod("LateUpdate", Flags).Invoke(bubble, null);
                Check(bubble.gameObject.activeSelf && bubble.GetComponent<CanvasGroup>().alpha == 1, "NPC dialogue persists after ten seconds");
                PlayerWorldUI worldUI = player.GetComponentInChildren<PlayerWorldUI>(true);
                worldUI.ShowChat("준비됐어요!");
                ChatBubbleUI chat = worldUI.GetComponentInChildren<ChatBubbleUI>(true);
                Set(chat, "m_startTime", Time.unscaledTime - .5f); typeof(PlayerWorldUI).GetMethod("LateUpdate", Flags).Invoke(worldUI, null);
                typeof(ChatBubbleUI).GetMethod("LateUpdate", Flags).Invoke(chat, null);
                Texture2D image = ReadFrame(); File.WriteAllBytes("Build/Validation/SharedSpeechBubble.png", image.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(image);
                Set(chat, "m_startTime", Time.unscaledTime - 3.1f); typeof(ChatBubbleUI).GetMethod("LateUpdate", Flags).Invoke(chat, null);
                Check(!chat.gameObject.activeSelf && bubble.gameObject.activeSelf, "player dialogue hides after three seconds; NPC remains");
                UnityEngine.Object.DestroyImmediate(npc);
            }
            UnityEngine.Object.DestroyImmediate(player); UnityEngine.Object.DestroyImmediate(target.gameObject);
        }
        m_camera.targetTexture = null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt);
        typeof(ZoneSceneNetwork).GetProperty("Instance").SetValue(null, m_network);
    }
}
