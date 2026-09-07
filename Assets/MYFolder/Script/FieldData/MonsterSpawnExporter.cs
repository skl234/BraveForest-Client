using System.Globalization;
using System.IO;
using UnityEngine;

public class MonsterSpawnExporter : MonoBehaviour
{
    [SerializeField] Transform m_spawnRoot;
	[SerializeField] Terrain m_terrain;
    [SerializeField] string m_fileName = "MonsterSpawnData.txt";
	[SerializeField] ulong m_zoneId = 2;

	public ulong GetZoneId() { return m_zoneId; }

	public bool IsValidZone(ulong _zoneId)
	{
		if (_zoneId == 0 || _zoneId != m_zoneId) return false;
		if (!gameObject.scene.IsValid()) return false;
		GameObject[] roots = gameObject.scene.GetRootGameObjects();
		ZoneSceneInfo zoneInfo = null;
		for (int i = 0; i < roots.Length; ++i)
		{
			ZoneSceneInfo[] list = roots[i].GetComponentsInChildren<ZoneSceneInfo>(true);
			for (int j = 0; j < list.Length; ++j)
			{
				if (zoneInfo != null) return false;
				zoneInfo = list[j];
			}
		}
		return zoneInfo != null && zoneInfo.GetZoneId() == m_zoneId;
	}

	public void SetExportInfo(ulong _zoneId, Terrain _terrain)
	{
		m_zoneId = _zoneId;
		m_terrain = _terrain;
	}

	public bool Validate()
	{
		if (!IsValidZone(m_zoneId))
		{
			Debug.LogError("[MonsterSpawnExporter] 씬과 내보내기의 ZoneId를 동일하게 설정해야 합니다.");
			return false;
		}

		MonsterSpawnPoint[] list = GetSpawnPointList();
		if (list.Length > 512) return false;
		Vector3 origin = Vector3.zero;
		if (m_terrain != null) origin = m_terrain.transform.position;
		for (int i = 0; i < list.Length; ++i)
		{
			Vector3 position = list[i].transform.position - origin;
			if (list[i].IsValid() && position.x >= 0.0f && position.z >= 0.0f &&
				!float.IsInfinity(position.x) && !float.IsInfinity(position.z)) continue;
			Debug.LogError("[MonsterSpawnExporter] 올바르지 않은 스폰 : " + list[i].name);
			return false;
		}
		return true;
	}

    public Transform GetSpawnRoot()
    {
        if (m_spawnRoot != null) return m_spawnRoot;
        return transform;
    }

    public MonsterSpawnPoint[] GetSpawnPointList()
    {
        return GetSpawnRoot().GetComponentsInChildren<MonsterSpawnPoint>(true);
    }

    [ContextMenu("Export Monster Spawn Data")]
    public void Export()
    {
		// 검증이 끝나기 전에는 기존 내보내기 파일을 건드리지 않음
		if (!Validate()) return;
        string directory = Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "Zone" + m_zoneId + "_" + m_fileName);

        using (StreamWriter writer = new StreamWriter(path, false))
        {
            Vector3 origin = Vector3.zero;
			if (m_terrain != null) origin = m_terrain.transform.position;
			WriteDetail(writer, origin);
        }

