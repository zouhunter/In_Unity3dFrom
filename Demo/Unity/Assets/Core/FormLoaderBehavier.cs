﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using System;
using MessageTrans;
using MessageTrans.Interal;
using FormSwitch.Internal;
using FormSwitch;
using Newtonsoft.Json;
using Protocal;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;

public class FormLoaderBehavier : MonoBehaviour {

    IWindowSwitchCtrl windowswitch;
    DataReceiver receiver;
    DataSender childSender;

    Queue<KeyValuePair<string, string>> sendQueue = new Queue<KeyValuePair<string, string>>();
    Dictionary<ProtocalType, Action<string>> unityWait = new Dictionary<ProtocalType, Action<string>>();

    private void Awake()
    {
        receiver = new MessageTrans.DataReceiver();
        receiver.RegistHook();
        childSender = new MessageTrans.DataSender();
    }

    private void Update()
    {
        if (sendQueue.Count > 0 && windowswitch.Child != IntPtr.Zero)
        {
            KeyValuePair<string, string> data = sendQueue.Dequeue();
            childSender.SendMessage(data.Key, data.Value);
        }
    }

    public void TryOpenHelpExe(string exePath)
    {
        windowswitch = new WindowSwitchUnity();
        string path = exePath;// Application.dataPath.Substring(0, Application.dataPath.LastIndexOf("Demo")) + "FormLoader/FormLoader/bin/Debug/FormLoader.exe";
        if (System.IO.File.Exists(path))
        {
#if UNITY_EDITOR
            if (windowswitch.OpenChildWindow(path, false, "1"))
#else
            if (windowswitch.OpenChildWindow(path, false))
#endif
            {
                //打开子应用程序
                StartCoroutine(DelyRegisterSender());
            }
        }
        else
        {
            Debug.LogError("exe not fond");
        }
    }

    private void OnExeHelpExt(object sender, EventArgs e)
    {
        Destroy(gameObject);
    }

    private IEnumerator DelyRegisterSender()
    {
        while (windowswitch.Child == IntPtr.Zero){
            yield return null;
        }
        childSender.RegistHandle(windowswitch.Child);
#if UNITY_EDITOR
        yield return ReadPathSelect();
#endif
    }

#if UNITY_EDITOR
    private IEnumerator ReadPathSelect()
    {
        while (true)
        {
            yield return new WaitForSeconds(1);
            foreach (var item in unityWait)
            {
                try
                {
                    string path = System.IO.Directory.GetCurrentDirectory() + "/" + item.Key+ ".txt";
                    if (System.IO.File.Exists(path))
                    {
                        string receivedText = System.IO.File.ReadAllText(path);
                        if (item.Value != null) item.Value.Invoke(receivedText);
                        System.IO.File.Delete(path);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }
            
          
        }
    }
#endif

    public void AddSendQueue(ProtocalType type,string value,Action<string> onReceive)
    {
        sendQueue.Enqueue(new KeyValuePair<string, string>(type.ToString(), value));
        receiver.RegisterEvent(type.ToString(), onReceive);
#if UNITY_EDITOR
        Debug.Log(type.ToString());
        if (!unityWait.ContainsKey(type)){
            unityWait.Add(type,onReceive);
        }
#endif
    }

    private void OnDestroy()
    {
        receiver.RemoveHook();
        windowswitch.CloseChildWindow();
        windowswitch.OnCloseThisWindow();
    }
}
