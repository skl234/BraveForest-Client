using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 기존 씬/프리팹의 설정만 갱신. 지형과 스폰 위치는 그대로 둠
public static class PlayerMonsterPresentationSetup
{
	const string Root = "Assets/MYFolder/";
	const string HitPath = "Assets/Travis Game Assets/Hit Impact Effects/Prefabs/Hits/Hit_02.prefab";
	static string m_backup;

	public static void Run()
	{
		m_backup = "Build/Validation/PresentationBackup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
		GameObject hit = AssetDatabase.LoadAssetAtPath<GameObject>(HitPath);
		HitEffect old = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Script/Effect/HitEffect.prefab").GetComponent<HitEffect>();
		if (hit == null || old.m_hitEffectPrefab != hit) throw new Exception("Original HitEffect reference differs");
		GameObject archer = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/Object/Player/LocalPlayerObject_Archer.prefab");
		float range = new SerializedObject(archer.GetComponent<PlayerCombat>()).FindProperty("m_archerRange").floatValue * 3;
		string[] monsters = { "Ghost", "Lizard", "Knight", "Rat" };
		for (int i = 0; i < monsters.Length; ++i) UpdatePrefab("Monster/MonsterObject_" + monsters[i], hit, range);
		string[] players = { "LocalPlayerObject_Warrior", "LocalPlayerObject_Archer", "RemotePlayerObject_Warrior", "RemotePlayerObject_Archer" };
		for (int i = 0; i < players.Length; ++i) UpdatePrefab("Player/" + players[i], hit, range);

		string[] scenes = { "TownScene", "BeginnerZoneScene" };
		for (int i = 0; i < scenes.Length; ++i)
		{
			string path = Root + "Scenes/" + scenes[i] + ".unity";
			Backup(path);
			var scene = EditorSceneManager.OpenScene(path);
			NPCObject[] npcs = UnityEngine.Object.FindObjectsOfType<NPCObject>(true);
			for (int j = 0; j < npcs.Length; ++j)
			{
				string npcPath = Root + "Prefab/Object/NPC/TownGuardNPC.prefab";
				Backup(npcPath);
				WorldPresentationSetup.AttachNPCBubble(npcs[j]);
				PrefabUtility.SaveAsPrefabAssetAndConnect(npcs[j].gameObject, npcPath, InteractionMode.AutomatedAction);
			}
			MonsterSpawnPoint[] spawns = UnityEngine.Object.FindObjectsOfType<MonsterSpawnPoint>(true);
			for (int j = 0; j < spawns.Length; ++j) SetRange(spawns[j], range);
			EditorSceneManager.SaveScene(scene);
			MonsterSpawnExporter[] exporters = UnityEngine.Object.FindObjectsOfType<MonsterSpawnExporter>(true);
			for (int j = 0; j < exporters.Length; ++j)
			{
				if (!exporters[j].Validate()) throw new Exception("Invalid spawns in " + scenes[i]);
				exporters[j].Export();
			}
		}
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
		Debug.Log("[PlayerMonsterPresentation] COMPLETE shared original hit, shared NPC bubble, maxAggroRange=" + range + ", backup=" + m_backup);
	}

	static void Backup(string _path)
	{
		string target = Path.Combine(m_backup, _path);
		Directory.CreateDirectory(Path.GetDirectoryName(target));
		if (!File.Exists(target)) File.Copy(_path, target);
	}

	static void SetRange(MonsterSpawnPoint _spawn, float _range)
	{
		if (_spawn == null || _spawn.GetMaxAggroRange() >= _range) return;
		SerializedObject data = new SerializedObject(_spawn);
		data.FindProperty("m_maxAggroRange").floatValue = _range;
		data.ApplyModifiedPropertiesWithoutUndo();
		if (PrefabUtility.IsPartOfPrefabInstance(_spawn)) PrefabUtility.RecordPrefabInstancePropertyModifications(_spawn);
	}

	static void UpdatePrefab(string _name, GameObject _hit, float _range)
	{
		string path = Root + "Prefab/Object/" + _name + ".prefab";
		Backup(path);
		GameObject root = PrefabUtility.LoadPrefabContents(path);
		try
		{
			CombatHitFeedback feedback = root.GetComponent<CombatHitFeedback>();
			if (feedback == null) throw new Exception("CombatHitFeedback missing: " + path);
			SerializedObject data = new SerializedObject(feedback);
			data.FindProperty("m_hitEffectPrefab").objectReferenceValue = _hit;
			data.ApplyModifiedPropertiesWithoutUndo();
			MonsterCombatStatus status = root.GetComponent<MonsterCombatStatus>();
			if (status != null)
			{
				data = new SerializedObject(status);
				data.FindProperty("m_hitEffectPrefab").objectReferenceValue = _hit;
				data.ApplyModifiedPropertiesWithoutUndo();
			}
			SetRange(root.GetComponent<MonsterSpawnPoint>(), _range);
			PrefabUtility.SaveAsPrefabAsset(root, path);
		}
		finally { PrefabUtility.UnloadPrefabContents(root); }
	}
}
