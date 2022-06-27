using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine;

/*  Structure responsible for servicing server modules information to other parts of EASU.
 *  It contains a few useful methods, for example the one for getting absolute network path
 *  to the php module.
 */
public struct Module
{
    /*  Cycle starts from the first layer (checking structure) and ends on the last
     *  layer (logging out). Obviously, everything is fine if structure is correct.
     */
    public const string GET_ERROR       = "get-error.php";      // GET  Gets latest error of the specified module.
    public const string CHECK_STRUCTURE = "chk-structure.php";  // GET  Checks the server's file structure.
    public const string LOG_IN          = "log-in.php";         // POST Logs user in.
    public const string UPDATE_ACTIVITY = "upt-activity.php";   // POST Update last activity time.
    public const string SYNC            = "sync.php";           // POST Synchronize updates interval with server.
    public const string LOG_OUT         = "log-out.php";        // POST Log user out.
    /*  Describes state (good, broken or empty) of data requested by module. It's usually
     *  used for better error handling.
     */
    public enum DataStatus
    {
        OK,         // Data-download went well.
        BROKEN,     // Data has invalid format, module can't use it properly.
        NO_DATA     // No data has been downloaded (it may or may not be an error status).
    }
    /*  The AMR describes every state the 'log-in' and 'log-out' modules can respond with.
     *  The 'log-in' is able to respond with majority of possibilities, where the 'log-out' can respond
     *  only with one state (LOGGED_OUT or optionaly with a wrong format aka error).
     */
    public enum AccountManagerResponse
    {
        LOGGED_IN,          // User successfuly logged in.
        USER_NOT_FOUND,     // Specified account name isn't found.
        WRONG_PASSWORD,     // Specified password doesn't match the one in database.
        ALREADY_LOGGED_IN,  // User is already logged in. Can't log user in to the currently-online account.
        LOGGED_OUT          // User has successfuly logged out.
    }
    /*  The AUR describes states of activity updater. Updating the last activity time is important in order
     *  to show that the user is still active and his session is not deprecated (so he can stay logged-in).
     */
    public enum ActivityUpdaterResponse
    {
        UPDATED,        // Last activity time (LAT) successfuly updated to current (server time).
        USER_NOT_FOUND, // Can't find user for which LAT should be renewed.
        FAIL            // AUR failed, the module may be missing, or other unknown error occurred.
    }

    public static IEnumerator Exists(string moduleName, System.Action<bool> onFinish)
    {
        yield return Server.GetModule(moduleName, (string response, string error) =>
        {
            onFinish(error == null);
        });
    }
    public static string GetNetworkPath(string moduleName)
    {
        return $"{Server.socket}/{Server.EASU_DIR}/{moduleName}";
    }
}
/*  Structure used for storing data of the actualy logged-in user. It is set to
 *  new object, when user either logged in (set to new user data) or logged out (set to empty data).
 */
public struct LocalUser
{
    public string name;     // Name of the logged-in user.
    public string password; // Password of the logged-in user.
    public bool loggedIn;   // Is current user logged in?
    public LocalUser(string name, string password, bool loggedIn)
    {
        this.name = name;
        this.password = password;
        this.loggedIn = loggedIn;
    }
    /*  Clears user data even if there isn't any data. "Clearing local user" means only erasing his local data,
     *  it doesn't directly mean that user'll be logged out, he can still be logged in BUT nobody will know this.
     */
    public void Clear()
    {
        name = password = string.Empty;
        loggedIn = false;
    }
}
/*  Server socket used for client-server communication.
 */
public struct Socket
{
    public string address;
    public ushort port;
    public Socket(string address, ushort port)
    {
        this.address = address;
        this.port = port;
    }
    
    public override string ToString()
    {
        return $"{address}:{port}";
    }
}
public static class Server
{
    /*  The whole directory is used by EASU; if it has invalid structure,
     *  the client-server communication will be broken.
     */
    public const string EASU_DIR = "easu";
    public const string CUSTOM_MODULES_DIR = "easu/custom";             // Directory for custom php modules.
    public const string ERROR_CONTENT_FORMAT = "[ERROR {0}] \"{1}\"";   // Format of error with message.
    public const string ERROR_FORMAT = "ERROR {0} occurred.";           // Format of error code only.
    /*  Describes all possible errors that can occur in EASU, they are categorized:
     *  0 - 99: Module,
     *  100 - 199: Server,
     *  200 - 299: Client
     */
    public enum Error
    {
        ConnectionError = 0,            // Can't connect wit the socket given.                 
        BrokenResponse,                 // Server responded with broken data (module can't convert).
        MissingResponse,                // Server hasn't responded with any value when it was expected.
        InvalidServerStructure = 100,   // Server has invalid structure - can't properly use its modules.
        NotConnected = 200              // Client isn't connected to the network on which server is running.
    };

    public static System.Action             // I'm invoked when there is a try to connect to a server with specified socket.
        <string>
        onConnect;

