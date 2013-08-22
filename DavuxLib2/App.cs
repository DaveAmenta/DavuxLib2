using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Deployment.Application;
using System.Windows.Forms;

namespace DavuxLib2
{
    public class App
    {
        /// <summary>
        /// Full path to the directory where application data may be stored.  This location is read/write, unless App.Init fails.
        /// </summary>
        public static string DataDirectory { get; private set; }

        /// <summary>
        /// Short name of the application.
        /// </summary>
        public static string Name { get; private set; }

        /// <summary>
        /// Automatically incrementing Session ID, updated each time the user runs the application.
        /// </summary>
        public static int SessionID { get; private set; }

        /// <summary>
        /// Base URL for reporting and activation services.  Must end with a trailing /.
        /// </summary>
        public static string ReportingURL = "http://daveamenta.com/lic/";

        /// <summary>
        /// Support URL (Help And Support links on default dialogs)
        /// </summary>
        public static string SupportURL = "http://daveamenta.com/";

        /// <summary>
        /// Full path to the log file for this session.
        /// </summary>
        public static string LogFile
        {
            get { return Logging.FullPath; }
        }

        /// <summary>
        /// Attributes loaded from the license.
        /// </summary>
        public static string[] LicensedAttributes
        {
            get { return Licensing.Attributes == null ? new string[] {""} : Licensing.Attributes.Split(' '); }
        }

        public static string LicenseEncryptionKey { get; set; }

        /// <summary>
        /// Request for product key from the user.
        /// </summary>
        /// <param name="ExpiredLicense">The trial license for this user has expired.</param>
        /// <param name="mode">Freeware/Payware mode in use for this application.</param>
        /// <returns>A product key, or an empty string to signify a trial key</returns>
        public delegate string ProductKeyRequired(bool ExpiredLicense, LicensingMode mode);

        /// <summary>
        /// Check the EULA for this application.  
        /// </summary>
        /// <param name="mode">Licensing mode in use.</param>
        /// <returns>True if the user accepts the EULA</returns>
        public delegate bool EULACheck(LicensingMode mode);

        /// <summary>
        /// Only called when the EULA has never been accepted.
        /// If not attached, the default EULA check handler will be used.
        /// </summary>
        public static event EULACheck EULACheckRequired;

        /// <summary>
        /// Called when a product key is required from the user.  
        /// If not attached, the deault product key handler will be used.
        /// </summary>
        public static event ProductKeyRequired KeyRequiredFromUser;


        /// <summary>
        /// Initialize the Application Settings, Logging and Crash Reporting
        /// </summary>
        /// <param name="Name">The name of the application</param>
        public static void Init(string Name)
        {
            App.Name = Name;

            if (string.IsNullOrEmpty(LicenseEncryptionKey)) LicenseEncryptionKey = "DEFAULT_KEY_500400800e";

            if (File.Exists(Settings.FileName))
            {
                DataDirectory = Environment.CurrentDirectory;
            }
            else
            {
                DataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name);
                if (!Directory.Exists(DataDirectory))
                {
                    Directory.CreateDirectory(DataDirectory);
                }
            }

            Settings.Init();
            SessionID = Settings.Increment("__SessionID__");
            Logging.Init();
        }

        #region Single Instance Application (Mutex) Methods

        private static Mutex SingleInstanceMutex = null;

        /// <summary>
        /// Warn if this application runs more than once.  A message will be presented with OK and Cancel buttons.
        /// </summary>
        /// <returns>True if the application is already running.</returns>
        public static bool WarnIfRunning()
        {
            if (IsAppAlreadyRunning())
            {
                System.Windows.Forms.Application.EnableVisualStyles();
                var ret = System.Windows.Forms.MessageBox.Show(
                    Name + " is already running.  Are you sure you would like to start another instance?",
                    "Application is already running",
                    System.Windows.Forms.MessageBoxButtons.OKCancel,
                    System.Windows.Forms.MessageBoxIcon.Warning);
                if (ret == System.Windows.Forms.DialogResult.Cancel)
                {
                    Environment.Exit(0);
                }
                return true;
            }
            return false;
        }



