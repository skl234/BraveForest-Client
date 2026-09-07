using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class PointerSetup
{
    const string Root="Assets/MYFolder/";
    public static void Run()
    {
        Directory.CreateDirectory("Build/Validation/Backups");
        File.Copy(Root+"Scenes/BeginnerZoneScene.unity","Build/Validation/Backups/BeforePointer_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity");
        Texture2D normal=CursorTexture("Normal"), attack=CursorTexture("Attack");
        DestinationIndicator indicator=MakeIndicator();
        string[] names={"Warrior","Archer"};
        for(int i=0;i<names.Length;++i)
        {
            string path=Root+"Prefab/Object/Player/LocalPlayerObject_"+names[i]+".prefab";
            GameObject prefab=PrefabUtility.LoadPrefabContents(path);
            RPGCursor cursor=prefab.GetComponent<RPGCursor>(); if(cursor==null)cursor=prefab.AddComponent<RPGCursor>();
            Ref(cursor,"m_normal",normal);Ref(cursor,"m_attack",attack);
            SerializedObject so=new SerializedObject(cursor);
            so.FindProperty("m_normalHotspot").vector2Value=new Vector2(6,3);
            so.FindProperty("m_attackHotspot").vector2Value=new Vector2(32,32);so.ApplyModifiedPropertiesWithoutUndo();
            WorldPointerInput input=prefab.GetComponent<WorldPointerInput>();if(input==null)input=prefab.AddComponent<WorldPointerInput>();
            Ref(input,"m_cursor",cursor);Ref(input,"m_destinationPrefab",indicator);
            PrefabUtility.SaveAsPrefabAsset(prefab,path);PrefabUtility.UnloadPrefabContents(prefab);
        }
        string firePath=Root+"Prefab/Object/Effect/WarriorAreaFire.prefab";
        File.Copy(firePath,"Build/Validation/Backups/BeforePointerFire_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".prefab");
        GameObject fire=PrefabUtility.LoadPrefabContents(firePath);
        Transform floor=fire.transform.Find("FireGround");if(floor!=null)UnityEngine.Object.DestroyImmediate(floor.gameObject);
        PrefabUtility.SaveAsPrefabAsset(fire,firePath);PrefabUtility.UnloadPrefabContents(fire);
        string uiPath=Root+"Prefab/UI/CombatInterface.prefab";
        GameObject ui=PrefabUtility.LoadPrefabContents(uiPath);
        TMP_Text[] texts=ui.GetComponentsInChildren<TMP_Text>(true);
        for(int i=0;i<texts.Length;++i)if(texts[i].name=="Controls")texts[i].text="BEGINNER GROVE\n우클릭 유지 이동 · 몬스터 클릭 공격\n필드 좌클릭 타겟 해제 · A / S 스킬";
        PrefabUtility.SaveAsPrefabAsset(ui,uiPath);PrefabUtility.UnloadPrefabContents(ui);
        EditorSceneManager.OpenScene(Root+"Scenes/BeginnerZoneScene.unity",OpenSceneMode.Single);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();Debug.Log("[PointerSetup] COMPLETE");
    }
    static Texture2D CursorTexture(string name)
    {
        string path=Root+"Image/Cursor/"+name+".png";
        AssetDatabase.ImportAsset(path);
        TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=TextureImporterType.Cursor;importer.isReadable=true;importer.mipmapEnabled=false;
        importer.maxTextureSize=64;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
        Texture2D texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(texture.GetPixel(0,0).a>.1f)throw new Exception("Cursor alpha missing: "+name);
        Debug.Log("[PointerSetup] Cursor "+name+" "+texture.width+"px alpha="+texture.GetPixel(0,0).a);
        return texture;
    }
    static DestinationIndicator MakeIndicator()
    {
        string path=Root+"Prefab/Object/Effect/DestinationIndicator.prefab";
        GameObject root=new GameObject("DestinationIndicator");DestinationIndicator indicator=root.AddComponent<DestinationIndicator>();
        string materialPath=Root+"Prefab/Object/Effect/DestinationIndicator.mat";
        Material material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(material==null){material=new Material(Shader.Find("Sprites/Default"));material.color=new Color(.4f,1,.78f);AssetDatabase.CreateAsset(material,materialPath);}
        LineRenderer[] lines=new LineRenderer[2];
        for(int j=0;j<2;++j)
        {
            GameObject child=new GameObject(j==0?"PulseRing":"CenterDiamond");child.transform.SetParent(root.transform,false);
            LineRenderer line=child.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.sharedMaterial=material;line.widthMultiplier=.055f;
            line.positionCount=j==0?48:4;
            for(int i=0;i<line.positionCount;++i){float angle=i*Mathf.PI*2/line.positionCount;float radius=j==0?.55f:.19f;line.SetPosition(i,new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius));}
            lines[j]=line;
        }
        SerializedObject so=new SerializedObject(indicator);SerializedProperty array=so.FindProperty("m_lines");array.arraySize=lines.Length;
        for(int i=0;i<lines.Length;++i)array.GetArrayElementAtIndex(i).objectReferenceValue=lines[i];so.ApplyModifiedPropertiesWithoutUndo();
        GameObject prefab=PrefabUtility.SaveAsPrefabAsset(root,path);UnityEngine.Object.DestroyImmediate(root);return prefab.GetComponent<DestinationIndicator>();
    }
    static void Ref(UnityEngine.Object obj,string field,UnityEngine.Object value){SerializedObject so=new SerializedObject(obj);so.FindProperty(field).objectReferenceValue=value;so.ApplyModifiedPropertiesWithoutUndo();}
}
