using UnityEngine;

public class SandboxManager : MonoBehaviour
{
    public Socket socket;
    void Awake()
    {
        Application.targetFrameRate = 300;
        Server.onConnect = OnConnect;
        socket = new Socket("dpy6ft.camdvr.org", 8000); // Server address and http service port (for me it's 8000).
        StartCoroutine(Server.Connect(socket));         // Try to connect to the server with socket made above.
    }
    public void OnConnect(string error)
    {
        if(error == null)
        {
            Debug.LogFormat("Connected to '{0}' successfuly!", Server.socket);
        }
        else
        {
            Debug.Log(error);
        }
    }
}
