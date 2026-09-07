using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ZoneFieldDataVerification
{
    const string Root = "Assets/MYFolder/";
    const string ScenePath = Root + "Scenes/BeginnerZoneScene.unity";

    public static void Run()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        VerifyScene();
        VerifyFieldFile();
        VerifySpawnFile();
        VerifyToolPrefab();
        Debug.Log("[ZoneFieldDataVerification] ALL PASSED");
    }

    static void VerifyScene()
    {
        GameObject field = GameObject.Find("Field");
        Check(field != null, "Field missing");
        Transform spawnRoot = field.transform.Find("MonsterSpawnList");
        Check(spawnRoot != null, "MonsterSpawnList missing");
        MonsterSpawnPoint[] spawnPointList = spawnRoot.GetComponentsInChildren<MonsterSpawnPoint>(true);
        Check(spawnPointList.Length == 4, "spawn count must be 4");

        bool[] typeList = new bool[4];
        for (int i = 0; i < spawnPointList.Length; ++i)
        {
            MonsterSpawnPoint spawnPoint = spawnPointList[i];
            int type = (int)spawnPoint.GetMonsterType();
            Check(type >= 0 && type < typeList.Length, "invalid monster type");
            Check(!typeList[type], "duplicated monster type");
            typeList[type] = true;
            Check(spawnPoint.IsValid(), "invalid spawn values: " + spawnPoint.name);
            Check(spawnPoint.IsInvincible(), "dummy monster must be invincible");

            MonsterObject monster = spawnPoint.GetComponent<MonsterObject>();
            Check(monster != null, "MonsterObject missing");
            Check(monster.GetState() == eMonsterState.Idle, "dummy monster must be Idle");
            MonsterWander wander = spawnPoint.GetComponent<MonsterWander>();
            Check(wander == null || !wander.enabled, "dummy monster movement must be disabled");
            MonsterHitbox hitbox = spawnPoint.GetComponentInChildren<MonsterHitbox>(true);
            Check(hitbox != null, "MonsterHitbox missing");
            Check(Mathf.Abs(hitbox.GetComponent<SphereCollider>().radius - spawnPoint.GetHitboxRadius()) < 0.001f,
                "hitbox radius mismatch");
        }

        Transform tools = field.transform.Find("FieldDataTools");
        Check(tools != null, "FieldDataTools missing");
        Check(tools.GetComponent<FieldDataExporter>() != null, "FieldDataExporter missing");
        Check(tools.GetComponent<MonsterSpawnExporter>() != null, "MonsterSpawnExporter missing");
    }

    static void VerifyFieldFile()
    {
        string path = Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData", "BeginnerZoneField.txt");
        string[] lineList = File.ReadAllLines(path);
        Check(lineList.Length > 9, "field file is empty");

        uint widthX = ReadHeader(lineList[0], "WidthX");
        uint widthZ = ReadHeader(lineList[1], "WidthZ");
        uint rows = ReadHeader(lineList[2], "CellRows");
        uint cols = ReadHeader(lineList[3], "CellCols");
        uint sectorWidth = ReadHeader(lineList[4], "SectorWidth");
        uint sectorRows = ReadHeader(lineList[5], "SectorRows");
        uint sectorCols = ReadHeader(lineList[6], "SectorCols");
        uint blockCount = ReadHeader(lineList[7], "BlockCount");
        Check(rows == widthX && cols == widthZ, "Cell size must be exactly 1");
        Check(sectorWidth > 0, "SectorWidth must be positive");
        Check(sectorRows == (widthX + sectorWidth - 1) / sectorWidth, "SectorRows mismatch");
        Check(sectorCols == (widthZ + sectorWidth - 1) / sectorWidth, "SectorCols mismatch");

        HashSet<uint> blockList = new HashSet<uint>();
        for (uint i = 0; i < blockCount; ++i)
        {
            uint index = uint.Parse(lineList[8 + i], CultureInfo.InvariantCulture);
            Check(index < rows * cols, "block index out of range");
            Check(blockList.Add(index), "duplicated block index");
        }

        int monsterHeader = 8 + (int)blockCount;
        Check(ReadHeader(lineList[monsterHeader], "MonsterCount") == 4, "field monster count mismatch");
        Check(lineList.Length == monsterHeader + 5, "field file trailing or missing line");
    }

    static void VerifySpawnFile()
    {
        string path = Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData", "MonsterSpawnData.txt");
        string[] lineList = File.ReadAllLines(path);
        Check(ReadHeader(lineList[0], "MonsterSpawnCount") == 4, "detail spawn count mismatch");
        Check(lineList.Length == 5, "detail spawn line count mismatch");
        for (int i = 1; i < lineList.Length; ++i)
        {
            string[] valueList = lineList[i].Split(' ');
            Check(valueList.Length == 14, "detail spawn value count mismatch");
            Check(valueList[0] == "MonsterSpawn", "detail spawn label mismatch");
            Check(ushort.Parse(valueList[1], CultureInfo.InvariantCulture) == i - 1, "monster enum order mismatch");
            Check(uint.Parse(valueList[5], CultureInfo.InvariantCulture) > 0, "monster level must be positive");
            Check(uint.Parse(valueList[6], CultureInfo.InvariantCulture) > 0, "monster HP must be positive");
            Check(valueList[13] == "1", "dummy monster must be invincible");
        }
    }

    static void VerifyToolPrefab()
    {
        Check(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/FieldData/FieldDataExporter.prefab") != null,
            "FieldDataExporter prefab missing");
        Check(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "Prefab/FieldData/MonsterSpawnExporter.prefab") != null,
            "MonsterSpawnExporter prefab missing");
    }

    static uint ReadHeader(string _line, string _name)
    {
        string[] valueList = _line.Split(' ');
        Check(valueList.Length == 2 && valueList[0] == _name, "invalid header: " + _name);
        return uint.Parse(valueList[1], CultureInfo.InvariantCulture);
    }

    static void Check(bool _result, string _message)
    {
        if (!_result) throw new Exception("[ZoneFieldDataVerification] " + _message);
    }
}