        /// <summary>
        /// Use a mutex to determine if this application is already running.
        /// </summary>
        /// <returns>True if the application is already running</returns>
        public static bool IsAppAlreadyRunning()
        {
            try
            {
                SingleInstanceMutex = Mutex.OpenExisting(Name + (Debugger.IsAttached ? "-Debug" : ""));
                return true;
            }
            catch (Exception)
            {
                SingleInstanceMutex = new Mutex(false, Name + (Debugger.IsAttached ? "-Debug" : ""));
            }
            finally
            {
                AppDomain.CurrentDomain.ProcessExit += (_, __) =>
                {
                    try
                    {
                        if (SingleInstanceMutex != null)
                            SingleInstanceMutex.ReleaseMutex();
                    }
                    catch (Exception)
                    {
                        // no recovery is required or possible.
                    }
                    finally
                    {
                        SingleInstanceMutex = null;
                    }
                };
            }
            return false;
        }

        #endregion

        #region Software Activation
        /// <summary>
        /// Main Registration routine.  Checks for proper licensing, and attemps to 
        /// register if unable to do so.
        /// </summary>
        /// <param name="mode">Licensing scheme for this product</param>
        /// <param name="ProductKeyRequired">Called when a product key is needed.  Return the product key, or an empty string to indicate trial key.  Boolean parameter is true if the trial license has expired</param>
        /// <example>AppAuthorizationException</example>
        /// <returns>True if the user is licensed</returns>
        public static LicenseValidity IsAllowedToExecute(LicensingMode mode)
        {
            LicenseValidity ret = LicenseValidity.Invalid;
            // check for license file on disk
            ret = Licensing.CheckLicense(Licensing.ReadLicenseFromDisk());
            if (ret == LicenseValidity.OK) return ret;

            // try and register with key on file
            string key = Settings.Get("__ProductKey__", "");

            // if there is a key on file, register with it 
            if (!string.IsNullOrEmpty(key))
            {
                ret = Licensing.Register(key);
                if (ret == LicenseValidity.OK) return ret;
            }

            // user does not have a valid key, get one
            string type = "free";
            if (mode == LicensingMode.Pay || mode == LicensingMode.PayWithTrial)
            {
                // get a key, param is TRUE if the old key is expired
                // if the return key is blank, get a trial key
                if (KeyRequiredFromUser != null)
                {
                    key = KeyRequiredFromUser(ret == LicenseValidity.Expired, mode);
                }
                else
                {
                    key = DefaultProductKeyRequried(ret == LicenseValidity.Expired, mode);
                }
                type = "trial";
            }

            // try and request a key (trial or free)
            if (string.IsNullOrEmpty(key))
            {
                key = Licensing.RequestKey(type);
            }

            // now that the user entered or downloaded a key, register
            ret = Licensing.Register(key);

            // save the key, even if it's expired.
            Settings.Set("__ProductKey__", key);
            return ret;
        }

        public static bool CheckEULA(LicensingMode mode)
        {
            bool accepted = Settings.Get("__EULA__", false);
            if (!accepted)
            {
                if (EULACheckRequired != null)
                {
                    accepted = EULACheckRequired(mode);
                }
                else
                {
                    accepted = DefaultEULACheck(mode);
                }
                Settings.Set("__EULA__", accepted);
            }
            return accepted;
        }

