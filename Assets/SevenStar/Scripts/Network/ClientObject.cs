﻿using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityUtil;

public class ClientObject : UtilObjectSingleton<ClientObject>
{
    //public TexasHoldemClient m_Client = new TexasHoldemClient();
    public TexasHoldemClient m_Client = null;
    public UserMgr m_UserMgr = null;
    public UserInfo m_UserInfo = null;
    public int m_RoomIdx = -1;
    public int m_UserIdx = -1;
    public int m_BlindType = -1;
    private Timer m_Timer = new System.Timers.Timer();
    private const int m_ApplicationPauseTimeLimit = 10;//sec
    private int m_PauseTime = 0;

    private void Awake()
    {
        m_Client = TexasHoldemClient.Instance;
        m_UserMgr = UserMgr.Instance;
        //Instance = this;
        /*if (m_Client == null)
            m_Client = new TexasHoldemClient();*/
        if(m_Timer ==null)
            m_Timer = new System.Timers.Timer();
        m_Timer.Interval = 1000;
        m_Timer.Elapsed += new ElapsedEventHandler(Callback_TimerElapsed);
    }
    
    void Start ()
    {
        //DontDestroyOnLoad(this);
        //m_Client.Connect("118.45.180.254");
        //m_Client.Connect("211.238.13.182");
        //m_Client.Connect("127.0.0.1");
    }
	
	void Update ()
    {
        m_Client.Update();
        m_UserMgr.Update();
        CheckUserInfo();
        CheckConnection();
    }
    
    void CheckConnection()
    {
        if(m_Client.IsConnect == false)
        {
            if(SceneManager.GetActiveScene().name != "1_Title")
            {
                SceneManager.LoadScene("1_Title");
            }
        }
    }

    void CheckUserInfo()
    {
        if (m_UserIdx <= 0)
        {
            m_UserInfo = null;
            return;
        }
        m_UserInfo = m_UserMgr.GetUserInfo(m_UserIdx);
    }

    /// <summary>
    /// When application Pause
    /// </summary>
    /// <param name="pause"></param>
    private void OnApplicationPause(bool pause)
    {
        Debug.Log("OnApplicationPause " + pause);
        if(pause == true)
        {
            SevenStarLogic.m_ApplicationPauseTrigger = true;
            m_Timer.Start();
        }
        else
        {
            SevenStarLogic.m_ApplicationPauseTrigger = false;
            m_Timer.Stop();
            if (m_PauseTime >= m_ApplicationPauseTimeLimit)
                SceneManager.LoadScene("1_Title");
            m_PauseTime = 0;
        }

        /*if (pause == true)
        {
            if(m_Client.IsConnect)
                m_Client.Disconnect();
        }
        else
        {
            SceneManager.LoadSceneAsync("1_Title");
        }*/
    }

    private void Callback_TimerElapsed(object sender, ElapsedEventArgs e)
    {
        m_PauseTime++;
        Debug.Log("pause time elapsed : "+m_PauseTime);
        if(m_PauseTime >= m_ApplicationPauseTimeLimit)
        {
            if (m_Client.IsConnect)
                m_Client.Disconnect();

            m_Timer.Stop();
        }
    }

#if !UNITY_EDITOR
    /// <summary>
    /// When application Quit
    /// </summary>
    private void OnApplicationQuit()
    {
        m_Client.Disconnect();
        if (m_Timer != null)
        {
            m_Timer.Stop();
            m_Timer.Dispose();
        }

		System.Diagnostics.Process.GetCurrentProcess().Kill();

    }
#endif
}

