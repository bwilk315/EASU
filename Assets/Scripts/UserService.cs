using UnityEngine;

public class UserService : MonoBehaviour
{
    public static UserService current; // Reference to the only running user service.
    // Time gap between next activity-updates.
    public WaitForSeconds UpdateInterval => new WaitForSeconds(Server.updateInterval);

    void Awake()
    {
        DontDestroyOnLoad(this);                // Service won't stop unless application is closed.
        current = this;                         // Reference to running service (the only).
        Application.targetFrameRate = 60;       // Limit frames, for android 60 frames per second is optimal.
        Server.onAccountAction = OnAccountAction;
    }

    void OnAccountAction(Module.AccountManagerResponse response, Module.DataStatus data, string username, string password)
    {
        // If user has successfully logged in, start activity-updater loop.
        if(response == Module.AccountManagerResponse.LOGGED_IN)
        {
            StartCoroutine(UpdaterLoop());
        }
        // Stop updater, when user logs out.
        else if(response == Module.AccountManagerResponse.LOGGED_OUT)
        {
            StopCoroutine(UpdaterLoop());
        }
    }

    /*  Recurency-based activity updater loop. It updates user's activity timer if he's logged in,
     *  then waits <update-interval> seconds (interval's downloaded using 'sync' module) and cycles again.
     */
    System.Collections.IEnumerator UpdaterLoop()
    {
        if (Server.localUser.loggedIn)
        {
            yield return Server.UpdateActivity();
        }
        yield return UpdateInterval;
        yield return UpdaterLoop();
    }
}