        Debug.Log("[MonsterSpawnExporter] " + path);
		ExportSpawnFile(directory);
		ExportSQL(directory);
    }

	void ExportSpawnFile(string _directory)
	{
		string fileName = gameObject.scene.name.Replace("Scene", "") + "Spawn.txt";
		string path = Path.Combine(_directory, fileName);
		MonsterSpawnPoint[] list = GetSpawnPointList();
		Vector3 origin = Vector3.zero;
		if (m_terrain != null) origin = m_terrain.transform.position;
		using (StreamWriter writer = new StreamWriter(path, false))
		{
			writer.WriteLine("ZoneId " + m_zoneId);
			writer.WriteLine("SpawnCount " + list.Length);
			for (int i = 0; i < list.Length; ++i)
			{
				Vector3 position = list[i].transform.position - origin;
				writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "Spawn {0} {1} {2:F3} {3:F3} {4:F3}",
					i, (ushort)list[i].GetMonsterType(), position.x, position.z, list[i].transform.eulerAngles.y));
			}
		}
		Debug.Log("[MonsterSpawnExporter] FILE " + path);
	}

	void ExportSQL(string _directory)
	{
		string path = Path.Combine(_directory, "MonsterTypes_Zone" + m_zoneId + ".sql");
		MonsterSpawnPoint[] list = GetSpawnPointList();
		using (StreamWriter writer = new StreamWriter(path, false))
		{
			writer.WriteLine("-- ZoneId " + m_zoneId + " / " + gameObject.scene.name);
			writer.WriteLine("-- Unity에서 생성한 SQL. DB에 적용한 뒤 해당 ZoneServer 재시작");
			writer.WriteLine("USE game_db;");
			writer.WriteLine("START TRANSACTION;");
			bool[] writtenType = new bool[4];
			for (int i = 0; i < list.Length; ++i)
			{
				MonsterSpawnPoint spawnPoint = list[i];

				int type = (int)spawnPoint.GetMonsterType();
				if (writtenType[type]) continue;
				writtenType[type] = true;
				string monsterName = "Monster";
				if (spawnPoint.GetMonsterType() == eMonsterType.Ghost) monsterName = "Ghost";
				if (spawnPoint.GetMonsterType() == eMonsterType.Lizard) monsterName = "Lizard";
				if (spawnPoint.GetMonsterType() == eMonsterType.Knight) monsterName = "Knight";
				if (spawnPoint.GetMonsterType() == eMonsterType.Rat) monsterName = "Rat";
				writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
					"INSERT INTO MonsterTypes (monsterType, name, level, maxHP, experience, attackPower, aggressive, detectionRange, attackRange, moveSpeed, attackIntervalMs, wanderRadius, maxAggroRange, respawnRadius, respawnDelayMs, canWander, canChase, canAttack, idleMinTimeMs, idleMaxTimeMs, wanderMinTimeMs, wanderMaxTimeMs) VALUES ({0}, '{1}', {2}, {3}, {4}, {5}, {6}, {7:F3}, {8:F3}, {9:F3}, {10}, {11:F3}, {12:F3}, {13:F3}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}) ON DUPLICATE KEY UPDATE name=VALUES(name), level=VALUES(level), maxHP=VALUES(maxHP), experience=VALUES(experience), attackPower=VALUES(attackPower), aggressive=VALUES(aggressive), detectionRange=VALUES(detectionRange), attackRange=VALUES(attackRange), moveSpeed=VALUES(moveSpeed), attackIntervalMs=VALUES(attackIntervalMs), wanderRadius=VALUES(wanderRadius), maxAggroRange=VALUES(maxAggroRange), respawnRadius=VALUES(respawnRadius), respawnDelayMs=VALUES(respawnDelayMs), canWander=VALUES(canWander), canChase=VALUES(canChase), canAttack=VALUES(canAttack), idleMinTimeMs=VALUES(idleMinTimeMs), idleMaxTimeMs=VALUES(idleMaxTimeMs), wanderMinTimeMs=VALUES(wanderMinTimeMs), wanderMaxTimeMs=VALUES(wanderMaxTimeMs);",
					type, monsterName, spawnPoint.GetLevel(), spawnPoint.GetMaxHP(), spawnPoint.GetExperience(), spawnPoint.GetAttackPower(), spawnPoint.IsAggressive() ? 1 : 0,
					spawnPoint.GetDetectionRange(), spawnPoint.GetAttackRange(), spawnPoint.GetMoveSpeed(), spawnPoint.GetAttackIntervalMs(),
					spawnPoint.GetWanderRadius(), spawnPoint.GetMaxAggroRange(), spawnPoint.GetRespawnRadius(), (uint)(spawnPoint.GetRespawnDelay() * 1000.0f),
					spawnPoint.CanWander() ? 1 : 0, spawnPoint.CanChase() ? 1 : 0, spawnPoint.CanAttack() ? 1 : 0,
					(uint)(spawnPoint.GetIdleMinTime() * 1000.0f), (uint)(spawnPoint.GetIdleMaxTime() * 1000.0f),
					(uint)(spawnPoint.GetWanderMinTime() * 1000.0f), (uint)(spawnPoint.GetWanderMaxTime() * 1000.0f)));
			}
			writer.WriteLine("COMMIT;");
		}
		Debug.Log("[MonsterSpawnExporter] SQL " + path);
	}

    public bool Write(StreamWriter _writer, Vector3 _fieldOrigin)
    {
        MonsterSpawnPoint[] spawnPointList = GetSpawnPointList();
        _writer.WriteLine("MonsterCount " + spawnPointList.Length);

        for (int i = 0; i < spawnPointList.Length; ++i)
        {
            MonsterSpawnPoint spawnPoint = spawnPointList[i];
            if (!spawnPoint.IsValid()) return false;

            Vector3 position = spawnPoint.transform.position - _fieldOrigin;
            if (position.x < 0.0f || position.z < 0.0f) return false;

            _writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "Monster {0} {1:F3} {2:F3} {3:F3} {4:F3} {5:F3} {6:F3}",
                (ushort)spawnPoint.GetMonsterType(),
                position.x,
                position.z,
                spawnPoint.GetWanderRadius(),
                spawnPoint.GetMaxAggroRange(),
                spawnPoint.GetRespawnRadius(),
                spawnPoint.GetRespawnDelay()));
        }
        return true;
    }

    public bool WriteDetail(StreamWriter _writer, Vector3 _fieldOrigin)
    {
        MonsterSpawnPoint[] spawnPointList = GetSpawnPointList();
        _writer.WriteLine("MonsterSpawnCount " + spawnPointList.Length);

        for (int i = 0; i < spawnPointList.Length; ++i)
        {
            MonsterSpawnPoint spawnPoint = spawnPointList[i];
            if (!spawnPoint.IsValid()) return false;

            Vector3 position = spawnPoint.transform.position - _fieldOrigin;
            if (position.x < 0.0f || position.z < 0.0f) return false;

            _writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
				"MonsterSpawn {0} {1:F3} {2:F3} {3:F3} {4} {5} {6} {7:F3} {8:F3} {9:F3} {10:F3} {11:F3} {12} {13} {14:F3} {15:F3} {16:F3} {17}",
                (ushort)spawnPoint.GetMonsterType(),
                position.x,
                position.z,
                spawnPoint.transform.eulerAngles.y,
                spawnPoint.GetLevel(),
                spawnPoint.GetMaxHP(),
                spawnPoint.GetExperience(),
                spawnPoint.GetHitboxRadius(),
                spawnPoint.GetWanderRadius(),
                spawnPoint.GetMaxAggroRange(),
                spawnPoint.GetRespawnRadius(),
                spawnPoint.GetRespawnDelay(),
				spawnPoint.IsInvincible() ? 1 : 0,
				spawnPoint.IsAggressive() ? 1 : 0,
				spawnPoint.GetDetectionRange(),
				spawnPoint.GetAttackRange(),
				spawnPoint.GetMoveSpeed(),
				spawnPoint.GetAttackIntervalMs()));
        }
        return true;
    }
}
