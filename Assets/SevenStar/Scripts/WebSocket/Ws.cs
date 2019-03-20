using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Ws : MonoBehaviour
{
    public static Ws Instance;

    public static WebSocket ws;

    private Queue<string> sendQueue;

    int queueCn;
    
    private void Awake()
    {
        Instance = this;
//        ws=new WebSocket(new Uri("ws://211.238.13.182:18080"));
        ws=new WebSocket(new Uri("ws://localhost:13300"));
        sendQueue=new Queue<string>();
        queueCn = 0;
    }
    
    private IEnumerator Start()
    {
        yield return StartCoroutine(ws.Connect());
        Debug.Log("connected");
        string[] strArr = new string[2] { "t1", "a" };
        byte[] data = ClientBase.StringArrayToByte(strArr);
        Send(Protocols.Login, data);
        while (true)
        {
            byte[] reply0 = ws.Recv();
            if (reply0 != null)
            {
                Receive(reply0);
            }
            if (ws.error != null)
            {
                Debug.LogError("Error: " + ws.error);
                break;
            }
            yield return 0;
        }
        Debug.Log("close");
        ws.Close();
    }

    private int m_PacketNumber = 13;
    public bool Send(Protocols protocol, byte[] data)
    {
        try
        {
            int p = (int)protocol;
            int len = data.Length + 12;
            int num = m_PacketNumber;
            byte[] SendData = new byte[len];
            m_PacketNumber++;

            Array.Copy(BitConverter.GetBytes(len), 0, SendData, 0, 4);
            Array.Copy(BitConverter.GetBytes(num), 0, SendData, 4, 4);
            Array.Copy(BitConverter.GetBytes(p), 0, SendData, 8, 4);

            Array.Copy(data, 0, SendData, 12, data.Length);

            ws.Send(SendData);
            return true;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
        return false;
    }

    public void Send(string res)
    {
//        reply = null;
        Debug.Log("Send:"+queueCn+":"+res);
        if (queueCn == 0)
        {
            ws.SendString(res);
        }
        else
        {
            sendQueue.Enqueue(res);
        }
        queueCn++;
    }
    public void Receive(byte[] res)
    {
        queueCn--;
        int len = BitConverter.ToInt32(res,0);
        int length = len - 12;
        int protocol = BitConverter.ToInt32(res, 8);
        Debug.Log("Received:"+queueCn+"  len:"+len+" length:"+length+" proto:"+protocol);
        
        byte[] data = new byte[length];
        Array.Copy(res, 12, data, 0, length);
        int user_id = BitConverter.ToInt32(data, 0);
        Debug.Log("user_id:"+user_id);
        TexasHoldemClient c = TexasHoldemClient.Instance;
        c.AddRecvData(protocol,data);
        if (queueCn>0)
            ws.SendString(sendQueue.Dequeue());
//        reply = res;
    }
}
