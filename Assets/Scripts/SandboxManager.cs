#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class SandboxManager : MonoBehaviour
{

    void Awake()
    {
        Server.name = "dpy6ft.camdvr.org";
        Server.port = 2022;
        Server.onAccountAction += OnAccountAction;
        Debug.Log(Server.GetAbsoluteModulePath(ServerModule.LOG_IN));
        StartCoroutine(Server.LogIn("bwilk315", "1jz702xq"));
    }
    void Update()
    {
        
    }
    void OnAccountAction(ServerModule.AccountManagerResponse amr, ServerModule.DataStatus data, string username, string password)
    {
        Debug.Log(data);
    }
}
#endif