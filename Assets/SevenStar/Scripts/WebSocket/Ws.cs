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

    private Queue<byte[]> sendQueue;

    int queueCn;
    
    private void Awake()
    {
        Instance = this;
//        ws=new WebSocket(new Uri("ws://211.238.13.182:18080"));
        ws=new WebSocket(new Uri("ws://localhost:13300"));
        sendQueue=new Queue<byte[]>();
        queueCn = 0;
    }

    private void Start()
    {
        StartCoroutine(StartConnect());
    }

    public IEnumerator StartConnect()
    {
        yield return StartCoroutine(ws.Connect());
        Debug.Log("connected");
        TexasHoldemClient.Instance.IsConnect = true;
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

    private int m_PacketNumber = 1;
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
            Debug.Log("send cn:" + queueCn + "  protocal:" + protocol + " len" + len);
            ws.Send(SendData);
//            if (queueCn == 0)
//            {
//                ws.Send(SendData);
//            }
//            else
//            {
//                sendQueue.Enqueue(SendData);
//            }
//            queueCn++;
            return true;
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
        return false;
    }
    
    public void Receive(byte[] res)
    {
//        queueCn--;
        int len = BitConverter.ToInt32(res,0);
        int length = len - 12;
        int protocol = BitConverter.ToInt32(res, 8);
        Debug.Log("Received:"+queueCn+"  len:"+len+" length:"+length+" proto:"+(Protocols)protocol);
        
        byte[] data = new byte[length];
        Array.Copy(res, 12, data, 0, length);
        TexasHoldemClient c = TexasHoldemClient.Instance;
        c.AddRecvData(protocol,data);
        if (queueCn>0)
            ws.Send(sendQueue.Dequeue());
//        reply = res;
    }
}
