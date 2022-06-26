using UnityEngine;
using UnityEngine.UI;

public class UserListRecord : MonoBehaviour
{
    [SerializeField]
    Text username;
    [SerializeField]
    Text password;
    void Start()
    {
        
    }
    void Update()
    {
        
    }
    public void SetFields(string username, string password)
    {
        this.username.text = username;
        this.password.text = password;
    }
}
