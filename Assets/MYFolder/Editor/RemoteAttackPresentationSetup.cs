using System;
using System.IO;
using UnityEditor;
using UnityEngine;

// 실행 중 생성하지 않고 공용 이펙트 참조를 플레이어 프리팹에 미리 연결
public static class RemoteAttackPresentationSetup
{
	const string Root = "Assets/MYFolder/Prefab/Object/";
	public static void Verify()
	{
		Run();
		CombatSyncClientVerification.Run();
	}
	public static void Run()
	{
		string backup = "Build/Validation/RemoteAttackPrefabs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
		Directory.CreateDirectory(backup);
		string[] classes = { "Warrior", "Archer" };
		for (int c = 0; c < classes.Length; ++c)
		{
			GameObject local = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Player/LocalPlayerObject_" + classes[c] + ".prefab");
			UnityEngine.Object controller = new SerializedObject(local.GetComponent<LocalPlayerObject>()).FindProperty("m_gameplayController").objectReferenceValue;
			if (controller == null) throw new Exception("Missing local gameplay controller: " + classes[c]);
			string[] kinds = { "Local", "Remote" };
			for (int i = 0; i < kinds.Length; ++i)
			{
				string path = Root + "Player/" + kinds[i] + "PlayerObject_" + classes[c] + ".prefab";
				File.Copy(path, backup + "/" + Path.GetFileName(path));
				GameObject root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					if (root.GetComponent<PlayerInfo>() == null) root.AddComponent<PlayerInfo>();
					PlayerAttackEffects effects = root.GetComponent<PlayerAttackEffects>();
					if (effects == null) effects = root.AddComponent<PlayerAttackEffects>();
					SerializedObject data = new SerializedObject(effects);
					string[] fields = { "m_arrowBasic", "m_arrowPower", "m_arrowRapid" };
					string[] assets = { "ArrowBasic", "ArrowPower", "ArrowRapid" };
					for (int n = 0; n < fields.Length; ++n)
						data.FindProperty(fields[n]).objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Effect/" + assets[n] + ".prefab").GetComponent<ArrowShotEffect>();
					data.FindProperty("m_areaEffectPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Effect/WarriorAreaFire.prefab");
					data.ApplyModifiedPropertiesWithoutUndo();
					if (i == 1)
					{
						SerializedObject player = new SerializedObject(root.GetComponent<RemotePlayerObject>());
						player.FindProperty("m_gameplayController").objectReferenceValue = controller;
						player.FindProperty("m_areaRadius").floatValue = new SerializedObject(local.GetComponent<PlayerCombat>()).FindProperty("m_areaRadius").floatValue;
						player.ApplyModifiedPropertiesWithoutUndo();
					}
					PrefabUtility.SaveAsPrefabAsset(root, path);
				}
				finally { PrefabUtility.UnloadPrefabContents(root); }
			}
		}
		AssetDatabase.SaveAssets();
		Debug.Log("[RemoteAttackSetup] PASS four player prefabs share production attack effects and class controllers. Backup=" + backup);
	}
}
