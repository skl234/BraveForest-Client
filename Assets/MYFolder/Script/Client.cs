using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine.AI;
enum eUserType : byte
{
    Player = 0,
    Dummy,
}
enum PacketType : ushort
{
    C2S_UserNumber = 0,
    C2S_EnterField,
    C2S_ObjectList,
    C2S_TransForm,
    C2S_DestPosition,
    C2S_UserState,
    C2S_Attack,

    S2C_ServerFull,
    S2C_Connection,
    S2C_UserNumber,
    S2C_EnterField,
    S2C_ObjectList,
    S2C_ObjectEnter,
    S2C_ObjectLeave,
    S2C_DestPosition,
    S2C_MonsterPath,
    S2C_UserState,
    S2C_MonsterHP,
    S2C_MonsterDamage,
    S2C_MonsterState,
    S2C_MonsterTransform,
    S2C_UserTakeDamage,

    C2C_Latency,
}
enum eObjectType : byte
{
    User = 0,
    Monster,
}
public class Client : MonoBehaviour
{
    TcpClient m_client;
    NetworkStream m_networkStream;
    Thread m_recvThread;
    CCircleQueue m_packetQueue;
    int m_number ;
    Vector3 m_startPosition = new Vector3(40, 0, 40);

    //Dictionary<ushort, GameObject> m_playerList = new Dictionary<ushort, GameObject>();
    //Dictionary<int, GameObject> m_monsterList = new Dictionary<int, GameObject>();

    public GameObject m_localPlayerPrefab;
    public GameObject m_remotePlayerPrefab;
    public GameObject m_userCount;
    public GameObject m_monsterFactoryObj;
    private MonsterFactory m_monsterFactory;
    

    private Field m_field;

