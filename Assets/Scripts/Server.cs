using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public struct ServerModule
{
    // Server modules' root directory.
    public const string MODULES_ROOT = "jagc";
    /*  Cycle starts from the first layer (checking structure) and ends on the last
     *  layer (logging out).
     *  The 'error' module is used to read various errors which occurred during the other
     *  module's job. It should be implemented using rich GET method (with url parameter telling
     *  which script's error log should be returned).
     */
    public const string CHECK_STRUCTURE = "chk-structure.php";  // Check the server file structure.
    public const string LOG_IN = "log-in.php";                  // Log user in.
    public const string UPDATE_ACTIVITY = "upt-activity.php";   // Update last activity time.
    public const string SYNC = "sync.php";                      // Synchronize updates interval with server.
    public const string LOG_OUT = "log-out.php";                // Log user out.

    /*  Describes state of data downloaded by a module. It is usually used for
     *  better error handling.
     */
    public enum DataStatus
    {
        OK,
        BROKEN,
        NO_DATA
    }
    /*  Describes every result of the login process.
     */
    public enum AccountManagerResponse
    {
        LOGGED_IN,          // User successfuly logged in.
        USER_NOT_FOUND,     // Specified account name isn't found.
        WRONG_PASSWORD,     // Specified password donesn't match the database one.
        ALREADY_LOGGED_IN,  // User is already logged in.
        LOGGED_OUT          // User has logged out.
    }
    /*  Describes activity-updater possible response values (its states).
     */
    public enum ActivityUpdaterResponse
    {
        UPDATED,
        USER_NOT_FOUND,
        FAIL
    }
}
public struct LocalUser
{
    public string name;     // Name of the logged-in user.
    public string password; // Password of the logged-in user.
    public bool loggedIn;   // Is current user logged in?
    /*  Structure used for storing data of the actualy logged-in user. It is set to
     *  new object, when user either logged in (set to new user data) or logged out (set to empty data).
     */
    public LocalUser(string name, string password, bool loggedIn)
    {
        this.name = name;
        this.password = password;
        this.loggedIn = loggedIn;
    }
    /*  Clears user data even if there isn't any data. "Unmounting local user" means only clearing his local data,
     *  it doesn't directly mean that user'll be logged out, he can still be logged in BUT the server won't know.
     */
    public void Unmount()
    {
        name = password = string.Empty;
        loggedIn = false;
    }
}
public static class Server
{
    // Format used to make error message.
    public const string ERROR_CONTENT_FORMAT = "[ERROR {0}] \"{1}\"";
    public const string ERROR_FORMAT = "ERROR {0} occurred.";
    /*  Server-related errors:
     *  0 - 99: Module,
     *  100 - 199: Server,
     *  200 - 299: Client
     */
    public enum Error
    {
        BrokenResponse = 0,             // Server responded with broken data (can't convert).
        MissingResponse,                // Server hasn't responded with any value.
        InvalidServerStructure = 100,   // Server has invalid structure - can't properly use its modules.
        NotConnected = 200              // Client isn't connected to the network on which server is running.
    };
    // Concatenation of IPv4 address and port number, separated with colon (':') character.
    public static string Socket
    {
        get
        {
            return $"{name}:{port}";
        }
    }
    
    public static System.Action                 // It's called when the structure checking module reports a status.
        <bool, ServerModule.DataStatus> onStructureCheck;

    public static System.Action                 // Action called when account manager (php) sends a handlable response.
        <ServerModule.AccountManagerResponse,   // It offers useful manager's response, and account data (username and
        ServerModule.DataStatus,                // password of the user, he isn't supposed to be logged in!).
        string, string> onAccountAction;

    public static System.Action                 // I'm called when a timer synchronization occurrs. I hold the update interval value.
        <float, ServerModule.DataStatus> onSync;    

    public static System.Action                 // This action is called after activity timer was updated to current.
        <ServerModule.ActivityUpdaterResponse,
        ServerModule.DataStatus> onActivityUpdate;

    public static LocalUser localUser;              // Structure storing the currently logged-in user data. Empty if user is not logged in.
    public static string name;                      // Name can be either IPv4 address or domain name.
    public static float updateInterval;             // Time in seconds between next activity-updates. It's downloaded through 'sync' module.
    public static ushort port;                      // Port of the server service on host machine, which is specified by the server name.

    [RuntimeInitializeOnLoadMethod]
    public static void OnUnityApplicationStart()
    {
        SceneManager.activeSceneChanged += (Scene prev, Scene next) =>
        {
            ClearActionsList();
        };
    }

