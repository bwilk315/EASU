using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    void Awake()
    {
        
    }
    void Start()
    {
        
    }
    void Update()
    {
        
    }
    public void LogOut()
    {
        StartCoroutine(Server.LogOut());
    }
}
