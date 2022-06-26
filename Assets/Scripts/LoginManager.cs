using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LoginManager : MonoBehaviour
{
    [SerializeField]
    Text errorStatus;
    [SerializeField]
    InputField username;
    [SerializeField]
    InputField password;
    [SerializeField]
    Toggle rememberMe;
    [SerializeField]
    Button disconnectButton;
    [SerializeField]
    Button logInButton;

    void Awake()
    {
        Server.onAccountAction += OnAccountAction;
    }
    void Start()
    {
        if (PlayerPrefs.GetInt("rememberUserData") == 1)
        {
            username.text = PlayerPrefs.GetString("rememberedUsername");
            password.text = PlayerPrefs.GetString("rememberedPassword");
            rememberMe.isOn = true;
        }
        errorStatus.text = string.Empty;
    }
    public void Disconnect() => SceneManager.LoadScene("Connect");
    public void LogIn()
    {
        rememberMe.interactable = false;
        disconnectButton.interactable = false;
        logInButton.interactable = false;
        errorStatus.text = string.Empty;
        StartCoroutine(Server.LogIn(username.text, password.text));
    }
    public void OnAccountAction(ServerModule.AccountManagerResponse response, ServerModule.DataStatus data, string username, string password)
    {
        if(response == ServerModule.AccountManagerResponse.LOGGED_IN)
        {
            PlayerPrefs.SetInt("rememberUserData", rememberMe.isOn ? 1 : 0);
            PlayerPrefs.SetString("rememberedUsername", username);
            PlayerPrefs.SetString("rememberedPassword", password);
            SceneManager.LoadScene("Menu");
        }
        else
        {
            rememberMe.interactable = true;
            disconnectButton.interactable = true;
            logInButton.interactable = true;
            switch (response)
            {
                case ServerModule.AccountManagerResponse.USER_NOT_FOUND:
                    errorStatus.text = $"User {username} not found";
                    break;
                case ServerModule.AccountManagerResponse.WRONG_PASSWORD:
                    errorStatus.text = "Wrong password";
                    break;
                case ServerModule.AccountManagerResponse.ALREADY_LOGGED_IN:
                    errorStatus.text = $"User {username} is already logged in, try again later.";
                    break;
                case ServerModule.AccountManagerResponse.LOGGED_OUT:
                    errorStatus.text = $"User {username} logged out. You can log in now";
                    break;
            }
        }
    }
}
