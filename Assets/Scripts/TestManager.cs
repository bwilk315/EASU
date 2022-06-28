using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TestManager : EASU
{
    [SerializeField] GameObject connectPanel;
    [SerializeField] GameObject logInPanel;
    [SerializeField] GameObject boardPanel;
    [SerializeField] TMP_InputField serverNameInput;
    [SerializeField] TMP_InputField nameInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] Button connectBtn;
    [SerializeField] Button logInBtn;
    [SerializeField] Transform boardContent;

    void Awake()
    {
        Application.targetFrameRate = 300;
        connectPanel.SetActive(true);
        logInPanel.SetActive(false);
        boardPanel.SetActive(false);
    }

    public void Btn_Connect()
    {
        connectBtn.interactable = false;
        Connect(new Socket(serverNameInput.text, 8000), timeout: 4);
    }
    public void Btn_LogIn()
    {
        logInBtn.interactable = false;
        LogIn(nameInput.text, passwordInput.text);
    }
    public void Btn_LogOut()
    {
        LogOut();
    }

    public override void OnConnect(string error)
    {
        if (error == null)
        {
            Debug.LogFormat("Connected to '{0}' successfuly!", socket);
            connectPanel.SetActive(false);
            logInPanel.SetActive(true);
        }
        else
        {
            Debug.LogError(error);
            connectBtn.interactable = true;
        }
    }
    public override void OnAccountAction(Module.AccountManagerResponse amr, Module.DataStatus data, string username, string password)
    {
        if(amr == Module.AccountManagerResponse.LOGGED_IN)
        {
            logInPanel.SetActive(false);
            boardPanel.SetActive(true);
        }
        else if(amr == Module.AccountManagerResponse.LOGGED_OUT)
        {
            logInBtn.interactable = true;
            boardPanel.SetActive(false);
            logInPanel.SetActive(true);
        }
        else
        {
            logInBtn.interactable = true;
        }
        // Tests ...
        switch(data)
        {
            case Module.DataStatus.OK:
            case Module.DataStatus.NO_DATA:
                {
                    switch (amr)
                    {
                        case Module.AccountManagerResponse.LOGGED_IN:
                            Debug.LogFormat("User {0} successfuly logged in with password {1}!", username, password);
                            break;
                        case Module.AccountManagerResponse.USER_NOT_FOUND:
                            Debug.LogFormat("Account with username {0} isn't found!", username);
                            break;
                        case Module.AccountManagerResponse.WRONG_PASSWORD:
                            Debug.LogFormat("Password {0} isn't correct for user {1}.", password, username);
                            break;
                        case Module.AccountManagerResponse.ALREADY_LOGGED_IN:
                            Debug.LogFormat("User {0} is already logged in!", username);
                            break;
                        case Module.AccountManagerResponse.LOGGED_OUT:
                            Debug.LogFormat("Logged out user {0}.", username);
                            break;
                        case Module.AccountManagerResponse.ALREADY_LOGGED_OUT:
                            Debug.Log("User is already logged out!");
                            break;
                        case Module.AccountManagerResponse.FAIL:
                            Debug.LogError("Error occurred when tried to call AMR module (it may be a module code error).");
                            break;
                        default:
                            Debug.LogError("AMR response is unknown.");
                            break;
                    }
                    break;
                }
            case Module.DataStatus.BROKEN:
                Debug.LogError("AMR response is broken - can't convert.");
                break;
        }
    }
    public override void OnActivityReport(Module.ActivityReporterResponse aur, Module.DataStatus data)
    {
        if(aur == Module.ActivityReporterResponse.UPDATED && data == Module.DataStatus.OK)
        {
            StartCoroutine(GetModule("get-sessions.php", (string response, string error) =>
            {
                if (error == null)
                {
                    for(int i = 0; i < boardContent.childCount; i++)
                    {
                        Destroy(boardContent.GetChild(i).gameObject);
                    }
                    string[] users = response.Split('&');
                    for (int i = 0; i < users.Length; i++)
                    {
                        string user = users[i];
                        if (user != string.Empty)
                        {
                            string[] kvp = user.Split('=');
                            Transform record = Instantiate(Resources.Load<Transform>("Prefabs/UI/Board Record"), boardContent);
                            record.GetChild(0).GetComponent<TMP_Text>().text = kvp[0];
                            record.GetChild(1).GetComponent<TMP_Text>().text = kvp[1];
                        }
                    }
                }
            }));
        }
        switch(data)
        {
            case Module.DataStatus.OK:
            case Module.DataStatus.NO_DATA:
                {
                    switch(aur)
                    {
                        case Module.ActivityReporterResponse.UPDATED:
                            Debug.LogFormat("Reported activity successfuly!");

                            break;
                        case Module.ActivityReporterResponse.USER_NOT_FOUND:
                            Debug.LogError("ARR can't update activity because user isn't found. Are you logged in?");
                            break;
                        case Module.ActivityReporterResponse.FAIL:
                            Debug.LogError("ARR failed to update, user connection not found in database.");
                            break;
                        default:
                            Debug.LogError("ARR response is unknown.");
                            break;
                    }
                    break;
                }
            case Module.DataStatus.BROKEN:
                Debug.LogError("ARR can't convert response, it has invalid format (is broken).");
                break;
        }
    }
}