    public static System.Action             // Action called when account manager (php) sends a handlable response.
        <Module.AccountManagerResponse,     // It offers useful manager's response, and account data (username and
        Module.DataStatus, string, string>  // password of the user, he isn't supposed to be logged in!).
        onAccountAction;

    public static System.Action             // I'm called when a timer synchronization occurrs. I provide the update interval value.
        <float, Module.DataStatus>
        onSync;

    public static System.Action             // This action is called after activity timer was updated to current.
        <Module.ActivityUpdaterResponse,
        Module.DataStatus>
        onActivityUpdate;
    
    public static Socket socket;            // Combination of IPv4 address or DNS name, and opened port of http service.
    public static LocalUser localUser;      // Structure storing the currently logged-in user data. Empty if user is not logged in.
    public static float updateInterval;     // Time in seconds between next activity-updates. It's downloaded through 'sync' module.

    /*  Constructs error message from given error code and content.
     */
    public static string MakeError(Error errorCode) => string.Format(ERROR_FORMAT, (int)errorCode);
    public static string MakeError(Error errorCode, string content) => string.Format(ERROR_CONTENT_FORMAT, (int)errorCode, content);
    /*  Makes error message and then prints it into Unity console as error.
     */
    public static void PrintError(Error errorCode) => Debug.LogError(MakeError(errorCode));
    public static void PrintError(Error errorCode, string content) => Debug.LogError(MakeError(errorCode, content));
    /*  Checks if 'socket' points to correct http service (the one with EASU modules implemented).
     *  If server (on that socket) responds with null error, connection was successful.
     */
    public static IEnumerator Connect(Socket socket)
    {
        Server.socket = socket;
        yield return GetModule(Module.CHECK_STRUCTURE, (string response, string error) =>
        {
            if (error == null)
            {
                int isValid;
                if(int.TryParse(response, out isValid))
                {
                    if (onConnect != null)
                        onConnect.Invoke(isValid == 1 ? null : MakeError(Error.InvalidServerStructure, $"HTTP server under {socket} has invalid structure."));                
                }
                else
                {
                    if (onConnect != null)
                        onConnect.Invoke(MakeError(
                            Error.BrokenResponse,
                            "Response from 'chk-structure' module is broken."
                        ));
                }
            }
            else
            {
                Server.socket = new Socket();
                if (onConnect != null)
                    onConnect.Invoke(MakeError(Error.ConnectionError, $"Socket {socket} is not active."));
            }
        });
    }
    /*  Communicates with the server using GET method. It executes 'moduleName' (from EASU root)
     *  with list of URL parameters 'urlParams' and when it's done, calls 'onFinish' action
     *  with appropriately two string values: 'response' and 'error'. 
     *  You can execute your custom modules by adding 'custom/' prefix to your module's name.
     */
    public static IEnumerator GetModule(string moduleName, System.Action<string, string> onFinish, string urlParams = "")
    {
        // Make rich address pointing to the module (rich means the one with URL parameters).
        string modulePath = Module.GetNetworkPath(moduleName);
        string correctUrlParams = urlParams.Replace(" ", string.Empty);
        string richAddress = $"{modulePath}?{correctUrlParams}";

        var uwr = UnityWebRequest.Get(richAddress); // Create Unity request for the module.
        yield return uwr.SendWebRequest();
        // Call detecting method (by this, non-numerator functions can handle downloaded data).
        onFinish.Invoke(uwr.downloadHandler.text, uwr.error);
    }
    /*  Communicates with the server using GET method. It executes 'moduleName' (from modules root)
     *  with list of URL parameters (formData), and finally catches the output.
     */
    public static IEnumerator PostModule(string moduleName, System.Action<string> onFinish, string formParams = "")
    {
        var formDict = new Dictionary<string, string>();
        string modulePath = Module.GetNetworkPath(moduleName);
        // Try to convert string to dictionary key-value pairs.
        string[] keyValues = formParams.Replace(" ", string.Empty).Split('&');
        for (int i = 0; i < keyValues.Length; i++)
        {
            string[] kvp = keyValues[i].Split('=');
            formDict.Add(kvp[0], kvp[1]);
        }
        
        var uwr = UnityWebRequest.Post(modulePath, formDict); // Create Unity request for the module.
        yield return uwr.SendWebRequest();
        // Call detecting method (by this non-numerator functions can read error status).
        onFinish.Invoke(uwr.error);
    }
    /*  Simply logs user in. Copies data of account user has logged in to, checks for duplicate connections
     *  pointing to the same account (many users on single account) - prevents others from accessing
     *  already-accessed account.
     */
    public static IEnumerator LogIn(string username, string password)
    {
        if (localUser.loggedIn)
        {
            // %%% User is already logged in, no data has been downloaded.
            if(onAccountAction != null)
                onAccountAction.Invoke(Module.AccountManagerResponse.ALREADY_LOGGED_IN, Module.DataStatus.NO_DATA, localUser.name, localUser.password);
        }
        else
        {
            // Handle whole log-in process (literally relies on response management).
            yield return GetModule(Module.LOG_IN, (string response, string error) =>
            {
                if (error == null)
                {
                    Module.AccountManagerResponse amr;
                    if (System.Enum.TryParse(response, out amr))
                    {
                        // Fill local user with user data if he has successfuly logged in.
                        if(amr == Module.AccountManagerResponse.LOGGED_IN)
                        {
                            localUser = new LocalUser(
                                username,   // Typed username.
                                password,   // Typed password.
                                // If response informs that the user successfuly logged in, mark local user as logged in.
                                true
                            );
                        }
                        // %%% Response has been successfuly parsed, data is correct.
                        if (onAccountAction != null)
                            onAccountAction.Invoke(amr, Module.DataStatus.OK, username, password);
                    }
                    else
                    {
                        // %%% Data is broken (can't parse to AMR structure).
                        if (onAccountAction != null)
                            onAccountAction.Invoke(
                                Module.AccountManagerResponse.USER_NOT_FOUND,
                                Module.DataStatus.BROKEN,
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
                            Module.AccountManagerResponse.USER_NOT_FOUND,
                            Module.DataStatus.NO_DATA,
                            username,
                            password
                        );
                }
            }, $"username = {username} & password = {password}");
        }
    }
    /*  Renews logged-in user's connection timer stored in the database. If it won't be renewed, then connection
     *  will be removed soon.
     */
    public static IEnumerator UpdateActivity()
    {
        if (localUser.loggedIn)
        {
            yield return GetModule(Module.UPDATE_ACTIVITY, (string response, string error) =>
            {
                if(error == null)
                {
                    Module.ActivityUpdaterResponse aur;
                    if(System.Enum.TryParse(response, out aur))
                    {
                        // %%% Response has been successfuly parsed, data is correct.
                        if (onActivityUpdate != null)
                            onActivityUpdate.Invoke(aur, Module.DataStatus.OK);
                    }
                    else
                    {
                        // %%% Activity is probably updated, but data is broken and thus it's probably not 100% true.
                        if (onActivityUpdate != null)
                            onActivityUpdate.Invoke(Module.ActivityUpdaterResponse.UPDATED, Module.DataStatus.BROKEN);
                    }
                }
                else
                {
                    // %%% Module error, data hasn't been downloaded beacuse of module error.
                    if (onActivityUpdate != null)
                        onActivityUpdate.Invoke(Module.ActivityUpdaterResponse.FAIL, Module.DataStatus.NO_DATA);
                }
            }, $"username={localUser.name}");
        }
        else
        {
            // %%% User isn't logged in, can't update activity of empty user. Are you nuts?.
            if(onActivityUpdate != null)
                onActivityUpdate.Invoke(Module.ActivityUpdaterResponse.USER_NOT_FOUND, Module.DataStatus.NO_DATA);
        }
    }
    /*  Literally synchronizes local timers with server's by downloading the data.
     */
    public static IEnumerator Synchronize()
    {
        yield return GetModule(Module.SYNC, (string response, string error) =>
        {
            if (error == null)
            {
                float interval;
                if (float.TryParse(response, out interval))
                {
                    updateInterval = interval;
                    // %%% Response has been successfuly parsed, data is correct.
                    if (onSync != null)
                        onSync.Invoke(interval, Module.DataStatus.OK);
                }
                else
                {
                    // %%% Downloaded data is broken - can't convert to float.
                    if (onSync != null)
                        onSync.Invoke(-1.0f, Module.DataStatus.BROKEN);
                }
            }
            else
            {
                // %%% Module error, data can't be downloaded due to module error.
                if (onSync != null)
                    onSync.Invoke(-1.0f, Module.DataStatus.NO_DATA);
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
            yield return PostModule(Module.LOG_OUT, (string error) =>
            {
                if(error == null)
                {
                    // %%% No error occurred when logged user out.
                    if(onAccountAction != null)
                        onAccountAction.Invoke(
                            Module.AccountManagerResponse.LOGGED_OUT,
                            Module.DataStatus.NO_DATA,
                            localUser.name,
                            localUser.password
                        );
                }
                else
                {
                    // %%% Can't find connection associated to the user specified.
                    if (onAccountAction != null)
                            onAccountAction.Invoke(
                            Module.AccountManagerResponse.USER_NOT_FOUND,
                            Module.DataStatus.NO_DATA,
                            localUser.name,
                            localUser.password
                        );
                }
            }, $"username={localUser.name}");
        }
        else
        {
            // %%% Can't log out non-logged-in user.
            if(onAccountAction != null)
                onAccountAction.Invoke(Module.AccountManagerResponse.USER_NOT_FOUND, Module.DataStatus.NO_DATA, string.Empty, string.Empty);
        }
    }
}
