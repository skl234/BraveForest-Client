using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class MonsterInfoCreater : MonoBehaviour
{
	

	// Start is called before the first frame update
	void Start()
	{
		SaveMonsterList(Path.Combine(Application.dataPath, "MonsterInfo.bin"));
	}

	// Update is called once per frame
	void Update()
	{
		
	}

	void SaveMonsterList(string _path)
	{
		BinaryWriter stream = new BinaryWriter(File.Open(_path, FileMode.Create));

		stream.Write(transform.childCount);
		foreach (Transform obj in transform)
		{
			MonsterCreateInfo mci = obj.GetComponent<MonsterCreateInfo>();

			stream.Write((ushort)mci.m_type);
			stream.Write(mci.m_aggressive);
			stream.Write(mci.m_maxHP);
			stream.Write(mci.m_attackPower);
			stream.Write(mci.m_attackCoolTime);
			stream.Write(mci.m_attackRange);
			stream.Write(mci.m_moveSpeed);
			stream.Write(mci.m_hitboxRadius);
			//stream.Write(obj.transform.position.x);
			//stream.Write(obj.transform.position.y);
			//stream.Write(obj.transform.position.z);
			//stream.Write(obj.transform.rotation.x);
			//stream.Write(obj.transform.rotation.y);
			//stream.Write(obj.transform.rotation.z);
			//stream.Write(obj.transform.rotation.w);
		}

		stream.Dispose();

		Debug.Log("몬스터정보 저장완료");
	}
}
