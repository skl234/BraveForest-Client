using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

public static class ZonePortalSetup
{
	const string Root = "Assets/MYFolder/";
	const string Output = "Build/Validation/ZonePortal";
	static readonly string[] SceneNames = { "TownScene", "BeginnerZoneScene" };
	const string PortalPrefab = Root + "Prefab/Object/Portal/ZonePortal.prefab";

	[MenuItem("MYFolder/Environment/포탈 크기 및 근접 판정 정리")]
	public static void Apply()
	{
		if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		Directory.CreateDirectory(Output);
		string backup = Output + "/Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
		Directory.CreateDirectory(backup);
		for (int i = 0; i < SceneNames.Length; ++i)
		{
			File.Copy(Root + "Scenes/" + SceneNames[i] + ".unity", backup + "/" + SceneNames[i] + ".unity");
			string name = SceneNames[i].Replace("Scene", "");
			File.Copy(Root + "ExportedFieldData/" + name + "Field.txt", backup + "/" + name + "Field.txt");
			File.Copy(Root + "Image/Map/" + name + "Map.png", backup + "/" + name + "Map.png");
		}
		string[] oldPrefabs = { "BeginnerZonePortal", "VeteranZonePortal_Disabled" };
		for (int i = 0; i < oldPrefabs.Length; ++i)
			File.Copy(Root + "Prefab/Object/Portal/" + oldPrefabs[i] + ".prefab", backup + "/" + oldPrefabs[i] + ".prefab");
		try
		{
			GameObject prefab = CreatePrefab();
			for (int i = 0; i < SceneNames.Length; ++i)
			{
				Scene scene = EditorSceneManager.OpenScene(Root + "Scenes/" + SceneNames[i] + ".unity");
				ZonePortal[] portals = UnityEngine.Object.FindObjectsOfType<ZonePortal>(true);
				for (int j = 0; j < portals.Length; ++j) ReplacePortal(portals[j], prefab);
				GameObject legacy = GameObject.Find("VillagePortal");
				if (legacy != null)
				{
					// 예전 장식 문만 제거. 도착 구역과 마커는 그대로 둠
					UnityEngine.Object.DestroyImmediate(legacy);
					Debug.Log("[ZonePortal] Removed duplicate VillagePortal (scene backed up)");
				}
				EditorSceneManager.MarkSceneDirty(scene);
				EditorSceneManager.SaveScene(scene);
				if (i == 1) RebuildBeginner();
			}
			TownPlacementSetup.RebuildData();
			ZoneMapBuilder.RefreshBeginnerMap();
			// 존 ID와 도착 위치는 유지. 갱신된 Field 파일을 함께 배포
			ServerFileDataSetup.Export();
			Verify();
			Debug.Log("[ZonePortal] COMPLETE " + backup);
		}
		finally { Restore(setup); }
	}

