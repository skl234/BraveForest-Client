using System.Globalization;
using System.IO;
using UnityEngine;

public class MonsterSpawnExporter : MonoBehaviour
{
	[SerializeField] Transform m_spawnRoot;
	[SerializeField] Terrain m_terrain;
	[SerializeField] string m_fileName = "MonsterSpawnData.txt";
	[SerializeField] ZoneSceneInfo m_sceneInfo;

	public ulong GetZoneId()
	{
		if (m_sceneInfo == null) return 0;
		return m_sceneInfo.GetZoneId();
	}

	public ZoneSceneInfo GetSceneInfo() { return m_sceneInfo; }
	public Terrain GetTerrain() { return m_terrain; }
	public string GetSpawnFileName() { return gameObject.scene.name.Replace("Scene", "") + "Spawn.txt"; }

	public bool IsValidZone(ulong _zoneId)
	{
		return _zoneId != 0 && m_sceneInfo != null && m_sceneInfo == ZoneSceneInfo.Find(gameObject.scene) && GetZoneId() == _zoneId;
	}

	public void Initialize(ZoneSceneInfo _sceneInfo, Terrain _terrain)
	{
		m_sceneInfo = _sceneInfo;
		m_terrain = _terrain;
	}

	public bool Validate()
	{
		if (!IsValidZone(GetZoneId()) || m_terrain == null || m_terrain.terrainData == null || m_terrain.gameObject.scene != gameObject.scene)
		{
			Debug.LogError("[MonsterSpawnExporter] 씬과 내보내기의 ZoneId를 동일하게 설정해야 합니다.");
			return false;
		}

		MonsterSpawnPoint[] list = GetSpawnPointList();
		if (list.Length > 512 || GetSpawnRoot().gameObject.scene != gameObject.scene) return false;
		Vector2 size = m_sceneInfo.GetFieldSize();
		Vector3 origin = Vector3.zero;
		if (m_terrain != null) origin = m_terrain.transform.position;
		for (int i = 0; i < list.Length; ++i)
		{
			Vector3 position = list[i].transform.position - origin;
			if (list[i].IsValid() && position.x >= 0.0f && position.z >= 0.0f &&
				position.x < size.x && position.z < size.y && !float.IsInfinity(position.x) && !float.IsInfinity(position.z)) continue;
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
		Export(Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData"));
	}

	public void Export(string _directory)
	{
		if (!Validate()) throw new System.InvalidOperationException(name + " 스폰 설정을 확인하세요.");
		Directory.CreateDirectory(_directory);
		ExportSpawnFile(_directory);
	}

	[ContextMenu("Export Monster Type SQL")]
	public void ExportSQL()
	{
		if (!Validate()) throw new System.InvalidOperationException(name + " 스폰 설정을 확인하세요.");
		string directory = Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData");
		Directory.CreateDirectory(directory);
		ExportSQL(directory);
	}

	[ContextMenu("Export Monster Spawn Detail")]
	public void ExportDetail()
	{
		if (!Validate()) throw new System.InvalidOperationException(name + " 스폰 설정을 확인하세요.");
		string directory = Path.Combine(Application.dataPath, "MYFolder", "ExportedFieldData");
		Directory.CreateDirectory(directory);
		using (StreamWriter writer = new StreamWriter(Path.Combine(directory, "Zone" + GetZoneId() + "_" + m_fileName), false))
		{
			if (!WriteDetail(writer, m_terrain.transform.position)) throw new System.InvalidOperationException("스폰 상세 정보 오류");
		}
	}

	void ExportSpawnFile(string _directory)
	{
		string fileName = GetSpawnFileName();
		string path = Path.Combine(_directory, fileName);
		MonsterSpawnPoint[] list = GetSpawnPointList();
		Vector3 origin = Vector3.zero;
		if (m_terrain != null) origin = m_terrain.transform.position;
		using (StreamWriter writer = new StreamWriter(path, false))
		{
			writer.WriteLine("ZoneId " + GetZoneId());
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
		string path = Path.Combine(_directory, "MonsterTypes_Zone" + GetZoneId() + ".sql");
		MonsterSpawnPoint[] list = GetSpawnPointList();
		using (StreamWriter writer = new StreamWriter(path, false))
		{
			writer.WriteLine("-- ZoneId " + GetZoneId() + " / " + gameObject.scene.name);
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
				if (spawnPoint.GetMonsterType() == eMonsterType.BlueMushroom) monsterName = "BlueMushroom";
				if (spawnPoint.GetMonsterType() == eMonsterType.Knight) monsterName = "Knight";
				if (spawnPoint.GetMonsterType() == eMonsterType.Slime) monsterName = "Slime";
				writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
					"INSERT INTO MonsterTypes (monsterType, name, level, maxHP, experience, attackPower, aggressive, detectionRange, attackRange, moveSpeed, attackIntervalMs, wanderRadius, maxAggroRange, respawnRadius, respawnDelayMs, canWander, canChase, canAttack, idleMinTimeMs, idleMaxTimeMs, wanderMinTimeMs, wanderMaxTimeMs) VALUES ({0}, '{1}', {2}, {3}, {4}, {5}, {6}, {7:F3}, {8:F3}, {9:F3}, {10}, {11:F3}, {12:F3}, {13:F3}, {14}, {15}, {16}, {17}, {18}, {19}, {20}, {21}) ON DUPLICATE KEY UPDATE name=VALUES(name), level=VALUES(level), maxHP=VALUES(maxHP), experience=VALUES(experience), attackPower=VALUES(attackPower), aggressive=VALUES(aggressive), detectionRange=VALUES(detectionRange), attackRange=VALUES(attackRange), moveSpeed=VALUES(moveSpeed), attackIntervalMs=VALUES(attackIntervalMs), wanderRadius=VALUES(wanderRadius), maxAggroRange=VALUES(maxAggroRange), respawnRadius=VALUES(respawnRadius), respawnDelayMs=VALUES(respawnDelayMs), canWander=VALUES(canWander), canChase=VALUES(canChase), canAttack=VALUES(canAttack), idleMinTimeMs=VALUES(idleMinTimeMs), idleMaxTimeMs=VALUES(idleMaxTimeMs), wanderMinTimeMs=VALUES(wanderMinTimeMs), wanderMaxTimeMs=VALUES(wanderMaxTimeMs);",
					type, monsterName, spawnPoint.GetLevel(), spawnPoint.GetMaxHP(), spawnPoint.GetExperience(), spawnPoint.GetAttackPower(), System.Convert.ToInt32(spawnPoint.IsAggressive()),
					spawnPoint.GetDetectionRange(), spawnPoint.GetAttackRange(), spawnPoint.GetMoveSpeed(), spawnPoint.GetAttackIntervalMs(),
					spawnPoint.GetWanderRadius(), spawnPoint.GetMaxAggroRange(), spawnPoint.GetRespawnRadius(), (uint)(spawnPoint.GetRespawnDelay() * 1000.0f),
					System.Convert.ToInt32(spawnPoint.CanWander()), System.Convert.ToInt32(spawnPoint.CanChase()), System.Convert.ToInt32(spawnPoint.CanAttack()),
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
				System.Convert.ToInt32(spawnPoint.IsInvincible()),
				System.Convert.ToInt32(spawnPoint.IsAggressive()),
				spawnPoint.GetDetectionRange(),
				spawnPoint.GetAttackRange(),
				spawnPoint.GetMoveSpeed(),
				spawnPoint.GetAttackIntervalMs()));
		}
		return true;
	}
}
