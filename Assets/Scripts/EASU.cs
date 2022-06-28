﻿using System.Collections;
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
        LOGGED_OUT,         // User has successfuly logged out.
        ALREADY_LOGGED_OUT, // User has already logged out or he hasn't logged in yet, while you want to log him out.
        FAIL                // Error occurred when tried to call module (it may also mean that module is returning invalid format).
    }
    /*  The AUR describes states of activity updater. Updating the last activity time is important in order
     *  to show that the user is still active and his session is not deprecated (so he can stay logged-in).
     */
    public enum ActivityReporterResponse
    {
        UPDATED,        // Last activity time (LAT) successfuly updated to current (server time).
        USER_NOT_FOUND, // Can't find user for which LAT should be renewed.
        FAIL            // AUR failed, the module may be missing, or other unknown error occurred.
    }
    /*  Checks if module on with absolute path 'modulePath' exists and is executable PHP script.
     */
    public static IEnumerator Exists(string modulePath, System.Action<bool> onFinish)
    {
        var uwr = UnityWebRequest.Get(modulePath);
        yield return uwr.SendWebRequest();
        onFinish.Invoke(uwr.error == null);
    }
    /*  Tries to resolve the path of module with name 'moduleName'. If no file named 'moduleName' is found
     *  in easu directory, it looks for it in custom modules directory (/custom).
     */
    public static IEnumerator GetNetworkPath(string moduleName, System.Action<string, bool> onFinish)
    {
        string easuModule = $"{EASU.socket}/{EASU.ROOT_DIR}/{moduleName}";
        string customModule = $"{EASU.socket}/{EASU.ROOT_DIR}/custom/{moduleName}";
        string path = string.Empty;
        yield return Exists(easuModule, (bool exists) =>
        {
            if(exists)
            {
                path = easuModule;
            }
        });
        if(path == string.Empty)
        {
            yield return Exists(customModule, (bool exists) =>
            {
                if(exists)
                {
                    path = customModule;
                }
            });
        }
        onFinish(path, path == string.Empty);
    }
}
/*  Structure used for storing data of the actualy logged-in user. It is set to
 *  new object, when user either logged in (set to new user data) or logged out (set to empty data).
 */
