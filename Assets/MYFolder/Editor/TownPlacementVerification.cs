using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class TownPlacementVerification
{
	public static void Run()
	{
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		try
		{
			EditorSceneManager.OpenScene("Assets/MYFolder/Scenes/TownScene.unity");
			Physics.SyncTransforms();
			string directory = "Build/Validation/TownPlacement";
			Directory.CreateDirectory(directory);
			string[] lines = File.ReadAllLines("Assets/MYFolder/ExportedFieldData/TownField.txt");
			int rows = int.Parse(lines[2].Split(' ')[1]);
			int cols = int.Parse(lines[3].Split(' ')[1]);
			int count = int.Parse(lines[7].Split(' ')[1]);
			HashSet<int> blocks = new HashSet<int>();
			for (int i = 0; i < count; ++i) blocks.Add(int.Parse(lines[8 + i]));
			GameObject environment = GameObject.Find("TownEnvironment");
			if (environment == null) throw new Exception("TownEnvironment missing");
			Terrain terrain = Terrain.activeTerrain;
			StringBuilder report = new StringBuilder("Name\tPosition\tGround\tBaseGap\tCenterBlocked\tCenterOnNavMesh\tSolidSamples\tWalkableSolidSamples\n");
			for (int i = 0; i < environment.transform.childCount; ++i)
			{
				Transform item = environment.transform.GetChild(i);
				Vector3 position = item.position;
				float ground = terrain.SampleHeight(position) + terrain.transform.position.y;
				Renderer[] renderers = item.GetComponentsInChildren<Renderer>();
				float bottom = float.MaxValue;
				for (int j = 0; j < renderers.Length; ++j) bottom = Mathf.Min(bottom, renderers[j].bounds.min.y);
				int index = Mathf.FloorToInt(position.x) * cols + Mathf.FloorToInt(position.z);
				bool onNav = NavMesh.SamplePosition(new Vector3(position.x, ground, position.z), out NavMeshHit hit, 0.25f, NavMesh.AllAreas);
				int samples = 0;
				int walkable = 0;
				Collider[] colliders = item.GetComponentsInChildren<Collider>();
				for (int j = 0; j < colliders.Length; ++j)
				{
					Collider collider = colliders[j];
					if (!collider.enabled || collider.isTrigger) continue;
					Bounds bounds = collider.bounds;
					for (float x = bounds.min.x + 0.08f; x < bounds.max.x; x += 0.25f)
					{
						for (float z = bounds.min.z + 0.08f; z < bounds.max.z; z += 0.25f)
						{
							if (x < 0 || z < 0 || x >= rows || z >= cols) continue;
							Vector3 point = new Vector3(x, 0, z);
							point.y = terrain.SampleHeight(point) + terrain.transform.position.y + 0.2f;
							if ((collider.ClosestPoint(point) - point).sqrMagnitude > 0.0001f) continue;
							++samples;
							if (!blocks.Contains(Mathf.FloorToInt(x) * cols + Mathf.FloorToInt(z))) ++walkable;
						}
					}
				}
				report.AppendLine(item.name + "\t" + position.ToString("F2") + "\t" + ground.ToString("F2") + "\t" + (bottom - ground).ToString("F2") + "\t" + blocks.Contains(index) + "\t" + onNav + "\t" + samples + "\t" + walkable);
			}
			File.WriteAllText(directory + "/Audit.tsv", report.ToString());
			Capture(directory + "/Overview.png", new Vector3(50, 85, -30), new Vector3(50, 0, 55), 70);
			Capture(directory + "/Road.png", new Vector3(50, 38, -3), new Vector3(50, 0, 35), 32);
			Debug.Log("[TownPlacement] AUDIT " + directory + " No scene edits");
		}
		finally
		{
			if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
			else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
		}
	}

	static void Capture(string _path, Vector3 _position, Vector3 _target, float _size)
	{
		GameObject root = new GameObject("AuditCamera");
		Camera camera = root.AddComponent<Camera>();
		camera.transform.position = _position;
		camera.transform.LookAt(_target);
		camera.orthographic = true;
		camera.orthographicSize = _size;
		camera.farClipPlane = 500;
		RenderTexture target = new RenderTexture(1920, 1080, 24);
		camera.targetTexture = target;
		camera.Render();
		RenderTexture previous = RenderTexture.active;
		RenderTexture.active = target;
		Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
		image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
		image.Apply();
		File.WriteAllBytes(_path, image.EncodeToPNG());
		RenderTexture.active = previous;
		camera.targetTexture = null;
		UnityEngine.Object.DestroyImmediate(image);
		UnityEngine.Object.DestroyImmediate(target);
		UnityEngine.Object.DestroyImmediate(root);
	}
}