    /*  Constructs error message from given error code and content.
     */
    public static string MakeError(Error errorCode, string content = "")
    {
        return string.Format(content == string.Empty ? ERROR_FORMAT : ERROR_CONTENT_FORMAT, (int)errorCode, content);
    }

    /*  Clears invocation list of every server action.
     */
    public static void ClearActionsList()
    {
        onStructureCheck = (bool isValid, ServerModule.DataStatus data) => { };
        onAccountAction = (ServerModule.AccountManagerResponse amr, ServerModule.DataStatus data, string username, string password) => { };
        onSync = (float updateInterval, ServerModule.DataStatus data) => { };
        onActivityUpdate = (ServerModule.ActivityUpdaterResponse aur, ServerModule.DataStatus data) => { };
    }

    /*  Computes the absolute path to the module named 'moduleName', Additionaly includes URL parameters.
     */
    public static string GetAbsoluteModulePath(string moduleName, string urlParams = "")
    {
        return $"{Socket}/{ServerModule.MODULES_ROOT}/{moduleName}?{urlParams.Replace(" ", string.Empty)}";
    }
    
    /*  Communicates with the server using GET method. It executes 'moduleName' (from modules root)
     *  with list of URL parameters (formData), and finally catches the output.
     */
    public static IEnumerator GetModule(string moduleName, System.Action<string, string> onFinish, string urlParams = "")
    {
        string modulePath = GetAbsoluteModulePath(moduleName, urlParams);
        // Create Unity request for the module.
        var uwr = UnityWebRequest.Get(modulePath);
        yield return uwr.SendWebRequest();
        // Call detecting method (by this non-numerator functions can handle downloaded data).
        onFinish.Invoke(uwr.downloadHandler.text, uwr.error);
    }

    /*  Communicates with the server using GET method. It executes 'moduleName' (from modules root)
     *  with list of URL parameters (formData), and finally catches the output.
     */
    public static IEnumerator PostModule(string moduleName, System.Action<string> onFinish, string formParams = "")
    {
        var formDict = new Dictionary<string, string>();
        string modulePath = GetAbsoluteModulePath(moduleName, formParams);
        try
        {
            string[] keyValues = formParams.Replace(" ", string.Empty).Split('&');
            for (int i = 0; i < keyValues.Length; i++)
            {
                string[] kvp = keyValues[i].Split('=');
                formDict.Add(kvp[0], kvp[1]);
            }
        }
        catch(System.Exception)
        {
            throw new System.FormatException($"Can't convert form parameters from text to dictionary type, the format is invalid: '{formParams}'.");
        }
        // Create Unity request for the module.
        var uwr = UnityWebRequest.Post(modulePath, formDict);
        yield return uwr.SendWebRequest();
        // Call detecting method (by this non-numerator functions can read error status).
        onFinish.Invoke(uwr.error);
    }

    /*  Checks if the server structure is correct and every module is working.
     */
    public static IEnumerator CheckStructure()
    {
        yield return GetModule(ServerModule.CHECK_STRUCTURE, (string response, string error) =>
        {
            if (error == null)
            {
                int isValid;
                if (int.TryParse(response, out isValid))
                {
                    // %%% Structure is valid, downloaded data is correct.
                    if(onStructureCheck != null)
                        onStructureCheck.Invoke(isValid == 1, ServerModule.DataStatus.OK);
                }
                else
                {
                    // %%% Structure is invalid, data is broken (parse failed).
                    if (onStructureCheck != null)
                        onStructureCheck.Invoke(false, ServerModule.DataStatus.BROKEN);
                }
            }
            else
            {
                // %%% Structure is incorrect (module error), data hasn't been downloaded because of module error.
                if (onStructureCheck != null)
                    onStructureCheck.Invoke(false, ServerModule.DataStatus.NO_DATA);
            }
        });
    }

