using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Field : MonoBehaviour
{
    Dictionary<int, GameObject> m_playerList  = new Dictionary<int, GameObject>();
    Dictionary<int, GameObject>    m_monsterList = new Dictionary<int, GameObject>();

    private void Awake()
    {
        
    }
    public bool HasPlayer(int _id)
    {
        if (m_playerList.ContainsKey(_id)) return true;
        return false;
    }
    public void EnterPlayer(GameObject _player)
    {
        int playerID = _player.GetComponent<Player>().GetID();
        if (m_playerList.ContainsKey(playerID)) return;
        m_playerList.Add(playerID, _player);
    }
    public void LeavePlayer(int _id)
    {
        if (!m_playerList.ContainsKey(_id)) return;
        GameObject playerObj = m_playerList[_id];
        m_playerList.Remove(_id);
        Destroy(playerObj);
    }
    public List<int> GetPlayerIDList()
    {
        List<int> list = new List<int>();
        foreach (KeyValuePair<int, GameObject> kv in m_playerList)
        {
            list.Add(kv.Key);
        }
        return list;
    }
    public bool HasMonster(int _num)
    {
        if (m_monsterList.ContainsKey(_num)) return true;
        return false;
    }
    public void EnterMonster(GameObject _monster)
    {
        int monsterNum = _monster.GetComponent<Monster>().GetNumber();
        if (m_monsterList.ContainsKey(monsterNum)) return;
        m_monsterList.Add(monsterNum, _monster);
    }
    public void LeaveMonster(int _num)
    {
        if (!m_monsterList.ContainsKey(_num)) return;
        GameObject monsterObj = m_monsterList[_num];
        m_monsterList.Remove(_num);
        Destroy(monsterObj);
    }
    public List<int> GetMonsterNumList()
    {
        List<int> list = new List<int>();
        foreach (KeyValuePair<int, GameObject> kv in m_monsterList)
        {
            list.Add(kv.Key);
        }
        return list;
    }
    public Dictionary<int, GameObject> GetPlayerList()
    {
        return m_playerList;
    }
    public Dictionary<int, GameObject> GetMonsterList()
    {
        return m_monsterList;
    }
}
