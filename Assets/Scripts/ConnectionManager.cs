using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ConnectionManager : MonoBehaviour
{
    // Service port used by JAGC to communicate.
    public const ushort JAGC_PORT = 2022;
    [Tooltip("Name of a JAGC-compatible server (IPv4 addresses are also considered).")]
    [SerializeField]
    InputField serverName;
    [Tooltip("Button used to quit the application.")]
    [SerializeField]
    Button quitButton;
    [Tooltip("Button used to start checking server structure.")]
    [SerializeField]
    Button checkButton;
    [Tooltip("Text which'll inform user about errors if they come.")]
    [SerializeField]
    Text errorStatus;

    string server;

    void Start()
    {
        Server.onStructureCheck += OnStructureCheck;
        serverName.onEndEdit.AddListener(OnServerNameEditEnd);

        errorStatus.text = string.Empty; // Clear error status.
        // Save server name in order to avoid inputting it next time (comfortability reasons).
        server = serverName.text = PlayerPrefs.GetString("lastServerName");
    }

    public void Quit()
    {
        Application.Quit();
    }

    /*  Sets server name to this specified in input field and port to the constant value of service running this
     *  type of application, then initiates structure check of the server with data set earlier  (covered on the beginning
     *  of comment).
     */
    public void CheckServerStructure()
    {
        errorStatus.text = string.Empty;
        quitButton.interactable = false;
        checkButton.interactable = false;
        Server.name = server;                       // Name of server to check (it could be IPv4 address too).
        Server.port = JAGC_PORT;                    // Service port of application (here JAGC).
        StartCoroutine(Server.CheckStructure());    // Check structure of server with above data.
    }
    public void OnStructureCheck(bool isValid, ServerModule.DataStatus data)
    {
        bool moduleWorks = true;
        switch(data)
        {
            case ServerModule.DataStatus.OK:
                break;
            case ServerModule.DataStatus.BROKEN:
                errorStatus.text = Server.MakeError(Server.Error.BrokenResponse, "Response received from module is broken.");
                moduleWorks = false;
                break;
            case ServerModule.DataStatus.NO_DATA:
                errorStatus.text = Server.MakeError(Server.Error.MissingResponse, "Missing module response.");
                moduleWorks = false;
                break;
        }
        if (moduleWorks)
        {
            if (isValid)
            {
                SceneManager.LoadScene("LogIn");
            }
            else
            {
                errorStatus.text = Server.MakeError(Server.Error.InvalidServerStructure, $"Server '{server}' has invalid structure.");
            }
        }
        quitButton.interactable = true;
        checkButton.interactable = true;
    }
    public void OnServerNameEditEnd(string address)
    {
        server = serverName.text;
        PlayerPrefs.SetString("lastServerName", address);
    }
}