    void Start()
    {
        m_client = new TcpClient();
        try
        {
            m_client.Connect(IPAddress.Parse("125.186.182.181"), 30002);
            Debug.Log("Connect 성공");
        }
        catch (SocketException ex)
        {
            Debug.Log("접속 실패 : " + ex.Message + ", 에러코드 : " + ex.SocketErrorCode);
            m_client.Close();
            m_client = null;
            QuitApp();
        }
        catch (Exception ex)
        {
            Debug.Log("접속 실패 : " + ex.Message);
            m_client.Close();
            m_client = null;
            QuitApp();
        }

        if (m_client == null) return;
        
        m_networkStream = m_client.GetStream();
        m_recvThread = new Thread(RecvThreadFunc);
        m_recvThread.Start();
        m_packetQueue = new CCircleQueue(65535);

        m_field = GameObject.Find("Field").GetComponent<Field>();
        m_monsterFactory = m_monsterFactoryObj.GetComponent<MonsterFactory>();
    }
    void Update()
    {
        byte[] packet;
        ushort packetSize = 0;
        while (true)
        {
            if (!m_packetQueue.GetWord(ref packetSize)) break;
            if (m_packetQueue.GetSize() < packetSize) break;

            packet = m_packetQueue.Pop(packetSize);
            if (packet == null) 
            {
                Debug.Log("recvQueue Error");
                QuitApp();
                break;
            }

            PacketType type = (PacketType)BitConverter.ToUInt16(packet, 2);
            switch (type)
            {
                case PacketType.S2C_Connection:
                    OnRecvConnection();
                    break;

                case PacketType.S2C_ServerFull:
                    OnRecvServerFull();
                    break;

                case PacketType.S2C_UserNumber:
                    OnRecvUserNumber(packet);
                    break;

                case PacketType.S2C_EnterField:
                    OnRecvEnter();
                    break;

                case PacketType.S2C_ObjectList:
                    OnRecvObjectList(packet);
                    break;

                case PacketType.S2C_ObjectEnter:
                    OnRecvObjectEnter(packet);
                    break;

                case PacketType.S2C_ObjectLeave:
                    OnRecvLeaveObject(packet);
                    break;

                case PacketType.S2C_DestPosition:
                    OnRecvDestposition(packet);
                    break;

                case PacketType.S2C_MonsterPath:
                    OnRecvMonsterPath(packet);
                    break;

                case PacketType.S2C_UserState:
                    OnRecvUserState(packet);
                    break;

                case PacketType.S2C_MonsterHP:
                    OnRecvMonsterHP(packet);
                    break;

                case PacketType.S2C_MonsterDamage:
                    OnRecvMonsterDamage(packet);
                    break;

                case PacketType.S2C_MonsterState:
                    OnRecvMonsterState(packet);
                    break;

                case PacketType.S2C_MonsterTransform:
                    OnRecvMonsterTransform(packet);
                    break;

                case PacketType.S2C_UserTakeDamage:
                    OnRecvPlayerTakeDamage(packet);
                    break;

                default:
                    break;
            }
        }

        //m_userCount.GetComponent<TMP_Text>().text = "User Count : " + m_playerList.Count.ToString();
    }
    void QuitApp()
    {
        #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    void OnApplicationQuit()
    {
        if(m_client != null)
        {
            m_client.Close();
            //client가 살아있다면 관련 종료 작업
        }
        Debug.Log("애플리케이션 종료됨");
    }
    void OnRecvConnection()
    {
        Debug.Log("Connection 성공");

        MemoryStream ms = new MemoryStream(4);
        ms.Write(BitConverter.GetBytes((ushort)4));
        ms.Write(BitConverter.GetBytes((ushort)(PacketType.C2S_UserNumber)));
        m_networkStream.Write(ms.GetBuffer(), 0, 4);
    }
    void OnRecvServerFull()
    {
        Debug.Log("서버 가득참");
        QuitApp();
    }
    void OnRecvUserNumber(byte[] _packet)
    {   
        m_number = BitConverter.ToInt32(_packet, 4);
        Debug.Log("내 번호 : " + m_number);

        RequestFieldEnter();
    }
    void OnRecvEnter()
    {
        GameObject playerObject = null;

        //Debug.Log("enter받음");

        NavMeshHit hit;
        if (NavMesh.SamplePosition(m_startPosition, out hit, 5.0f, NavMesh.AllAreas))
        {
            // NavMesh 위에 가까운 점을 찾았으면 그 위치로 생성
            playerObject = Instantiate(m_localPlayerPrefab, hit.position, Quaternion.identity);
        }
        else return;

        //Debug.Log("enter받음");

        playerObject.GetComponent<LocalPlayer>().SetID(m_number);
        //playerObject.GetComponent<LocalPlayer>().SetPosition(m_startPosition);
        playerObject.GetComponent<LocalPlayer>().SetNetworkStream(m_networkStream);
        playerObject.GetComponent<LocalPlayer>().SetHP(100);

        m_field.EnterPlayer(playerObject);

        RequestFieldObjectList();
    }
    void OnRecvObjectList(byte[] _packet)
    {
        List<int> removePlayerNumberList = m_field.GetPlayerIDList();
        List<int> removeMonsterNumberList = m_field.GetMonsterNumList();

        ushort packetIndex = 4;
        while(packetIndex < _packet.Length)
        {
            eObjectType objectType = (eObjectType)_packet[packetIndex]; ++packetIndex;
            if (objectType == eObjectType.User)
            {
                int num = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
                ePlayerState state = (ePlayerState)_packet[packetIndex++];
                float x = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float y = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float z = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

                float rotationX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float rotationY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float rotationZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float rotationW = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

                float desX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float desY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float desZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

                int maxHP = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
                int hp = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;

                removePlayerNumberList.Remove(num);
                if (m_field.HasPlayer(num)) continue;

                GameObject playerObject = Instantiate(m_remotePlayerPrefab);
                playerObject.GetComponent<RemotePlayer>().SetID(num);
                playerObject.GetComponent<RemotePlayer>().SetState(state);
                playerObject.GetComponent<RemotePlayer>().SetPosition(new Vector3(x, y, z));
                playerObject.GetComponent<RemotePlayer>().SetDestPosition(new Vector3(desX, desY, desZ));
                playerObject.GetComponent<RemotePlayer>().SetRotation(new Quaternion(rotationX, rotationY, rotationZ, rotationW));
                playerObject.GetComponent<RemotePlayer>().SetHP(hp);
                m_field.EnterPlayer(playerObject);
            }
            else if (objectType == eObjectType.Monster)
            {
                int number = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
                eMonsterType type = (eMonsterType)BitConverter.ToInt16(_packet, packetIndex); packetIndex += 2;
                int maxHP = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
                int hp = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
                float moveSpeed = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float hitBoxRadius = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                eMonsterState state = (eMonsterState)BitConverter.ToInt16(_packet, packetIndex); packetIndex += 2;
                float x = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float y = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float z = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float rx = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float ry = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float rz = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float rw = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

                List<Vector3> path = new List<Vector3>();
                int pathSize = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
                for (int i = 0; i < pathSize; ++i)
                {
                    float pathX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                    float pathY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                    float pathZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                    path.Add(new Vector3(pathX, pathY, pathZ));
                }

                removeMonsterNumberList.Remove(number);
                if (m_field.HasMonster(number)) continue;

                GameObject monsterObj = m_monsterFactory.CreateMonster(type);
                monsterObj.GetComponent<Monster>().Initialize(number, type, state, maxHP, hp,
                    moveSpeed, hitBoxRadius, new Vector3(x, y, z), new Quaternion(rx, ry, rz, rw), path);
                m_field.EnterMonster(monsterObj);

                Debug.Log(state);
                Debug.Log(pathSize);
                Debug.Log(path);
            }
        }

        foreach (int num in removePlayerNumberList)
        {
            m_field.LeavePlayer(num); Debug.Log("유저 지워줌");
        }
        foreach (int num in removeMonsterNumberList)
        {
            m_field.LeaveMonster(num);
        }
    }
    void OnRecvObjectEnter(byte[] _packet)
    {
        ushort packetIndex = 4;
        eObjectType objectType = (eObjectType)_packet[packetIndex]; ++packetIndex;
        if (objectType == eObjectType.User)
        {
            int num = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
            ePlayerState state = (ePlayerState)_packet[packetIndex++];
            float x = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float y = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float z = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

            float rotationX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float rotationY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float rotationZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float rotationW = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

            float desX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float desY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float desZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

            int maxHP = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
            int hp = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;

            if (m_field.HasPlayer(num)) { Debug.Log("이미 있는 유저 Enter"); return; }

            GameObject playerObject = Instantiate(m_remotePlayerPrefab);
            playerObject.GetComponent<RemotePlayer>().SetID(num);
            playerObject.GetComponent<RemotePlayer>().SetState(state);
            playerObject.GetComponent<RemotePlayer>().SetPosition(new Vector3(x, y, z));
            playerObject.GetComponent<RemotePlayer>().SetDestPosition(new Vector3(desX, desY, desZ));
            playerObject.GetComponent<RemotePlayer>().SetRotation(new Quaternion(rotationX, rotationY, rotationZ, rotationW));
            playerObject.GetComponent<RemotePlayer>().SetHP(hp);
            m_field.EnterPlayer(playerObject);

            Debug.Log("받은 유저Enter 유저번호 : " + num);
        }
        else if(objectType == eObjectType.Monster)
        {
            int number = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
            eMonsterType type = (eMonsterType)BitConverter.ToInt16(_packet, packetIndex); packetIndex += 2;
            int maxHP = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
            int hp = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
            float moveSpeed = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float hitBoxRadius = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            eMonsterState state = (eMonsterState)BitConverter.ToInt16(_packet, packetIndex); packetIndex += 2;
            float x = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float y = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float z = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float rx = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float ry = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float rz = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float rw = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

            List<Vector3> path = new List<Vector3>();
            int pathSize = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
            for (int i = 0; i < pathSize; ++i)
            {
                float pathX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float pathY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                float pathZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
                path.Add(new Vector3(pathX, pathY, pathZ));
            }

            if (m_field.HasMonster(number)) return;

            GameObject monsterObj = m_monsterFactory.CreateMonster(type);
            monsterObj.GetComponent<Monster>().Initialize(number, type, state, maxHP, hp,
                moveSpeed, hitBoxRadius, new Vector3(x, y, z), new Quaternion(rx, ry, rz, rw), path);
            m_field.EnterMonster(monsterObj);
        }
    }
    void OnRecvLeaveObject(byte[] _packet)
    {
        eObjectType type = (eObjectType)_packet[4];
        int number = BitConverter.ToInt32(_packet, 5);

        if(type == eObjectType.User) m_field.LeavePlayer(number);
        if(type == eObjectType.Monster) m_field.LeaveMonster(number);
    }
    void OnRecvDestposition(byte[] _packet)
    {
        ushort packetIndex = 4;
        int id = BitConverter.ToUInt16(_packet, packetIndex); packetIndex += 4;
        float destX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float destY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float destZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

        float currentX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float currentY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float currentZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

        if (!m_field.HasPlayer(id)) return;

        Vector3 desPos = new Vector3(destX, destY, destZ);
        Vector3 currentPos = new Vector3(currentX, currentY, currentZ);
        m_field.GetPlayerList()[id].GetComponent<RemotePlayer>().SetDestPosition(desPos, currentPos);
    }
    void OnRecvMonsterPath(byte[] _packet)
    {
        ushort packetIndex = 4;
        int index = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
        int pathSize = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
        List<Vector3> path = new List<Vector3>();
        for (int i = 0; i < pathSize; ++i)
        {
            float pathX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float pathY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            float pathZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
            path.Add(new Vector3(pathX, pathY, pathZ));
        }

        if (!m_field.HasMonster(index)) return;

        Debug.Log("Path 받음");
        Monster monster = m_field.GetMonsterList()[index].GetComponent<Monster>();
        monster.SetPath(path);
    }
    void OnRecvUserState(byte[] _packet)
    {
        ushort packetIndex = 4;
        ushort number = BitConverter.ToUInt16(_packet, packetIndex); packetIndex += 4;
        ePlayerState state = (ePlayerState)_packet[packetIndex]; packetIndex += 1;

        float x = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float y = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float z = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

        float rotationX = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rotationY = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rotationZ = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rotationW = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

        if (!m_field.HasPlayer(number)) return;
        RemotePlayer player = m_field.GetPlayerList()[number].GetComponent<RemotePlayer>();
        player.SetState(state);
        //player.SetPosition(new Vector3(x, y, z));
        player.SetRotation(new Quaternion(rotationX, rotationY, rotationZ, rotationW));
    }
    void OnRecvMonsterHP(byte[] _packet)
    {
        int num = BitConverter.ToInt32(_packet, 4);
        int hp = BitConverter.ToInt32(_packet, 8);

        if (!m_field.HasMonster(num)) return;
        m_field.GetMonsterList()[num].GetComponent<Monster>().SetHP(hp);
    }
    void OnRecvMonsterDamage(byte[] _packet)
    {
        int num = BitConverter.ToInt32(_packet, 4);
        int hp = BitConverter.ToInt32(_packet, 8);
        int damage = BitConverter.ToInt32(_packet, 12);

        if (!m_field.HasMonster(num)) return;
        m_field.GetMonsterList()[num].GetComponent<Monster>().SetHP(hp);
        m_field.GetMonsterList()[num].GetComponent<Monster>().OnHit(damage);
    }
    void OnRecvMonsterState(byte[] _packet)
    {
        ushort packetIndex = 4;
        int num = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
        eMonsterState state = (eMonsterState)BitConverter.ToInt16(_packet, packetIndex); packetIndex += 2;
        float x = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float y = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float z = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

        float rx = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float ry = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rz = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rw = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

        if (!m_field.HasMonster(num)) return;
        Monster monster = m_field.GetMonsterList()[num].GetComponent<Monster>();
        monster.SetState(state);
        if(state == eMonsterState.Attack) monster.SetRotation(new Quaternion(rx, ry, rz, rw));

        //Debug.Log("몬스터 State 받음 : " + state);
    }
    void OnRecvMonsterTransform(byte[] _packet)
    {
        ushort packetIndex = 4;
        int num = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
        float x = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float y = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float z = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rx = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float ry = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rz = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;
        float rw = BitConverter.ToSingle(_packet, packetIndex); packetIndex += 4;

        if (!m_field.HasMonster(num)) return;
        Monster monster = m_field.GetMonsterList()[num].GetComponent<Monster>();
        monster.SetPosition(new Vector3(x, y, z));
        monster.SetRotation(new Quaternion(rx, ry, rz, rw));
    }
    void OnRecvPlayerTakeDamage(byte[] _packet)
    {
        ushort packetIndex = 4;
        int num = BitConverter.ToUInt16(_packet, packetIndex); packetIndex += 4;
        int hp = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;
        int damage = BitConverter.ToInt32(_packet, packetIndex); packetIndex += 4;

        if (!m_field.HasPlayer(num)) return;

        Player player = m_field.GetPlayerList()[num].GetComponent<Player>();
        player.SetHP(hp);
        player.OnHit(damage);
    }
    private void RequestFieldEnter()
    {
        MemoryStream ms = new MemoryStream(20);
        ms.Write(BitConverter.GetBytes((ushort)20));
        ms.Write(BitConverter.GetBytes((ushort)(PacketType.C2S_EnterField)));
        ms.Write(BitConverter.GetBytes(m_startPosition.x));
        ms.Write(BitConverter.GetBytes(m_startPosition.y));
        ms.Write(BitConverter.GetBytes(m_startPosition.z));
        ms.Write(BitConverter.GetBytes(0));
        m_networkStream.Write(ms.GetBuffer(), 0, 20);
    }
    private void RequestFieldObjectList()
    {
        MemoryStream ms = new MemoryStream(4);
        ms.Write(BitConverter.GetBytes((ushort)4));
        ms.Write(BitConverter.GetBytes((ushort)(PacketType.C2S_ObjectList)));
        m_networkStream.Write(ms.GetBuffer(), 0, 4);
    }
    void RecvThreadFunc()
    {
        byte[] byteArray = new byte[65535];
        int readSize;
        while (true)
        {
            readSize = m_networkStream.Read(byteArray);
            if(readSize == 0)
            {
                Debug.Log("Recv Error");
                break;
            }
            while(true)
            {
                if (m_packetQueue.Push(byteArray, readSize) > 0) break;
            }
        }
    }
}
