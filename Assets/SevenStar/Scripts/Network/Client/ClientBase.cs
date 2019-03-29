using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;



public class ClientBase
{

    public class WorkBuffer
    {
        public const int BufferSize = 8192;
        public byte[] buffer = new byte[BufferSize];
        public int BufferPos = 0;

        public void Clear()
        {
            BufferPos = 0;
        }

        public void AddBuffer(byte[] data, int length)
        {
            Array.Copy(data, 0, buffer, BufferPos, length);
            BufferPos += length;
        }

        public byte[] Work()
        {
            if (BufferPos < 12)
                return null;
            int workPos = 0;
            while (BufferPos >= workPos + 12)
            {
                int len = BitConverter.ToInt32(buffer, workPos);
                if (len > ClientBase.RecvBufferSize)
                {
                    workPos += 1;
                    continue;
                }
                if (BufferPos < workPos + len)
                    break;
                byte[] rData = new byte[len];
                Array.Copy(buffer, workPos, rData, 0, len);
                workPos += len;
                PopFront(workPos);
                return rData;
            }
            PopFront(workPos);
            return null;
        }

        void PopFront(int length)
        {
            int LeftLen = BufferPos - length;
            if (LeftLen <= 0)
            {
                BufferPos = 0;
                return;
            }
            Array.Copy(buffer, length, buffer, 0, LeftLen);
            BufferPos -= length;
        }
    }

    const int RecvBufferSize = 2048;
    public class RecvData
    {
        public byte[] data;
        public int protocol;
    }

    Socket m_Sock;
    int m_PacketNumber = 0;
    //int m_ServerPort = 12300;
    int m_ServerPort = 13300;

    byte[] m_RecvBuffer = new byte[RecvBufferSize];
    WorkBuffer m_WorkBuf = new WorkBuffer();
    IAsyncResult m_RecvAsync = null;
    public Action m_OnDisconnect = null;

    object m_LockObj = new object();
    List<RecvData> m_RecvData = new List<RecvData>();
    static public Action<string> LogFunc = null;


    public bool IsConnect = false;
    
    void Receive()
    {
        try
        {
            m_RecvAsync = m_Sock.BeginReceive(m_RecvBuffer, 0, RecvBufferSize, SocketFlags.None, new AsyncCallback(OnRecvCallback), m_Sock);
        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
    }

    static public void Log(string logMessage)
    {
        if (LogFunc == null)
            return;
        LogFunc(logMessage);
    }

    public bool Connect(string ServerIP)
    {
        
        if (m_Sock != null)
        {
            return false;
            //Disconnect();
        }
        try
        {
            m_Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_Sock.Connect(ServerIP, m_ServerPort);
            m_PacketNumber = 0;
            m_WorkBuf.Clear();
            Receive();
        }
        catch(SocketException e)
        {
            Log(e.ToString());
            if (m_Sock != null)
                m_Sock.Close();
            m_Sock = null;
            return false;
        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
        return true;
    }

    public void Disconnect()
    {
        IsConnect = false;
        Ws.ws.Close();
    }
        
    public bool Send(Protocols protocol, byte[] data)
    {
        return Ws.Instance.Send(protocol, data);
    }

    public bool Send_int(Protocols protocol, int v)
    {
        //byte[] data = new byte[4];
        //Array.Copy(BitConverter.GetBytes(v), 0, data, 0, 4);
        byte[] data = BitConverter.GetBytes(v);
        return Send(protocol, data);
    }

    public bool Send_UInt64(Protocols protocol, UInt64 v)
    {
        byte[] data = BitConverter.GetBytes(v);
        return Send(protocol, data);
    }

    static public byte[] StringArrayToByte(string[] strArr)
    {
        byte[] rData;
        List<byte[]> tmp = new List<byte[]>();
        int totLen = 0;
        int i, j;
        j = strArr.Length;
        for (i = 0; i < j; i++)
            tmp.Add(Encoding.UTF8.GetBytes(strArr[i]));
        totLen = j;
        for (i = 0; i < j; i++)
            totLen += tmp[i].Length;
        rData = new byte[totLen];
        int pos = 0;
        for (i = 0; i < j; i++)
        {
            rData[pos] = (byte)tmp[i].Length;
            Array.Copy(tmp[i], 0, rData, pos + 1, tmp[i].Length);
            pos += tmp[i].Length + 1;
        }

        return rData;
    }

    

    public virtual void Update()
    {
        while (true)
        {
            try
            {
                RecvData d = GetRecvData();
                if (d == null)
                    break;
                RecvDataWork(d);
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }
    }

	public void AddRecvData(int pro,byte[] data0)
    {
        RecvData d = new RecvData();
        d.protocol = pro;
        d.data = data0;
        m_RecvData.Add(d);
    }
	
    void AddRecvData()
    {
        try
        {
            lock (m_LockObj)
            {
                while (true)
                {
                    byte[] buf = m_WorkBuf.Work();
                    if (buf == null)
                        break;
                    int length = BitConverter.ToInt32(buf, 0) - 12;
                    if (length < 0)
                        return;
                    int protocol = BitConverter.ToInt32(buf, 8);

                    RecvData d = new RecvData();
                    d.protocol = BitConverter.ToInt32(buf, 8);

                    //type2
                    d.data = new byte[length];
                    Array.Copy(buf, 12, d.data, 0, length);
                    m_RecvData.Add(d);
                }

            }
        }
        catch(Exception e)
        {
            Log(e.ToString());
        }

    }

    RecvData GetRecvData()
    {
        lock (m_LockObj)
        {
            try
            {
                while (m_RecvData.Count > 0)
                {
                    RecvData d = m_RecvData[0];
                    m_RecvData.RemoveAt(0);
                    return d;
                }
            }
            catch(Exception e)
            {
                Log(e.ToString());
            }
            
        }
        return null;
    }

    protected virtual void RecvDataWork(RecvData data)
    {

    }

    void OnRecvCallback(IAsyncResult ar)
    {
        try
        {
            Socket tmpSock = (Socket)ar.AsyncState;
            if (tmpSock != null)
            {
                if (tmpSock.Connected)
                {
                    int readSize = tmpSock.EndReceive(ar);
                    if (readSize > 0)
                    {
                        int length = BitConverter.ToInt32(m_RecvBuffer, 0);
                        m_WorkBuf.AddBuffer(m_RecvBuffer, readSize);
                        AddRecvData();
                        /*if (length == readSize)
                        {
                            AddRecvData();
                        }*/
                    }
                    Receive();
                }
                else
                {
                    m_RecvAsync = null;
                    Disconnect();
                    if (m_OnDisconnect != null)
                        m_OnDisconnect();
                }
            }
        }
        catch (SocketException e)
        {
            Log(e.ToString());
            m_RecvAsync = null;
            Disconnect();
            if (m_OnDisconnect != null)
                m_OnDisconnect();
        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
    }
        
    void OnDisconnectCallback(IAsyncResult ar)
    {
        try
        {
            m_Sock.EndDisconnect(ar);
            m_Sock = null;
        }
        catch (SocketException e)
        {
            Log(e.ToString());
        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
    }
}