public struct LocalUser
{
    public string name;         // Name of the logged-in user.
    public string password;     // Password of the logged-in user.
    public bool loggedIn;       // Is current user logged in?  
    public LocalUser(string name, string password, bool loggedIn)
    {
        this.name = name;
        this.password = password;
        this.loggedIn = loggedIn;
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
/*  The core of EASU. It controls and uses everything. The class which wants to implement networking needs to
 *  derive from this class. EASU class has lots of logical virtual methods which you can override for your
 *  purposes, the connection doesn't go away during scene change - it's staticaly described, so it will remain
 *  intil application death (or user log-out).
 */
public abstract class EASU : MonoBehaviour
{
    /*  The whole directory is used by EASU; if it has invalid structure,
     *  the client-server communication will be broken.
     */
    public const string ROOT_DIR                = "easu";
    public const string CUSTOM_MODULES_DIR      = "easu/custom";            // Directory for custom php modules.
    public const string ERROR_CONTENT_FORMAT    = "[ERROR {0}] \"{1}\"";    // Format of error with message.
    public const string ERROR_FORMAT            = "ERROR {0} occurred.";    // Format of error code only.
    public const int DEFAULT_TIMEOUT            = 16;                       // Default time limit of web request (when time's up error is thrown).
    /*  Describes all possible errors that can occur in EASU, they are categorized:
     *  0 - 99: Module,
     *  100 - 199: Server,
     *  200 - 299: Client
     */
    public enum Error
    {
        MissingModule           = 0,    // Requested module doesn't exist on the server (in root and 'custom' directory).
        BrokenResponse,                 // Server responded with broken data (module can't convert).
        MissingResponse,                // Server hasn't responded with any value when it was expected.
        ConnectionError         = 100,  // Can't connect wit the socket given.
        InvalidServerStructure,         // Server has invalid structure - can't properly use its modules.
        NotConnected            = 200   // Client isn't connected to the network on which server is running.
    };
    
    public static Socket socket;                            // Combination of IPv4 address or DNS name, and opened port of http service.
    public static LocalUser localUser;                      // Structure storing the currently logged-in user data. Empty if user is not logged in.
    public static WaitForSeconds updateInterval;            // Time in seconds between next activity-updates. It's downloaded through 'sync' module.
    public static int requestTimeLimit = DEFAULT_TIMEOUT;   // The maximum time HTTP GET and POST have to receive response. After it error is thrown.
    static Stack<IEnumerator> reporterCoroutines;           // Stack of coroutines called during activity-reporting loop. All are stopped on user log-out.

    /*  Loop used for reporting the activity of current logged-in user. It calls itself with pauses of length
     *  downloaded from server (specified in server configuration).
     */
    IEnumerator _ActivityReporterLoop()
    {
        yield return _ReportActivity();
        yield return updateInterval;
        if (localUser.loggedIn)
        {
            IEnumerator reporter = _ActivityReporterLoop();
            reporterCoroutines.Push(reporter);
            yield return reporter;
        }
        yield break;
    }

    /***********************************************************************************************************************************/
    /********************************************************[ VIRTUAL METHODS ]********************************************************/
    /***********************************************************************************************************************************/
    /*  I'm invoked when there is a try to connect to a server with specified socket.
     */
    public virtual void OnConnect(string error) { }
    /*  Action called when account manager (php) sends a handlable response.
     *  It offers useful manager's response, and account data (username and password
     *  of the user, he isn't supposed to be logged in!).
     */
    public virtual void OnAccountAction(Module.AccountManagerResponse amr, Module.DataStatus data, string username, string password) { }
    /*  I'm called when a timer synchronization occurrs. I provide the update interval value.
     */
    public virtual void OnSynchronize(float interval, Module.DataStatus data) { }
    /*  This action is called after activity timer was updated to current.
     */
    public virtual void OnActivityReport(Module.ActivityReporterResponse aur, Module.DataStatus data) { }

    /***********************************************************************************************************************************/
    /*********************************************************[ CLASS METHODS ]*********************************************************/
    /***********************************************************************************************************************************/
    /*  Constructs error message from given error code and content.
     */
    public static string MakeError(Error errorCode) => string.Format(ERROR_FORMAT, (int)errorCode);
    public static string MakeError(Error errorCode, string content) => string.Format(ERROR_CONTENT_FORMAT, (int)errorCode, content);
    /*  Makes error message and then prints it into Unity console as error.
     */
    public static void PrintError(Error errorCode) => Debug.LogError(MakeError(errorCode));
    public static void PrintError(Error errorCode, string content) => Debug.LogError(MakeError(errorCode, content));
    /*  Communicates with the server using GET method. It executes 'moduleName' (from EASU root)
     *  with list of URL parameters 'urlParams' and when it's done, calls 'onFinish' action
     *  with appropriately two string values: 'response' and 'error'. 
     *  You can execute your custom modules by adding 'custom/' prefix to your module's name.
     */
    public static IEnumerator GetModule(string moduleName, System.Action<string, string> onFinish, int timeout = DEFAULT_TIMEOUT, string urlParams = "")
    {
        string correctUrlParams = urlParams.Replace(" ", string.Empty);
        string richAddress = string.Empty;
        // Make rich address pointing to the module (rich means the one with URL parameters).
        yield return Module.GetNetworkPath(moduleName, (string path, bool error) =>
        {
            if(!error)
            {
                richAddress = $"{path}?{correctUrlParams}";
            }
        });
        if (richAddress == string.Empty)
        {
            onFinish.Invoke(string.Empty, MakeError(Error.MissingModule, $"Module named {moduleName} is missing on the server."));
        }
        else
        {
            var uwr = UnityWebRequest.Get(richAddress); // Create Unity request for the module.
            uwr.timeout = timeout;
            yield return uwr.SendWebRequest();
            // Call detecting method (by this, non-numerator functions can handle downloaded data).
            onFinish.Invoke(uwr.downloadHandler.text, uwr.error);
        }
    }
    /*  Communicates with the server using GET method. It executes 'moduleName' (from modules root)
     *  with list of URL parameters (formData), and finally catches the output.
     */
    public static IEnumerator PostModule(string moduleName, System.Action<string, string> onFinish, int timeout = DEFAULT_TIMEOUT, string formParams = "")
    {
        string modulePath = string.Empty;
        yield return Module.GetNetworkPath(moduleName, (string path, bool error) =>
        {
            if(!error)
            {
                modulePath = path;
            }
        });
        if (modulePath == string.Empty)
        {
            onFinish.Invoke(string.Empty, MakeError(Error.MissingModule, $"Module named {moduleName} is missing on the server."));
        }
        else
        {
            // Try to convert string to dictionary key-value pairs.
            var formDict = new Dictionary<string, string>();
            if (formParams.Length > 0)
            {
                string[] keyValues = formParams.Contains("&") ? formParams.Replace(" ", string.Empty).Split('&') : new string[] { formParams };
                for (int i = 0; i < keyValues.Length; i++)
                {
                    string[] kvp = keyValues[i].Split('=');
                    formDict.Add(kvp[0], kvp[1]);
                }
            }
            var uwr = UnityWebRequest.Post(modulePath, formDict); // Create Unity request for the module.
            uwr.timeout = timeout;
            yield return uwr.SendWebRequest();
            // Call detecting method (by this non-numerator functions can read error status).
            onFinish.Invoke(uwr.downloadHandler.text, uwr.error);
        }
    }
    /*  Checks if 'socket' points to correct http service (the one with EASU modules implemented).
     *  If server (on that socket) responds with null error, connection was successful.
     *  If client can't connect using 'socket' and time spent on waiting for response is greater than
     *  'timeout', connection error is thrown.
     */
    IEnumerator _Connect(Socket socket, int timeout)
    {
        EASU.socket = socket;
        yield return GetModule(Module.CHECK_STRUCTURE, (string response, string error) =>
        {
            if (error == null)
            {
                int isValid;
                if(int.TryParse(response, out isValid))
                {
                    if(isValid == 1)
                    {
                        requestTimeLimit = timeout;
                        reporterCoroutines = new Stack<IEnumerator>();
                    }
                    OnConnect(isValid == 1 ? null : MakeError(Error.InvalidServerStructure, $"HTTP server under {socket} has invalid structure."));                
                }
                else
                {
                    OnConnect(MakeError(Error.BrokenResponse,
                        "Response from 'chk-structure' module is broken."
                    ));
                }
            }
            else
            {
                EASU.socket = new Socket();
                OnConnect(MakeError(Error.ConnectionError, $"Socket {socket} is not active."));
            }
        }, timeout);
    }
    /*  Simply logs user in. Copies data of account user has logged in to, checks for duplicate connections
     *  pointing to the same account (many users on single account) - prevents others from accessing
     *  already-accessed account.
     */
    IEnumerator _LogIn(string username, string password)
    {
        if (localUser.loggedIn)
        {
            // %%% User is already logged in, no data has been downloaded.
            OnAccountAction(Module.AccountManagerResponse.ALREADY_LOGGED_IN, Module.DataStatus.NO_DATA, localUser.name, localUser.password);
        }
        else
        {
            // Handle whole log-in process (literally relies on response management).
            yield return PostModule(Module.LOG_IN, (string response, string error) =>
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
                        OnAccountAction(amr, Module.DataStatus.OK, username, password);
                    }
                    else
                    {
                        // %%% Data is broken (can't parse to AMR structure).
                        OnAccountAction(
                            Module.AccountManagerResponse.FAIL,
                            Module.DataStatus.BROKEN,
                            username,
                            password
                        );
                    }
                }
                else
                {
                    // %%% No data has been downloaded due to module error.
                    OnAccountAction(
                        Module.AccountManagerResponse.FAIL,
                        Module.DataStatus.NO_DATA,
                        username,
                        password
                    );
                }
            },
                requestTimeLimit,
                $"userName = {username} & password = {password}"
            );
            if(localUser.loggedIn)
            {
                IEnumerator reporter = _ActivityReporterLoop();
                reporterCoroutines.Push(reporter);
                yield return _Synchronize();
                yield return reporter;
            }
        }
    }
    /*  Renews logged-in user's connection timer stored in the database. If it won't be renewed, then connection
     *  will be removed soon.
     */
    IEnumerator _ReportActivity()
    {
        if (localUser.loggedIn)
        {
            yield return PostModule(Module.UPDATE_ACTIVITY, (string response, string error) =>
            {
                if(error == null)
                {
                    Module.ActivityReporterResponse aur;
                    if(System.Enum.TryParse(response, out aur))
                    {
                        // %%% Response has been successfuly parsed, data is correct.
                        OnActivityReport(aur, Module.DataStatus.OK);
                    }
                    else
                    {
                        // %%% Activity is probably updated, but data is broken and thus it's probably not 100% true.
                        OnActivityReport(Module.ActivityReporterResponse.UPDATED, Module.DataStatus.BROKEN);
                    }
                }
                else
                {
                    // %%% Module error, data hasn't been downloaded beacuse of module error.
                    OnActivityReport(Module.ActivityReporterResponse.FAIL, Module.DataStatus.NO_DATA);
                }
            },
                requestTimeLimit,
                $"userName={localUser.name}"
            );
        }
        else
        {
            // %%% User isn't logged in, can't update activity of empty user. Are you nuts?.
            OnActivityReport(Module.ActivityReporterResponse.USER_NOT_FOUND, Module.DataStatus.NO_DATA);
        }
    }
    /*  Literally synchronizes local timers with server's by downloading the data.
     */
    IEnumerator _Synchronize()
    {
        yield return PostModule(Module.SYNC, (string response, string error) =>
        {
            if (error == null)
            {
                float interval;
                if (float.TryParse(response, out interval))
                {
                    updateInterval = new WaitForSeconds(interval);
                    // %%% Response has been successfuly parsed, data is correct.
                    OnSynchronize(interval, Module.DataStatus.OK);
                }
                else
                {
                    // %%% Downloaded data is broken - can't convert to float.
                    OnSynchronize(-1.0f, Module.DataStatus.BROKEN);
                }
            }
            else
            {
                // %%% Module error, data can't be downloaded due to module error.
                OnSynchronize(-1.0f, Module.DataStatus.NO_DATA);
            }
        }, requestTimeLimit);
    }
    /*  Just logs user out. Additionaly removes connection record from the database.
     */
    IEnumerator _LogOut()
    {
        if (localUser.loggedIn)
        {
            yield return PostModule(Module.LOG_OUT, (string response, string error) =>
            {
                if (error == null)
                {
                    Module.AccountManagerResponse amr;
                    if (System.Enum.TryParse(response, out amr))
                    {
                        // %%% Response is correct (proper convert), data is valid.
                        OnAccountAction(
                            amr,
                            Module.DataStatus.OK,
                            localUser.name,
                            localUser.password
                        );
                        if(amr == Module.AccountManagerResponse.LOGGED_OUT)
                        {
                            localUser = new LocalUser();
                            while(reporterCoroutines.Count > 0)
                            {
                                StopCoroutine(reporterCoroutines.Pop());
                            }
                        }
                    }
                    else
                    {
                        // %%% Response has invalid format, can't convert to AMR - data is broken.
                        OnAccountAction(
                            Module.AccountManagerResponse.FAIL,
                            Module.DataStatus.BROKEN,
                            localUser.name,
                            localUser.password
                        );
                    }
                }
                else
                {
                    // %%% Module error occurred, no data has been received.
                    OnAccountAction(
                        Module.AccountManagerResponse.FAIL,
                        Module.DataStatus.NO_DATA,
                        localUser.name,
                        localUser.password
                    );
                }
            },
                requestTimeLimit,
                $"userName={localUser.name}"
            );
        }
        else
        {
            OnAccountAction(
                Module.AccountManagerResponse.ALREADY_LOGGED_OUT,
                Module.DataStatus.NO_DATA,
                string.Empty,
                string.Empty
            );
        }
    }

    /***********************************************************************************************************************************/
    /*****************************************************[ ENUMERATOR SHORTHANDS ]*****************************************************/
    /***********************************************************************************************************************************/
    public void Connect(Socket socket, int timeout = DEFAULT_TIMEOUT) => StartCoroutine(_Connect(socket, timeout));
    public void LogIn(string username, string password) => StartCoroutine(_LogIn(username, password));
    public void ReportActivity() => StartCoroutine(_ReportActivity());
    public void Synchronize() => StartCoroutine(_Synchronize());
    public void LogOut() => StartCoroutine(_LogOut());
}