    /*  Simply logs user in. Copies data of account user has logged in, checks for duplicate connections
     *  pointing to the same account (many users on single account) - prevents others from accessing
     *  already-accessed account.
     */
    public static IEnumerator LogIn(string username, string password)
    {
        if (localUser.loggedIn)
        {
            throw new System.ApplicationException($"Local user has already logged in as {localUser.name}.");
        }
        else
        {
            // Handle whole log-in process (literally response management).
            yield return GetModule(ServerModule.LOG_IN, (string response, string error) =>
            {
                if (error == null)
                {
                    ServerModule.AccountManagerResponse amr;
                    if (System.Enum.TryParse(response, out amr))
                    {
                        // Fill local user with user data he's logged-in to.
                        localUser = new LocalUser(
                            username,   // Typed username.
                            password,   // Typed password.
                                        // If response informs that the user successfuly logged in, mark local user as logged in.
                            amr == ServerModule.AccountManagerResponse.LOGGED_IN
                        );
                        // %%% Response has been successfuly parsed, data is correct.
                        if (onAccountAction != null)
                            onAccountAction.Invoke(amr, ServerModule.DataStatus.OK, username, password);
                    }
                    else
                    {
                        // %%% Data is broken (can't parse to AMR).
                        if (onAccountAction != null)
                            onAccountAction.Invoke(
                                ServerModule.AccountManagerResponse.USER_NOT_FOUND,
                                ServerModule.DataStatus.BROKEN,
                                username,
                                password
                            );
                    }
                }
                else
                {
                    // %%% No data has been downloaded due to module error.
                    if (onAccountAction != null)
                        onAccountAction.Invoke(
                            ServerModule.AccountManagerResponse.USER_NOT_FOUND,
                            ServerModule.DataStatus.NO_DATA,
                            username,
                            password
                        );
                }
            }, 
                $"username = {username} & password = {password}"
            );
        }
    }

    /*  Renews logged-in user's connection timer stored in the database. If it won't be renewed, then connection
     *  will be removed soon.
     */
    public static IEnumerator UpdateActivity()
    {
        if (localUser.loggedIn)
        {
            yield return GetModule(ServerModule.UPDATE_ACTIVITY, (string response, string error) =>
            {
                if(error == null)
                {
                    ServerModule.ActivityUpdaterResponse aur;
                    if(System.Enum.TryParse(response, out aur))
                    {
                        // %%% Response has been successfuly parsed, data is correct.
                        if (onActivityUpdate != null)
                            onActivityUpdate.Invoke(aur, ServerModule.DataStatus.OK);
                    }
                    else
                    {
                        // %%% Activity is probably updated, but data is broken and thus it's probably not 100% true.
                        if (onActivityUpdate != null)
                            onActivityUpdate.Invoke(ServerModule.ActivityUpdaterResponse.UPDATED, ServerModule.DataStatus.BROKEN);
                    }
                }
                else
                {
                    // %%% Module error, data hasn't been downloaded beacuse of module error.
                    if (onActivityUpdate != null)
                        onActivityUpdate.Invoke(ServerModule.ActivityUpdaterResponse.FAIL, ServerModule.DataStatus.NO_DATA);
                }
            },
                $"username={localUser.name}"
            );
        }
        else
        {
            throw new System.ApplicationException($"Local user is not logged in and thus can't update his activity. Are you nuts?");
        }
    }

    /*  Literally synchronizes local timers with server's by downloading the data.
     */
    public static IEnumerator Synchronize()
    {
        yield return GetModule(ServerModule.SYNC, (string response, string error) =>
        {
            if (error == null)
            {
                float interval;
                if (float.TryParse(response, out interval))
                {
                    updateInterval = interval;
                    // %%% Response has been successfuly parsed, data is correct.
                    if (onSync != null)
                        onSync.Invoke(interval, ServerModule.DataStatus.OK);
                }
                else
                {
                    // %%% Downloaded data is broken - can't convert to float.
                    if (onSync != null)
                        onSync.Invoke(0.0f, ServerModule.DataStatus.BROKEN);
                }
            }
            else
            {
                // %%% Module error, data can't be downloaded due to module error.
                if (onSync != null)
                    onSync.Invoke(0.0f, ServerModule.DataStatus.NO_DATA);
            }
        });
    }

    /*  Just logs user out. Additionaly removes connection record from the database.
     */
    public static IEnumerator LogOut()
    {
        // User can disconnect from server only if he is already connected to a one.
        if (localUser.loggedIn)
        {
            yield return PostModule(ServerModule.LOG_OUT, (string error) =>
            {
                if(error == null)
                {
                    // %%% No error occurred when executing module.
                    if(onAccountAction != null)
                        onAccountAction.Invoke(
                            ServerModule.AccountManagerResponse.LOGGED_OUT,
                            ServerModule.DataStatus.NO_DATA,
                            localUser.name,
                            localUser.password
                        );
                }
                else
                {
                    // %%% Module error.
                    if (onAccountAction != null)
                            onAccountAction.Invoke(
                            ServerModule.AccountManagerResponse.USER_NOT_FOUND,
                            ServerModule.DataStatus.NO_DATA,
                            localUser.name,
                            localUser.password
                        );
                }
            },
                $"username={localUser.name}"
            );
        }
        else
        {
            Debug.LogWarning("You are not logged in. I promise that if you log in you will be able to log out too.");
        }
    }
}