	static GameObject CreatePrefab()
	{
		GameObject root = new GameObject("ZonePortal", typeof(ZonePortal));
		Material stone = AssetDatabase.LoadAssetAtPath<Material>(Root + "Material/PortalStone.mat");
		Material glow = AssetDatabase.LoadAssetAtPath<Material>(Root + "Material/PortalGlow.mat");
		if (stone == null || glow == null) throw new Exception("포탈 머티리얼 없음");
		CreatePiece(root.transform, "LeftPillar", new Vector3(-1.675f, 2.1f, 0), new Vector3(.45f, 4.2f, .65f), stone, true);
		CreatePiece(root.transform, "RightPillar", new Vector3(1.675f, 2.1f, 0), new Vector3(.45f, 4.2f, .65f), stone, true);
		CreatePiece(root.transform, "Top", new Vector3(0, 3.975f, 0), new Vector3(3.8f, .45f, .65f), stone, true);
		CreatePiece(root.transform, "PortalGlow", new Vector3(0, 1.85f, 0), new Vector3(2.9f, 3.7f, .1f), glow, false);
		GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PortalPrefab);
		UnityEngine.Object.DestroyImmediate(root);
		return prefab;
	}

	static void CreatePiece(Transform _parent, string _name, Vector3 _position, Vector3 _scale, Material _material, bool _obstacle)
	{
		GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
		piece.name = _name;
		piece.transform.SetParent(_parent, false);
		piece.transform.localPosition = _position;
		piece.transform.localScale = _scale;
		piece.GetComponent<Renderer>().sharedMaterial = _material;
		piece.layer = LayerMask.NameToLayer("Obstacle");
		NavMeshModifier modifier = piece.AddComponent<NavMeshModifier>();
		if (_obstacle)
		{
			piece.AddComponent<ObstacleObject>();
			GameObjectUtility.SetStaticEditorFlags(piece, StaticEditorFlags.NavigationStatic);
			GameObjectUtility.SetNavMeshArea(piece, 1);
			modifier.overrideArea = true;
			modifier.area = 1;
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(piece.GetComponent<Collider>());
			modifier.ignoreFromBuild = true;
			piece.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
		}
	}

	static void ReplacePortal(ZonePortal _old, GameObject _prefab)
	{
		GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(_prefab, _old.transform.parent);
		root.name = _old.name;
		Vector3 position = _old.transform.position;
		position.y = Terrain.activeTerrain.SampleHeight(position) + Terrain.activeTerrain.transform.position.y;
		root.transform.position = position;
		root.transform.rotation = Quaternion.identity;
		ZonePortal portal = root.GetComponent<ZonePortal>();
		SerializedObject data = new SerializedObject(portal);
		data.FindProperty("m_portalId").ulongValue = _old.GetPortalId();
		data.FindProperty("m_active").boolValue = _old.IsActive();
		data.FindProperty("m_enterDistance").floatValue = 2.4f;
		data.FindProperty("m_targetZoneId").ulongValue = _old.GetTargetZoneId();
		data.FindProperty("m_targetEnterPosition").vector3Value = _old.GetTargetEnterPosition();
		data.ApplyModifiedPropertiesWithoutUndo();
		if (!_old.IsActive())
		{
			string path = Root + "Material/PortalInactive.mat";
			Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
			if (material == null)
			{
				material = new Material(Shader.Find("Standard"));
				material.color = new Color(.27f, .32f, .35f);
				AssetDatabase.CreateAsset(material, path);
			}
			Renderer renderer = root.transform.Find("PortalGlow").GetComponent<Renderer>();
			renderer.sharedMaterial = material;
			PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
		}
		PrefabUtility.RecordPrefabInstancePropertyModifications(portal);
		PrefabUtility.RecordPrefabInstancePropertyModifications(root.transform);
		if (root.name == "BeginnerZonePortal" || root.name == "VeteranZonePortal_Disabled")
			PrefabUtility.SaveAsPrefabAsset(root, Root + "Prefab/Object/Portal/" + root.name + ".prefab");
		UnityEngine.Object.DestroyImmediate(_old.gameObject);
	}

	static void RebuildBeginner()
	{
		Physics.SyncTransforms();
		NavMeshSurface[] surfaces = UnityEngine.Object.FindObjectsOfType<NavMeshSurface>();
		int rebuilt = 0;
		for (int i = 0; i < surfaces.Length; ++i)
		{
			if (!surfaces[i].isActiveAndEnabled) continue;
			++rebuilt;
			surfaces[i].BuildNavMesh();
			string path = Root + "Scenes/BeginnerZoneScene/NavMesh.asset";
			NavMeshData existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
			if (existing == null) AssetDatabase.CreateAsset(surfaces[i].navMeshData, path);
			else
			{
				EditorUtility.CopySerialized(surfaces[i].navMeshData, existing);
				surfaces[i].RemoveData();
				surfaces[i].navMeshData = existing;
				surfaces[i].AddData();
				EditorUtility.SetDirty(existing);
			}
			EditorUtility.SetDirty(surfaces[i]);
		}
		if (rebuilt != 1) throw new Exception("비기너존 활성 NavMeshSurface 확인 필요: " + rebuilt);
		UnityEngine.Object.FindObjectOfType<FieldDataExporter>(true).Export();
		EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
		AssetDatabase.SaveAssets();
	}

	public static void Verify()
	{
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		try
		{
			for (int i = 0; i < SceneNames.Length; ++i)
			{
				EditorSceneManager.OpenScene(Root + "Scenes/" + SceneNames[i] + ".unity");
				Physics.SyncTransforms();
				ZonePortal[] portals = UnityEngine.Object.FindObjectsOfType<ZonePortal>();
				int expected = 2;
				if (i == 1) expected = 1;
				Check(portals.Length == expected && GameObject.Find("VillagePortal") == null, "포탈 개수/중복");
				for (int j = 0; j < portals.Length; ++j) VerifyPortal(portals[j], i);
			}
			Debug.Log("[ZonePortal] ALL CHECKS PASSED");
		}
		finally { Restore(setup); }
	}

	static void VerifyPortal(ZonePortal _portal, int _sceneIndex)
	{
		Vector3 center = _portal.transform.position;
		Vector3 outside = center + Vector3.forward * 3;
		Vector3 inside = center + Vector3.forward * 2.2f;
		Check(!_portal.IsInRange(outside) && _portal.IsInRange(inside), "근접 반경");
		Check(_portal.IsInRange(inside + Vector3.up * 100), "XZ 판정");
		Check(!_portal.TryEnter(outside), "처음 등장 시 요청 안함");
		Check(_portal.TryEnter(inside) == _portal.IsActive(), "진입 시 요청/비활성 거부");
		for (int i = 0; i < 100; ++i) Check(!_portal.TryEnter(inside), "안에 머무를 때 중복 요청 안함");
		Check(!_portal.TryEnter(outside), "이탈 시 요청 안함");
		Check(_portal.TryEnter(inside) == _portal.IsActive(), "재진입 허용");
		_portal.enabled = false;
		_portal.enabled = true;
		Check(!_portal.TryEnter(inside) && !_portal.TryEnter(inside), "포탈 안에서 등장해도 왕복 안함");
		if (_portal.IsActive())
		{
			Vector3 spawn = new Vector3(27, 0, 80);
			if (_sceneIndex == 1) spawn = new Vector3(22, 0, 23);
			Check(!_portal.IsInRange(spawn), "도착 위치가 재진입 범위 밖");
			Check(NavMesh.SamplePosition(Ground(spawn), out NavMeshHit start, .5f, NavMesh.AllAreas), "도착 NavMesh");
			// 문 쪽으로 곧장 접근해도 범위에 닿을 수 있어야 함
			Vector3 approachDirection = spawn - center;
			approachDirection.y = 0;
			Vector3 approach = Ground(center + approachDirection.normalized * 2.1f);
			Check(NavMesh.SamplePosition(approach, out NavMeshHit entrance, .2f, NavMesh.AllAreas), "입장 방향 접근점 NavMesh");
			Check(_portal.IsInRange(entrance.position), "입장 방향 접근점 반경");
			NavMeshPath entrancePath = new NavMeshPath();
			Check(NavMesh.CalculatePath(start.position, entrance.position, NavMesh.AllAreas, entrancePath) && entrancePath.status == NavMeshPathStatus.PathComplete, "도착 지점에서 문 앞으로 접근");
			Debug.Log("[ZonePortal] FRONT PATH PASS " + _portal.name + " -> " + entrance.position);
			bool reachable = false;
			for (int i = 0; i < 32; ++i)
			{
				float angle = i * Mathf.PI * 2 / 32;
				Vector3 wanted = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2.1f;
				if (!NavMesh.SamplePosition(Ground(wanted), out NavMeshHit hit, .2f, NavMesh.AllAreas)) continue;
				if (!_portal.IsInRange(hit.position)) continue;
				NavMeshPath path = new NavMeshPath();
				if (!NavMesh.CalculatePath(start.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) continue;
				reachable = true;
				Debug.Log("[ZonePortal] PATH PASS " + _portal.name + " " + start.position + " -> " + hit.position);
				break;
			}
			Check(reachable, "포탈 진입 범위까지 경로 없음");
		}
		Capture(_portal, SceneNames[_sceneIndex] + "_" + _portal.name);
		Debug.Log("[ZonePortal] PASS " + _portal.name + " center=" + center + " radius=" + _portal.GetEnterDistance());
	}

	static Vector3 Ground(Vector3 _position)
	{
		_position.y = Terrain.activeTerrain.SampleHeight(_position) + Terrain.activeTerrain.transform.position.y;
		return _position;
	}

	static void Capture(ZonePortal _portal, string _name)
	{
		GameObject root = new GameObject("PortalVerificationCamera", typeof(Camera));
		Camera camera = root.GetComponent<Camera>();
		camera.transform.position = _portal.transform.position + new Vector3(0, 11, -11);
		camera.transform.rotation = Quaternion.Euler(45, 0, 0);
		camera.orthographic = true;
		camera.orthographicSize = 7;
		camera.cullingMask = ~(1 << 5);
		RenderTexture render = new RenderTexture(1920, 1080, 24);
		RenderTexture previous = RenderTexture.active;
		camera.targetTexture = render;
		camera.Render();
		RenderTexture.active = render;
		Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
		image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
		image.Apply();
		File.WriteAllBytes(Output + "/" + _name + ".png", image.EncodeToPNG());
		RenderTexture.active = previous;
		camera.targetTexture = null;
		UnityEngine.Object.DestroyImmediate(image);
		UnityEngine.Object.DestroyImmediate(render);
		UnityEngine.Object.DestroyImmediate(root);
	}

	static void Check(bool _condition, string _message)
	{
		if (!_condition) throw new Exception("[ZonePortal] " + _message);
	}

	static void Restore(SceneSetup[] _setup)
	{
		if (_setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(_setup);
		else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
	}

	public static void Probe()
	{
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		Directory.CreateDirectory(Output);
		StringBuilder report = new StringBuilder();
		try
		{
			for (int i = 0; i < SceneNames.Length; ++i)
			{
				EditorSceneManager.OpenScene(Root + "Scenes/" + SceneNames[i] + ".unity");
				ZonePortal[] portals = UnityEngine.Object.FindObjectsOfType<ZonePortal>(true);
				for (int j = 0; j < portals.Length; ++j)
				{
					ZonePortal portal = portals[j];
					report.AppendLine(SceneNames[i] + " " + portal.name + " position=" + portal.transform.position + " scale=" + portal.transform.lossyScale + " target=" + portal.GetTargetEnterPosition());
					Transform[] children = portal.GetComponentsInChildren<Transform>(true);
					for (int k = 0; k < children.Length; ++k)
					{
						Renderer renderer = children[k].GetComponent<Renderer>();
						report.AppendLine("  " + children[k].name + " local=" + children[k].localPosition + " size=" + children[k].lossyScale + " renderer=" + (renderer != null));
						if (renderer != null) report.AppendLine("    bounds=" + renderer.bounds);
					}
				}
				Transform[] objects = UnityEngine.Object.FindObjectsOfType<Transform>(true);
				for (int j = 0; j < objects.Length; ++j)
				{
					if (objects[j].name.IndexOf("Portal", StringComparison.OrdinalIgnoreCase) < 0) continue;
					report.AppendLine("PORTAL OBJECT " + objects[j].name + " " + objects[j].position + " " + objects[j].lossyScale);
					if (objects[j].name == "VillagePortal")
					{
						Component[] components = objects[j].GetComponentsInChildren<Component>(true);
						for (int k = 0; k < components.Length; ++k)
							if (components[k] != null) report.AppendLine("OLD " + components[k].name + " " + components[k].GetType().Name);
					}
				}
				Unity.AI.Navigation.NavMeshSurface[] surfaces = UnityEngine.Object.FindObjectsOfType<Unity.AI.Navigation.NavMeshSurface>(true);
				for (int j = 0; j < surfaces.Length; ++j)
					report.AppendLine("SURFACE " + surfaces[j].name + " active=" + surfaces[j].isActiveAndEnabled + " data=" + AssetDatabase.GetAssetPath(surfaces[j].navMeshData) + " collect=" + surfaces[j].collectObjects + " layer=" + surfaces[j].layerMask.value);
			}
			File.WriteAllText(Output + "/Probe.txt", report.ToString());
			Debug.Log("[ZonePortal] " + report);
		}
		finally
		{
			if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
			else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
		}
	}
}
