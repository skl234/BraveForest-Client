using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ReleaseSourceVerification
{
	public static void Run()
	{
		const string output = "Build/Validation/Current";
		Directory.CreateDirectory(output);
		List<string> scenes = new List<string>();
		for (int i = 0; i < EditorBuildSettings.scenes.Length; ++i)
		{
			if (EditorBuildSettings.scenes[i].enabled) scenes.Add(EditorBuildSettings.scenes[i].path);
		}
		if (scenes.Count != 3 || !scenes[0].EndsWith("LoginScene.unity")) throw new Exception("Unexpected build scene list.");
		string[] dependencies = AssetDatabase.GetDependencies(scenes.ToArray(), true);
		File.WriteAllLines(output + "/SceneDependencies.txt", dependencies);
		StringBuilder report = new StringBuilder();
		for (int i = 0; i < scenes.Count; ++i)
		{
			Scene scene = EditorSceneManager.OpenScene(scenes[i], OpenSceneMode.Single);
			GameObject[] roots = scene.GetRootGameObjects();
			int missing = 0;
			for (int j = 0; j < roots.Length; ++j)
			{
				Transform[] objects = roots[j].GetComponentsInChildren<Transform>(true);
				for (int k = 0; k < objects.Length; ++k) missing += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objects[k].gameObject);
			}
			if (missing != 0) throw new Exception(scene.name + " missing scripts: " + missing);
			ZoneSceneInfo info = UnityEngine.Object.FindObjectOfType<ZoneSceneInfo>(true);
			if (info == null)
			{
				report.AppendLine(scene.name + " | login | missing scripts=0");
				continue;
			}
			if (info.GetMapImage() == null) throw new Exception(scene.name + " map texture missing.");
			string imagePath = AssetDatabase.GetAssetPath(info.GetMapImage());
			report.AppendLine(scene.name + " | zone=" + info.GetZoneId() + " | name=" + info.GetZoneName() + " | map=" + imagePath + " | min=" + info.GetWorldMin() + " | max=" + info.GetWorldMax() + " | missing scripts=0");
			File.Copy(imagePath, output + "/" + scene.name + "_Map.png", true);
			Capture(info, output + "/" + scene.name + "_Overview.png");
		}
		File.WriteAllText(output + "/SceneReport.txt", report.ToString());
		Debug.Log("[ReleaseSourceVerification] PASS: 3 build scenes, no missing scripts, current maps and dependencies recorded.");
	}

	static void Capture(ZoneSceneInfo _info, string _path)
	{
		Vector2 min = _info.GetWorldMin();
		Vector2 max = _info.GetWorldMax();
		Vector3 center = new Vector3((min.x + max.x) * .5f, 0, (min.y + max.y) * .5f);
		float width = Mathf.Max(max.x - min.x, max.y - min.y);
		GameObject root = new GameObject("MapOverviewCamera", typeof(Camera));
		Camera camera = root.GetComponent<Camera>();
		camera.transform.position = center + new Vector3(0, width, -width);
		camera.transform.rotation = Quaternion.Euler(45, 0, 0);
		camera.orthographic = true;
		camera.orthographicSize = width * .48f;
		camera.farClipPlane = width * 5;
		camera.cullingMask = ~(1 << 5);
		RenderTexture target = new RenderTexture(1920, 1080, 24);
		camera.targetTexture = target;
		RenderTexture old = RenderTexture.active;
		Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
		try
		{
			camera.Render();
			RenderTexture.active = target;
			image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
			image.Apply();
			File.WriteAllBytes(_path, image.EncodeToPNG());
		}
		finally
		{
			RenderTexture.active = old;
			camera.targetTexture = null;
			UnityEngine.Object.DestroyImmediate(image);
			UnityEngine.Object.DestroyImmediate(target);
			UnityEngine.Object.DestroyImmediate(root);
		}
	}
}
