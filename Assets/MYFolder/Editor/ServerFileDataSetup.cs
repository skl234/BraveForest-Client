using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ServerFileDataSetup
{
	const string Root = "Assets/MYFolder/";
	const string DataRoot = Root + "ExportedFieldData/";

	// 기존 DB에서 확인한 값을 처음 한 번만 씬에 옮김
	public static void Migrate()
	{
		string backup = "Build/Validation/Backups/FileData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
		Directory.CreateDirectory(backup);
		string[] names = { "TownScene", "BeginnerZoneScene" };
		for (int i = 0; i < names.Length; ++i)
		{
			string path = Root + "Scenes/" + names[i] + ".unity";
			File.Copy(path, backup + "/" + names[i] + ".unity");
			Scene scene = EditorSceneManager.OpenScene(path);
			ZoneSceneInfo info = UnityEngine.Object.FindObjectOfType<ZoneSceneInfo>(true);
			SerializedObject data = new SerializedObject(info);
			Vector3 entry = new Vector3(50, 0, 20);
			if (i == 1) entry = new Vector3(22, 0, 23);
			data.FindProperty("m_enterPosition").vector3Value = entry;
			data.FindProperty("m_isStartZone").boolValue = i == 0;
			data.FindProperty("m_isActive").boolValue = true;
			data.ApplyModifiedPropertiesWithoutUndo();
			ZonePortal[] portals = UnityEngine.Object.FindObjectsOfType<ZonePortal>(true);
			for (int j = 0; j < portals.Length; ++j)
			{
				if (!portals[j].IsActive()) continue;
				SerializedObject portal = new SerializedObject(portals[j]);
				ulong target = 2;
				Vector3 destination = new Vector3(22, 0, 23);
				if (i == 1) { target = 1; destination = new Vector3(27, 0, 80); }
				portal.FindProperty("m_targetZoneId").ulongValue = target;
				portal.FindProperty("m_targetEnterPosition").vector3Value = destination;
				portal.ApplyModifiedPropertiesWithoutUndo();
			}
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
		}
		Export();
		TownPlacementVerification.Run();
	}

	[MenuItem("MYFolder/FieldData/서버용 존 및 스폰 파일 내보내기")]
	public static void Export()
	{
		if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		List<string> zoneLines = new List<string>();
		List<string> portalLines = new List<string>();
		List<string> files = new List<string>();
		HashSet<ulong> ids = new HashSet<ulong>();
		List<ulong> targets = new List<ulong>();
		int starts = 0;
		try
		{
			string[] scenes = AssetDatabase.FindAssets("t:Scene", new[] { Root + "Scenes" });
			for (int i = 0; i < scenes.Length; ++i)
			{
				Scene scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(scenes[i]));
				ZoneSceneInfo info = UnityEngine.Object.FindObjectOfType<ZoneSceneInfo>(true);
				if (info == null) continue;
				if (!info.IsActive()) continue;
				if (info.GetZoneId() == 0 || !ids.Add(info.GetZoneId())) throw new Exception("중복/잘못된 ZoneId");
				int start = 0;
				if (info.IsStartZone()) { start = 1; ++starts; }
				Vector3 entry = info.GetEnterPosition();
				zoneLines.Add(string.Format(CultureInfo.InvariantCulture, "Zone {0} \"{1}\" \"{2}\" {3:F3} 0 {4:F3} {5} 1",
					info.GetZoneId(), info.GetZoneName().Replace("\"", ""), scene.name, entry.x, entry.z, start));
				ZonePortal[] portals = UnityEngine.Object.FindObjectsOfType<ZonePortal>(true);
				HashSet<ulong> portalIds = new HashSet<ulong>();
				for (int j = 0; j < portals.Length; ++j)
				{
					ZonePortal portal = portals[j];
					if (!portal.IsActive()) continue;
					if (!portalIds.Add(portal.GetPortalId())) throw new Exception("중복 PortalId");
					Vector3 position = portal.GetTargetEnterPosition();
					portalLines.Add(string.Format(CultureInfo.InvariantCulture, "Portal {0} {1} {2} {3:F3} 0 {4:F3} 1",
						info.GetZoneId(), portal.GetPortalId(), portal.GetTargetZoneId(), position.x, position.z));
					targets.Add(portal.GetTargetZoneId());
				}
				MonsterSpawnExporter spawns = UnityEngine.Object.FindObjectOfType<MonsterSpawnExporter>(true);
				if (spawns == null || !spawns.Validate()) throw new Exception(scene.name + " 스폰 설정 오류");
				spawns.Export();
				files.Add(scene.name.Replace("Scene", "") + "Spawn.txt");
				files.Add(scene.name.Replace("Scene", "") + "Field.txt");
				Debug.Log("[ServerFileData] " + scene.name + " Zone=" + info.GetZoneId() + " Spawns=" + spawns.GetSpawnPointList().Length);
			}
			if (starts != 1) throw new Exception("시작 존은 정확히 하나여야 함");
			for (int i = 0; i < targets.Count; ++i) if (!ids.Contains(targets[i])) throw new Exception("존이 없는 포탈 대상");
			zoneLines.Sort(StringComparer.Ordinal);
			portalLines.Sort(StringComparer.Ordinal);
			string text = "ZoneCount " + zoneLines.Count + "\n" + string.Join("\n", zoneLines) + "\nPortalCount " + portalLines.Count + "\n" + string.Join("\n", portalLines) + "\n";
			File.WriteAllText(DataRoot + "ZoneInfo.txt", text);
			files.Add("ZoneInfo.txt");
			string serverRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../MMONetwork2"));
			if (!File.Exists(Path.Combine(serverRoot, "MMONetwork.sln"))) throw new Exception("서버 프로젝트 위치 확인 필요");
			string destination = Path.Combine(serverRoot, "Data");
			Directory.CreateDirectory(destination);
			for (int i = 0; i < files.Count; ++i) File.Copy(DataRoot + files[i], Path.Combine(destination, files[i]), true);
			AssetDatabase.Refresh();
			Debug.Log("[ServerFileData] COMPLETE " + destination + " (DB writes: 0)");
		}
		finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
	}
}
