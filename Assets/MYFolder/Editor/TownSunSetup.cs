using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TownSunSetup
{
	public const string PrefabPath = "Assets/MYFolder/Prefab/Environment/ZoneSun.prefab";
	const string BeginnerPath = "Assets/MYFolder/Scenes/BeginnerZoneScene.unity";
	const string TownPath = "Assets/MYFolder/Scenes/TownScene.unity";
	const string OutputPath = "Build/Validation/TownSun";

	[MenuItem("MYFolder/Environment/비기너존 태양을 마을에 적용")]
	public static void Apply()
	{
		if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
		SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
		Directory.CreateDirectory(OutputPath);
		File.Copy(TownPath, OutputPath + "/TownScene_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity");
		try
		{
			EditorSceneManager.OpenScene(BeginnerPath);
			Light source = FindSun();
			if (source == null || source.shadows == LightShadows.None) throw new Exception("비기너존의 그림자용 태양을 확인해야 합니다.");
			Quaternion rotation = source.transform.rotation;
			Color ambientSky = RenderSettings.ambientSkyColor;
			Color ambientEquator = RenderSettings.ambientEquatorColor;
			Color ambientGround = RenderSettings.ambientGroundColor;
			Debug.Log("[TownSun] Beginner rotation=" + source.transform.eulerAngles.ToString("F3") + " shadows=" + source.shadows + " intensity=" + source.intensity + " strength=" + source.shadowStrength);

			// 비기너존은 수정하지 않고 조명 설정만 공용 프리팹으로 복사
			Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
			AssetDatabase.Refresh();
			GameObject temporary = new GameObject("ZoneSun", typeof(Light));
			EditorUtility.CopySerialized(source, temporary.GetComponent<Light>());
			temporary.transform.rotation = rotation;
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temporary, PrefabPath);
			UnityEngine.Object.DestroyImmediate(temporary);
			if (prefab == null) throw new Exception("태양 프리팹 저장 실패");

			Scene town = EditorSceneManager.OpenScene(TownPath);
			Light sun = FindSun();
			Capture(OutputPath + "/Before.png");
			if (sun == null)
			{
				GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, town);
				sun = instance.GetComponent<Light>();
			}
			else
			{
				EditorUtility.CopySerialized(prefab.GetComponent<Light>(), sun);
				sun.transform.rotation = rotation;
				sun.gameObject.name = "ZoneSun";
				PrefabUtility.SaveAsPrefabAssetAndConnect(sun.gameObject, PrefabPath, InteractionMode.AutomatedAction);
			}
			RenderSettings.sun = sun;
			// 베이크되지 않은 마을에서도 그림자 속이 검게 뭉개지지 않도록 환경광 지정
			RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
			RenderSettings.ambientSkyColor = ambientSky;
			RenderSettings.ambientEquatorColor = ambientEquator;
			RenderSettings.ambientGroundColor = ambientGround;
			// 맵 외곽 Terrain 절단면의 사각 테두리 그림자는 제외. 건물/나무 그림자는 그대로 유지
			if (Terrain.activeTerrain != null) Terrain.activeTerrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			EditorSceneManager.MarkSceneDirty(town);
			if (!EditorSceneManager.SaveScene(town)) throw new Exception("마을 씬 저장 실패");

			// 저장된 씬을 다시 열어서 연결/설정을 검증
			EditorSceneManager.OpenScene(TownPath);
			sun = FindSun();
			if (sun == null || RenderSettings.sun != sun || Quaternion.Angle(rotation, sun.transform.rotation) > 0.01f || sun.shadows == LightShadows.None)
				throw new Exception("마을 태양 검증 실패");
			Capture(OutputPath + "/After.png");
			LightShadows shadows = sun.shadows;
			sun.shadows = LightShadows.None;
			Capture(OutputPath + "/WithoutShadows.png");
			sun.shadows = shadows;
			Debug.Log("[TownSun] PASS rotation=" + sun.transform.eulerAngles.ToString("F3") + " shadows=" + sun.shadows + " prefab=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sun) + " quality=" + QualitySettings.names[QualitySettings.GetQualityLevel()] + " shadowDistance=" + QualitySettings.shadowDistance);
		}
		finally
		{
			if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
			else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
		}
	}

	static Light FindSun()
	{
		if (RenderSettings.sun != null && RenderSettings.sun.type == LightType.Directional) return RenderSettings.sun;
		Light[] lights = UnityEngine.Object.FindObjectsOfType<Light>();
		Light sun = null;
		for (int i = 0; i < lights.Length; ++i)
		{
			if (lights[i].type != LightType.Directional || !lights[i].enabled) continue;
			if (sun != null) throw new Exception("태양이 여러 개입니다. Lighting의 Sun Source를 지정해야 합니다.");
			sun = lights[i];
		}
		return sun;
	}

	static void Capture(string _path)
	{
		GameObject root = new GameObject("TownSunVerificationCamera", typeof(Camera));
		Camera camera = root.GetComponent<Camera>();
		camera.transform.position = new Vector3(50, 38, -3);
		camera.transform.rotation = Quaternion.Euler(45, 0, 0);
		camera.orthographic = true;
		camera.orthographicSize = 32;
		camera.farClipPlane = 500;
		camera.cullingMask = ~(1 << 5);
		RenderTexture target = new RenderTexture(1920, 1080, 24);
		RenderTexture previous = RenderTexture.active;
		Texture2D image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
		try
		{
			camera.targetTexture = target;
			camera.Render();
			RenderTexture.active = target;
			image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
			image.Apply();
			File.WriteAllBytes(_path, image.EncodeToPNG());
		}
		finally
		{
			RenderTexture.active = previous;
			camera.targetTexture = null;
			UnityEngine.Object.DestroyImmediate(image);
			UnityEngine.Object.DestroyImmediate(target);
			UnityEngine.Object.DestroyImmediate(root);
		}
	}
}
