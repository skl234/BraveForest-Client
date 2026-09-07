using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TownPlacementSetup
{
	const string Root = "Assets/MYFolder/";
	const string ScenePath = Root + "Scenes/TownScene.unity";
	const string Output = "Build/Validation/TownLayout";
	const string BasePrefab = Root + "Prefab/Environment/ObstacleBase.prefab";
	public static readonly Vector3[] LampPositions =
	{
		new Vector3(42, 0, 18), new Vector3(58, 0, 18),
		new Vector3(41, 0, 30), new Vector3(57, 0, 30),
		new Vector3(40, 0, 40), new Vector3(59, 0, 40),
		new Vector3(30, 0, 55), new Vector3(70, 0, 55),
		new Vector3(47, 0, 76), new Vector3(53, 0, 76)
	};

	public static void FinishVisuals()
	{
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		try
		{
			TownSunSetup.Apply();
			ZoneMapBuilder.RefreshTownMap();
			TownPlacementVerification.Run();
			ValidateAudit();
		}
		finally { Restore(setup); }
	}

	[MenuItem("MYFolder/Environment/마을 배치 정리 및 데이터 갱신")]
	public static void Apply()
	{
		if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		Backup();
		try
		{
			Scene scene = EditorSceneManager.OpenScene(ScenePath);
			ApplyLayout();
			EditorSceneManager.MarkSceneDirty(scene);
			EditorSceneManager.SaveScene(scene);
			RebuildCurrentScene();
			ZoneMapBuilder.RefreshTownMap();
			TownPlacementVerification.Run();
			ValidateAudit();
			Debug.Log("[TownLayout] COMPLETE: scene, NavMesh, Block cells, minimap and server Data updated");
		}
		finally { Restore(setup); }
	}

	[MenuItem("MYFolder/FieldData/마을 NavMesh 및 서버 맵 파일 갱신")]
	public static void RebuildData()
	{
		if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		Backup();
		try
		{
			EditorSceneManager.OpenScene(ScenePath);
			RebuildCurrentScene();
			ZoneMapBuilder.RefreshTownMap();
			TownPlacementVerification.Run();
			ValidateAudit();
		}
		finally { Restore(setup); }
	}

	static void Backup()
	{
		string directory = Output + "/Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
		Directory.CreateDirectory(directory);
		string[] paths = { ScenePath, Root + "Scenes/TownScene/NavMesh.asset", Root + "Image/Map/TownMap.png", Root + "ExportedFieldData/TownField.txt" };
		for (int i = 0; i < paths.Length; ++i)
		{
			if (File.Exists(paths[i])) File.Copy(paths[i], directory + "/" + Path.GetFileName(paths[i]));
		}
	}

	static void ApplyLayout()
	{
		Terrain terrain = Terrain.activeTerrain;
		Transform environment = GameObject.Find("TownEnvironment").transform;
		GameObject basePrefab = CreateBasePrefab();
		Material grass = CreateMaterial("ObstacleGrass", new Color(.43f, .53f, .29f));
		int lampIndex = 0;
		for (int i = 0; i < environment.childCount; ++i)
		{
			Transform item = environment.GetChild(i);
			if (item.name == "LampPost")
			{
				if (lampIndex >= LampPositions.Length) throw new Exception("가로등 개수 확인 필요");
				Vector3 position = LampPositions[lampIndex++];
				position.y = terrain.SampleHeight(position) + terrain.transform.position.y + .25f;
				item.position = position;
				AddBase(item, basePrefab, 1.6f, .25f, null);
			}
			else if (item.name.StartsWith("Tree") || item.name.StartsWith("Rock"))
			{
				Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
				float bottom = float.MaxValue;
				for (int j = 0; j < renderers.Length; ++j)
				{
					if (renderers[j].transform.name == "ObstacleBase") continue;
					bottom = Mathf.Min(bottom, renderers[j].bounds.min.y);
				}
				Vector3 position = item.position;
				float ground = terrain.SampleHeight(position) + terrain.transform.position.y;
				float offset = -.12f;
				if (item.name.StartsWith("Tree")) offset = .25f;
				position.y += ground + offset - bottom;
				item.position = position;
				if (item.name.StartsWith("Tree")) AddBase(item, basePrefab, 4.2f, .3f, grass);
			}
			if (item.GetComponent<ObstacleObject>() == null) item.gameObject.AddComponent<ObstacleObject>();
		}
		if (lampIndex != LampPositions.Length) throw new Exception("가로등 개수 불일치");
		SetObstacle(environment);
		Transform portals = GameObject.Find("Field").transform.Find("Portals");
		if (portals != null)
		{
			SetObstacle(portals);
			TMPro.TextMeshPro[] labels = portals.GetComponentsInChildren<TMPro.TextMeshPro>(true);
			for (int i = 0; i < labels.Length; ++i) labels[i].transform.localRotation = Quaternion.identity;
		}
		// 바깥쪽 장식 지형은 햇빛을 가로막는 구조물이 아님
		GameObject boundary = GameObject.Find("VisualBoundary");
		if (boundary != null)
		{
			Renderer[] renderers = boundary.GetComponentsInChildren<Renderer>();
			for (int i = 0; i < renderers.Length; ++i) renderers[i].shadowCastingMode = ShadowCastingMode.Off;
		}
	}

	static void SetObstacle(Transform _root)
	{
		int layer = LayerMask.NameToLayer("Obstacle");
		if (layer < 0) throw new Exception("Obstacle 레이어 없음");
		Transform[] list = _root.GetComponentsInChildren<Transform>(true);
		for (int i = 0; i < list.Length; ++i)
		{
			list[i].gameObject.layer = layer;
			GameObjectUtility.SetStaticEditorFlags(list[i].gameObject, GameObjectUtility.GetStaticEditorFlags(list[i].gameObject) | StaticEditorFlags.NavigationStatic);
			GameObjectUtility.SetNavMeshArea(list[i].gameObject, 1);
			if (PrefabUtility.IsPartOfPrefabInstance(list[i])) PrefabUtility.RecordPrefabInstancePropertyModifications(list[i].gameObject);
		}
	}

	static GameObject CreateBasePrefab()
	{
		GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefab);
		if (existing != null) return existing;
		Directory.CreateDirectory(Path.GetDirectoryName(BasePrefab));
		GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
		root.name = "ObstacleBase";
		UnityEngine.Object.DestroyImmediate(root.GetComponent<Collider>());
		MeshCollider collider = root.AddComponent<MeshCollider>();
		collider.sharedMesh = root.GetComponent<MeshFilter>().sharedMesh;
		collider.convex = true;
		root.AddComponent<ObstacleObject>();
		root.GetComponent<Renderer>().sharedMaterial = CreateMaterial("ObstacleStone", new Color(.43f, .44f, .38f));
		SetObstacle(root.transform);
		GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BasePrefab);
		UnityEngine.Object.DestroyImmediate(root);
		return prefab;
	}

	static Material CreateMaterial(string _name, Color _color)
	{
		string path = Root + "Prefab/Environment/" + _name + ".mat";
		Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
		if (material != null) return material;
		material = new Material(Shader.Find("Standard"));
		material.color = _color;
		material.SetFloat("_Glossiness", .05f);
		AssetDatabase.CreateAsset(material, path);
		return material;
	}

	static void AddBase(Transform _parent, GameObject _prefab, float _diameter, float _height, Material _material)
	{
		Transform item = _parent.Find("ObstacleBase");
		if (item == null) item = ((GameObject)PrefabUtility.InstantiatePrefab(_prefab, _parent)).transform;
		item.name = "ObstacleBase";
		Vector3 scale = _parent.lossyScale;
		item.localScale = new Vector3(_diameter / scale.x, _height / (2 * scale.y), _diameter / scale.z);
		item.position = new Vector3(_parent.position.x, Terrain.activeTerrain.SampleHeight(_parent.position) + Terrain.activeTerrain.transform.position.y + _height * .5f, _parent.position.z);
		if (_material != null) item.GetComponent<Renderer>().sharedMaterial = _material;
		PrefabUtility.RecordPrefabInstancePropertyModifications(item);
		PrefabUtility.RecordPrefabInstancePropertyModifications(item.GetComponent<Renderer>());
	}

	static void RebuildCurrentScene()
	{
		GameObject environment = GameObject.Find("TownEnvironment");
		if (environment == null) throw new Exception("TownEnvironment 없음");
		SetObstacle(environment.transform);
		Physics.SyncTransforms();
		UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
		FieldDataExporter exporter = UnityEngine.Object.FindObjectOfType<FieldDataExporter>(true);
		SerializedObject data = new SerializedObject(exporter);
		data.FindProperty("m_obstacleMask").intValue = LayerMask.GetMask("Obstacle");
		data.FindProperty("m_requireNavMesh").boolValue = true;
		data.ApplyModifiedPropertiesWithoutUndo();
		exporter.Export();
		EditorUtility.SetDirty(exporter);
		EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
		AssetDatabase.SaveAssets();
		// 베이크 에셋 재임포트 후 저장된 씬의 NavMesh로 다시 확인
		EditorSceneManager.OpenScene(ScenePath);
		ValidatePath(new Vector3(50, 0, 20), new Vector3(27, 0, 80));
		ValidatePath(new Vector3(50, 0, 20), new Vector3(73, 0, 80));
		ValidatePath(new Vector3(27, 0, 80), new Vector3(73, 0, 80));
		string source = Root + "ExportedFieldData/TownField.txt";
		string target = Path.GetFullPath(Path.Combine(Application.dataPath, "../../MMONetwork2/Data/TownField.txt"));
		File.Copy(source, target, true);
		Debug.Log("[TownLayout] FILE " + target);
	}

	static void ValidatePath(Vector3 _start, Vector3 _end)
	{
		bool hasStart = NavMesh.SamplePosition(_start, out NavMeshHit start, .3f, NavMesh.AllAreas);
		bool hasEnd = NavMesh.SamplePosition(_end, out NavMeshHit end, .3f, NavMesh.AllAreas);
		if (!hasStart || !hasEnd) throw new Exception("마을 경로 지점 확인: " + _start + "=" + hasStart + " " + _end + "=" + hasEnd + " triangles=" + NavMesh.CalculateTriangulation().indices.Length);
		NavMeshPath path = new NavMeshPath();
		if (!NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) throw new Exception("마을 주요 경로가 끊겼습니다.");
		Debug.Log("[TownLayout] PATH PASS " + _start + " -> " + _end + " corners=" + path.corners.Length);
	}

	static void ValidateAudit()
	{
		string[] lines = File.ReadAllLines("Build/Validation/TownPlacement/Audit.tsv");
		int failures = 0;
		for (int i = 1; i < lines.Length; ++i)
		{
			string[] values = lines[i].Split('\t');
			if (values.Length != 8) continue;
			if (int.Parse(values[7]) != 0) { Debug.LogError("[TownLayout] Block 누락 " + lines[i]); ++failures; }
			if (values[4] != "True" || values[5] != "False") { Debug.LogError("[TownLayout] 중심 판정 불일치 " + lines[i]); ++failures; }
		}
		if (failures != 0) throw new Exception("장애물 검증 실패 " + failures);
		Debug.Log("[TownLayout] OBSTACLE PASS " + (lines.Length - 1) + " objects; no solid sample on walkable cell");
	}

	static void Restore(SceneSetup[] _setup)
	{
		if (_setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(_setup);
		else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
	}
}
