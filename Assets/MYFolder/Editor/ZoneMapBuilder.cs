using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// 현재 저장된 씬으로 지도 이미지만 갱신. 지형이나 오브젝트 배치는 변경하지 않음
public static class ZoneMapBuilder
{
	const string Root = "Assets/MYFolder/";
	const string BeginnerScene = Root + "Scenes/BeginnerZoneScene.unity";
	const string TownScene = Root + "Scenes/TownScene.unity";

	public static void RefreshTownMap()
	{
		RenderMap(TownScene, Root + "Image/Map/TownMap.png", new Vector3(50, 100, 50), 50);
		AssetDatabase.ImportAsset(Root + "Image/Map/TownMap.png", ImportAssetOptions.ForceSynchronousImport);
	}

	public static void RefreshBeginnerMap()
	{
		EditorSceneManager.OpenScene(BeginnerScene);
		ZoneSceneInfo info = UnityEngine.Object.FindObjectOfType<ZoneSceneInfo>();
		Vector2 min = info.GetWorldMin();
		Vector2 max = info.GetWorldMax();
		float size = (max.x - min.x) * .5f;
		RenderMap(BeginnerScene, Root + "Image/Map/BeginnerZoneMap.png", new Vector3((min.x + max.x) * .5f, 240, (min.y + max.y) * .5f), size);
		AssetDatabase.ImportAsset(Root + "Image/Map/BeginnerZoneMap.png", ImportAssetOptions.ForceSynchronousImport);
	}

	static void RenderMap(string scenePath,string outputPath,Vector3 position,float size)
	{
		Scene scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Single); GameObject cameraObject=new GameObject("MapRenderCamera",typeof(Camera)); Camera camera=cameraObject.GetComponent<Camera>(); camera.transform.position=position; camera.transform.rotation=Quaternion.Euler(90,0,0); camera.orthographic=true; camera.orthographicSize=size; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=new Color(.05f,.08f,.06f); camera.cullingMask=~(1<<5);
		LODGroup[] groups = UnityEngine.Object.FindObjectsOfType<LODGroup>();
		for (int i = 0; i < groups.Length; ++i) groups[i].ForceLOD(0);
		RenderTexture render=new RenderTexture(1024,1024,24); camera.targetTexture=render; camera.Render(); RenderTexture.active=render; Texture2D texture=new Texture2D(1024,1024,TextureFormat.RGB24,false); texture.ReadPixels(new Rect(0,0,1024,1024),0,0); texture.Apply(); ApplyNavigationOverlay(texture,position,size); File.WriteAllBytes(outputPath,texture.EncodeToPNG()); RenderTexture.active=null; camera.targetTexture=null; UnityEngine.Object.DestroyImmediate(render); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(cameraObject);
		for (int i = 0; i < groups.Length; ++i) groups[i].ForceLOD(-1);
	}

	static void ApplyNavigationOverlay(Texture2D _texture,Vector3 _center,float _size)
	{
		int cellCount=Mathf.RoundToInt(_size*2.0f);
		bool[,] walkable=new bool[cellCount,cellCount];
		float minX=_center.x-_size; float minZ=_center.z-_size;
		for(int z=0;z<cellCount;++z) for(int x=0;x<cellCount;++x)
		{
			Vector3 point=new Vector3(minX+x+.5f,0.0f,minZ+z+.5f);
			NavMeshHit hit;
			walkable[x,z]=NavMesh.SamplePosition(point,out hit,1.0f,NavMesh.AllAreas);
		}
		// 파일이 있으면 서버와 같은 Block 셀을 지도에도 사용
		string fieldPath = Root + "ExportedFieldData/" + SceneManager.GetActiveScene().name.Replace("Scene", "") + "Field.txt";
		if (File.Exists(fieldPath))
		{
			string[] lines = File.ReadAllLines(fieldPath);
			int rows = int.Parse(lines[2].Split(' ')[1]);
			int cols = int.Parse(lines[3].Split(' ')[1]);
			if (rows == cellCount && cols == cellCount)
			{
				for (int x = 0; x < cellCount; ++x) for (int z = 0; z < cellCount; ++z) walkable[x,z] = true;
				int count = int.Parse(lines[7].Split(' ')[1]);
				for (int i = 0; i < count; ++i)
				{
					int index = int.Parse(lines[8+i]);
					walkable[index/cols,index%cols] = false;
				}
			}
		}
		// 지도 표현만 부드럽게. 서버용 Block 목록은 수정하지 않음
		float[,] mask = new float[cellCount,cellCount];
		int[] weights = { 1, 2, 1 };
		for (int z = 0; z < cellCount; ++z) for (int x = 0; x < cellCount; ++x)
		{
			float blocked = 0;
			for (int dz = -1; dz <= 1; ++dz) for (int dx = -1; dx <= 1; ++dx)
			{
				int sampleX = Mathf.Clamp(x + dx, 0, cellCount - 1);
				int sampleZ = Mathf.Clamp(z + dz, 0, cellCount - 1);
				if (!walkable[sampleX,sampleZ]) blocked += weights[dx+1] * weights[dz+1];
			}
			mask[x,z] = blocked / 16.0f;
		}
		Color[] pixels=_texture.GetPixels();
		for(int y=0;y<_texture.height;++y) for(int x=0;x<_texture.width;++x)
		{
			float cellX = Mathf.Clamp((x + .5f) * cellCount / _texture.width - .5f, 0, cellCount - 1);
			float cellZ = Mathf.Clamp((y + .5f) * cellCount / _texture.height - .5f, 0, cellCount - 1);
			int left = Mathf.FloorToInt(cellX);
			int bottom = Mathf.FloorToInt(cellZ);
			int right = Mathf.Min(left + 1, cellCount - 1);
			int top = Mathf.Min(bottom + 1, cellCount - 1);
			float lower = Mathf.Lerp(mask[left,bottom], mask[right,bottom], cellX - left);
			float upper = Mathf.Lerp(mask[left,top], mask[right,top], cellX - left);
			float blocked = Mathf.Lerp(lower, upper, cellZ - bottom);
			int index=y*_texture.width+x; Color color=pixels[index];
			color = Color.Lerp(color, new Color(.78f,.86f,.62f,1), .06f * (1 - blocked));
			color = Color.Lerp(color, new Color(.025f,.08f,.055f,1), .38f * blocked);
			pixels[index]=color;
		}
		_texture.SetPixels(pixels); _texture.Apply();
	}

}
