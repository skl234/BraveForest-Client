using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine.AI;



enum eLoginPacket : ushort
{
    C2S_Login = 0,
    C2S_CreateAccount,

    S2C_Hellow,
    S2C_Login,
    S2C_CreateAccount,

    End,
}
public class LoginSceneClient : MonoBehaviour
{
    TcpClient m_client = null;
    public NetworkStream m_networkStream;
    Thread m_recvThread;
    CCircleQueue m_packetQueue;


    void Start()
    {
        m_client = new TcpClient();
        try
        {
            m_client.Connect(IPAddress.Parse("127.0.0.1"), 30002);
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
        m_packetQueue = new CCircleQueue(65535);
        m_recvThread = new Thread(RecvThreadFunc);
        m_recvThread.Start();
    }

    // Update is called once per frame
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

            eLoginPacket type = (eLoginPacket)BitConverter.ToUInt16(packet, 2);
            switch (type)
            {
                case eLoginPacket.S2C_Hellow:
                    S2C_Hellow();
                    break;

                case eLoginPacket.S2C_Login:
                    S2C_Login(packet);
                    break;

                case eLoginPacket.S2C_CreateAccount:
                    S2C_CreateAccount(packet);
                    break;

                default:
                    break;
            }
        }
    }
    void S2C_Hellow()
    {
        Debug.Log("CompleteConnect 받음");
    }
    void S2C_Login(byte[] _packet)
    {
        int packetIndex = 4;
        bool result = BitConverter.ToBoolean(_packet, packetIndex);

        if (result) Debug.Log("로그인 성공");
        else if (!result) Debug.Log("로그인 실패");
        //else
        //{
        //    Debug.Log("로그인 성공");
        //    MemoryStream stream = new MemoryStream(100);
        //    stream.Write(BitConverter.GetBytes((short)4));
        //    stream.Write(BitConverter.GetBytes((short)eLoginPacket.C2S_CharactorList));
        //    m_networkStream.Write(stream.GetBuffer(), 0, (int)stream.Position);
        //}
    }
    void S2C_CreateAccount(byte[] _packet)
    {
        int packetIndex = 4;

        int listSize = BitConverter.ToInt32(_packet, packetIndex); packetIndex += sizeof(int);
        Debug.Log("캐릭터 수 : " + listSize);

        for(int i = 0; i < listSize; ++i)
        {
            int nameBuffSize = BitConverter.ToInt32(_packet, packetIndex); packetIndex += sizeof(int);
            string name = Encoding.Unicode.GetString(_packet, packetIndex, nameBuffSize); packetIndex += nameBuffSize;
            Int64 Class = BitConverter.ToInt64(_packet, packetIndex); packetIndex += sizeof(Int64);

            Debug.Log("이름 : " + name);
            if (Class == 0) Debug.Log("직업 : 전사");
            else Debug.Log("직업 : 마법사");
        }
    }
    void RecvThreadFunc()
    {
        byte[] byteArray = new byte[65535];
        int readSize;
        while (true)
        {
            readSize = m_networkStream.Read(byteArray);
            if (readSize == 0)
            {
                Debug.Log("Recv Error");
                break;
            }
            while (true)
            {
                if (m_packetQueue.Push(byteArray, readSize) > 0) break;
            }
        }
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
        if (m_client != null)
        {
            m_client.Close();
            //client가 살아있다면 관련 종료 작업
        }
        Debug.Log("애플리케이션 종료됨");
    }
}
