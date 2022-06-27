using UnityEngine;

public class SandboxManager : EASU
{
    string username = "bwilk315";
    string password = "315315";

    void Awake()
    {
        Application.targetFrameRate = 300;
        Connect(new Socket("dpy6ft.camdvr.org", 8000));
    }
    void Update()
    {
        if (localUser.loggedIn && Input.GetKeyDown(KeyCode.Q))
            LogOut();
    }
    public override void OnConnect(string error)
    {
        if(error == null)
        {
            Debug.LogFormat("Connected to '{0}' successfuly!", socket);
            LogIn(username, password);
        }
        else
        {
            Debug.Log(error);
        }
    }
    public override void OnAccountAction(Module.AccountManagerResponse amr, Module.DataStatus data, string username, string password)
    {
        if(amr == Module.AccountManagerResponse.LOGGED_IN)
        {
            Debug.LogFormat("User {0} successfuly logged in with password {1}!", username, password);
        }
        else if(amr == Module.AccountManagerResponse.LOGGED_OUT)
        {
            Debug.LogFormat("User {0} logged out!", username);
        }
    }
    public override void OnActivityReport(Module.ActivityUpdaterResponse aur, Module.DataStatus data)
    {
        if(aur == Module.ActivityUpdaterResponse.UPDATED)
        {
            Debug.Log("Reported activity successfuly!");
        }
    }
}