        private static bool DefaultEULACheck(LicensingMode mode)
        {
            bool accepted = false;
            Thread t = new Thread(() =>
            {
                Windows.EULAWindow elw = new Windows.EULAWindow(mode);
                elw.Show();
                System.Windows.Threading.Dispatcher.Run();
                accepted = elw.Accepted;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            return accepted;
        }

        private static string DefaultProductKeyRequried(bool didExpire, LicensingMode mode)
        {
            string key = "";
            Thread t = new Thread(() =>
            {
                Windows.ProductKeyWindow pkw = new Windows.ProductKeyWindow(didExpire, mode);
                pkw.Show();
                System.Windows.Threading.Dispatcher.Run();
                key = pkw.Key;
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            return key;
        }

        #endregion

        #region ClickOnce update

        /// <summary>
        /// Check for ClickOnce updates and return control immediately
        /// </summary>
        public static void CheckForUpdatesAsync()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                CheckForUpdates();
            });
        }

        /// <summary>
        /// Check for ClickOnce updates, and present the user a dialog if needed.
        /// </summary>
        public static void CheckForUpdates()
        {
            try
            {
                UpdateCheckInfo info = null;

                if (ApplicationDeployment.IsNetworkDeployed)
                {
                    ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;

                    try
                    {
                        info = ad.CheckForDetailedUpdate();
                    }
                    catch (DeploymentDownloadException dde)
                    {
                        MessageBox.Show("The new version of the application cannot be downloaded at this time. \n\nPlease check your network connection, or try again later. Error: " + dde.Message);
                        return;
                    }
                    catch (InvalidDeploymentException ide)
                    {
                        MessageBox.Show("Cannot check for a new version of the application. The ClickOnce deployment is corrupt. Please redeploy the application and try again. Error: " + ide.Message);
                        return;
                    }
                    catch (InvalidOperationException ioe)
                    {
                        MessageBox.Show("This application cannot be updated. It is likely not a ClickOnce application. Error: " + ioe.Message);
                        return;
                    }

                    if (info.UpdateAvailable)
                    {
                        Boolean doUpdate = true;

                        if (!info.IsUpdateRequired)
                        {
                            DialogResult dr = MessageBox.Show("An update is available. Would you like to update the application now?", "Update Available", MessageBoxButtons.OKCancel);
                            if (!(DialogResult.OK == dr))
                            {
                                doUpdate = false;
                            }
                        }
                        else
                        {
                            // Display a message that the app MUST reboot. Display the minimum required version.
                            MessageBox.Show("This application has detected a mandatory update from your current " +
                                "version to version " + info.MinimumRequiredVersion.ToString() +
                                ". The application will now install the update and restart.",
                                "Update Available", MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                        }

                        if (doUpdate)
                        {
                            try
                            {
                                ad.Update();
                                MessageBox.Show("The application has been upgraded, and will now restart.");
                                Application.Restart();
                                Environment.Exit(0);
                            }
                            catch (DeploymentDownloadException dde)
                            {
                                MessageBox.Show("Cannot install the latest version of the application. \n\nPlease check your network connection, or try again later. Error: " + dde);
                                return;
                            }
                        }
                        else
                        {
                            Trace.WriteLine("Not Updating");
                        }
                    }
                    else
                    {
                        Trace.WriteLine("No ClickOnce update");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("DavuxLib2/App/CheckForUpdatesAsync Can't update: " + ex);
            }
        }

        #endregion

        #region Crash Reporting

        /// <summary>
        /// Search the DataDirectory for crash reports, and upload them to the crash service
        /// at the ReportingURL
        /// </summary>
        public static void SubmitCrashReports()
        {
            try
            {
                foreach (var file in Directory.GetFiles(DataDirectory))
                {
                    string name = Path.GetFileName(file);
                    if (name.StartsWith(Logging.CrashPrefix))
                    {
                        SubmitCrashReport(file);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("DavuxLib2/App/SubmitCrashReports Can't submit: " + ex);
            }
        }

        internal static void SubmitCrashReport(string file)
        {
            Trace.WriteLine("Submitting Crash Report: " + file);
            try
            {
                ReportingConnector.UploadCrashReport(File.ReadAllText(file));
                File.Delete(file);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("DavuxLib2/App/SubmitCrashReport Can't submit: " + ex);
            }
        }

        #endregion
    }

    /// <summary>
    /// Unable to access the activation server
    /// </summary>
    public class AppAuthorizationException : ApplicationException
    {
        public AppAuthorizationException(string message, Exception innerException)
            : base(message, innerException)
        {}
    }
}
