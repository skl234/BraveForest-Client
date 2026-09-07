using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// 임시 Play Mode 씬에서 실제 패킷 수신 함수와 배포 프리팹을 검사. DB/저장된 씬은 변경하지 않음
public static class RemoteAttackPresentationVerification
{
	const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
	static ZoneSceneNetwork m_network;
	static Camera m_camera;
	static Dictionary<string, GameObject> m_map;
	static void Check(bool value, string message)
	{
		if (!value) throw new Exception(message);
		Debug.Log("[RemoteAttackTest] PASS " + message);
	}
	static int Hits(GameObject victim) { return (int)typeof(CombatHitFeedback).GetField("m_numberIndex", Flags).GetValue(victim.GetComponent<CombatHitFeedback>()); }
	static float Field(RemotePlayerObject player, string name) { return (float)typeof(RemotePlayerObject).GetField(name, Flags).GetValue(player); }
	static void Read(byte[] bytes) { typeof(ZoneSceneNetwork).GetMethod("ReadPacket", Flags).Invoke(m_network, new object[] { bytes }); }
	static void Put(byte[] bytes, int offset, byte[] value) { Buffer.BlockCopy(value, 0, bytes, offset, value.Length); }
	static byte[] Packet(ePacketType type, int size)
	{
		byte[] result = new byte[size]; Put(result, 0, BitConverter.GetBytes((ushort)size)); Put(result, 2, BitConverter.GetBytes((ushort)type)); return result;
	}
	static void Start(byte type, ulong source, ulong target)
	{
		byte[] bytes = Packet(ePacketType.S2C_AttackStart, 37);
		Put(bytes, 4, BitConverter.GetBytes(source)); bytes[12] = type; Put(bytes, 13, BitConverter.GetBytes(target)); Read(bytes);
	}
	static void Result(ulong source, ulong target, int damage, int hp)
	{
		byte[] bytes = Packet(ePacketType.S2C_AttackResult, 46); bytes[4] = 0;
		Put(bytes, 5, BitConverter.GetBytes(source)); bytes[13] = 1;
		Put(bytes, 14, BitConverter.GetBytes(target)); Put(bytes, 22, BitConverter.GetBytes((ulong)damage)); Put(bytes, 30, BitConverter.GetBytes((ulong)hp)); Read(bytes);
	}
	static GameObject Prefab(string path, Vector3 position)
	{
		return UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/MYFolder/Prefab/Object/" + path + ".prefab"), position, Quaternion.identity);
	}
	static void Capture(string name)
	{
		Canvas.ForceUpdateCanvases();
		RenderTexture rt = new RenderTexture(1920, 1080, 24); m_camera.targetTexture = rt; m_camera.Render();
		RenderTexture.active = rt;
		Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false); image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
		File.WriteAllBytes("Build/Validation/" + name + ".png", image.EncodeToPNG());
		RenderTexture.active = null; m_camera.targetTexture = null; UnityEngine.Object.Destroy(image); rt.Release(); UnityEngine.Object.Destroy(rt);
	}
	public static IEnumerator Run(ZoneSceneNetwork network, Camera camera)
	{
		m_network = network; m_camera = camera;
		m_map = (Dictionary<string, GameObject>)typeof(ZoneSceneNetwork).GetField("m_objectMap", Flags).GetValue(network);
		m_camera.transform.position = new Vector3(40, 9, 22); m_camera.transform.rotation = Quaternion.Euler(45, 0, 0);
		GameObject target = Prefab("Monster/MonsterObject_Rat", new Vector3(40, 0, 32));
		target.GetComponent<MonsterObject>().InitializeServer();
		MonsterCombatStatus status = target.GetComponent<MonsterCombatStatus>(); status.InitializeServerData(eMonsterType.Rat, 1, 10000, 10000);
		m_map["1:90"] = target;
		string[] classes = { "Warrior", "Archer" };
		for (int c = 0; c < classes.Length; ++c)
		{
			GameObject root = Prefab("Player/RemotePlayerObject_" + classes[c], new Vector3(40, 0, 30));
			if (c == 1) root.transform.position = new Vector3(38, 0, 29);
			root.GetComponent<PlayerInfo>().Initialize("원격 " + classes[c], 6, 300, 300);
			RemotePlayerObject player = root.GetComponent<RemotePlayerObject>(); m_map["0:80"] = root;
			yield return null;
			PlayerAttackEffects effects = root.GetComponent<PlayerAttackEffects>();
			Check(effects != null, classes[c] + " shared attack presentation on production remote prefab");
			for (byte type = 1; type <= 2; ++type)
			{
				int count = Hits(target); status.ApplyServerHP(10000);
				Start(type, 80, 90); yield return null;
				Check(player.GetState() == ePlayerObjectState.Attack || player.GetState() == ePlayerObjectState.Skill, classes[c] + " attack animation state received");
				Animator animator = player.GetAnimator();
				string clip = (string)typeof(RemotePlayerObject).GetField("m_attackClip", Flags).GetValue(player);
				Check(animator.GetCurrentAnimatorStateInfo(0).IsName(clip), classes[c] + " controller plays " + clip);
				Result(80, 90, 40, 9960);
				Check(status.GetHP() == 9960 && Hits(target) == count, classes[c] + " HP authoritative now, impact waits for animation");
				float limit = Time.time + 2;
				while (Hits(target) == count && Time.time < limit) yield return null;
				Check(Hits(target) == count + 1, classes[c] + " first impact and damage number via received result");
				yield return null;
				if (c == 1) Check(UnityEngine.Object.FindObjectsOfType<ArrowShotEffect>().Length > 0, "Archer " + type + " full-length arrow trail appears");
				ParticleSystem[] particles = target.GetComponentsInChildren<ParticleSystem>();
				Check(particles.Length > 0, "original hit particles attached to remote victim");
				for (int i = 0; i < particles.Length; ++i) particles[i].Simulate(.08f, false, true, true);
				Transform hit = target.transform.Find("Hit_02(Clone)");
				Check(hit != null && Mathf.Abs(hit.lossyScale.x - 4.05f) < .02f, "Hit_02 original root scale preserved (3 x 1.35)");
				Capture("Remote_" + classes[c] + "_" + type);
				if (c == 0 && type == 2)
				{
					while (Hits(target) < count + 2 && Time.time < limit + 1) yield return null;
					Check(Hits(target) == count + 2 && status.GetHP() == 9960, "Warrior A two visual impacts, one authoritative HP result");
				}
				float finish = Time.time + .2f;
				while (Time.time < finish) yield return null;
			}
			if (c == 0)
			{
				int initial = UnityEngine.Object.FindObjectsOfType<AreaFireEffect>().Length;
				Start(3, 80, ulong.MaxValue);
				float impactTime = Field(player, "m_attackStarted") + Field(player, "m_attackDuration") * .65f;
				while (Time.time < impactTime + .04f) yield return null;
				Check(UnityEngine.Object.FindObjectsOfType<AreaFireEffect>().Length == initial + 1, "untargeted Warrior S produces exactly one fire effect");
				int count = Hits(target); Result(80, 90, 55, 9905); yield return null;
				Check(Hits(target) == count + 1, "Warrior S remote victim receives hit and number");
				Capture("Remote_Warrior_Area");
				Start(3, 80, ulong.MaxValue); Start(0, 80, ulong.MaxValue);
				float until = Time.time + 1.2f; while (Time.time < until) yield return null;
				Check(UnityEngine.Object.FindObjectsOfType<AreaFireEffect>().Length <= initial + 1 && player.GetState() == ePlayerObjectState.Idle, "cancelled fire attack creates no extra area effect");
			}
			else
			{
				Start(4, 80, 90); int count = Hits(target);
				for (int shot = 0; shot < 20; ++shot)
				{
					target.transform.position = new Vector3(40 + shot * .04f, 0, 32);
					Result(80, 90, 8, 9800 - shot * 8);
					float until = Time.time + .15f; while (Time.time < until) yield return null;
					if (shot == 8)
					{
						Vector3 facing = target.transform.position - root.transform.position; facing.y = 0;
						Check(Vector3.Dot(root.transform.forward, facing.normalized) > .99f, "remote rapid fire faces moving target");
						Capture("Remote_Archer_Rapid");
					}
				}
				Check(Hits(target) == count + 20, "20 rapid results produce exactly 20 hit effects and damage numbers");
				Start(4, 80, 90); Result(80, 90, 1, 0);
				float end = Time.time + .3f; while (Time.time < end) yield return null;
				Check(player.GetState() == ePlayerObjectState.Idle, "remote rapid fire stops when target dies");
				count = Hits(target); end = Time.time + .3f; while (Time.time < end) yield return null;
				Check(Hits(target) == count, "no invented rapid hits after death");
			}
			m_map.Remove("0:80"); UnityEngine.Object.Destroy(root); yield return null;
		}
		status.ApplyServerHP(10000); int previous = Hits(target);
		Result(999, 90, 7, 9993);
		Check(Hits(target) == previous + 1, "victim in view still shows damage when attacker is outside view");
		previous = Hits(target); Result(0, 90, 7, 9986);
		Check(Hits(target) == previous, "local echo only applies HP, never duplicates predicted impact");
		Result(999, ulong.MaxValue, 0, 0);
		Check(status.GetHP() == 9986, "empty area response changes no unrelated monster");
		m_map.Remove("1:90"); UnityEngine.Object.Destroy(target);
		Debug.Log("[RemoteAttackTest] COMPLETE all production remote attack paths and captures");
	}
}
