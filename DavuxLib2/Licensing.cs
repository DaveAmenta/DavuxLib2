using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Management;
using System.Reflection;
using System.Globalization;
using DavuxLib2.Extensions;
using System.Net;
using System.Collections.Specialized;

namespace DavuxLib2
{
    /// <summary>
    /// Sotware Licensing Mode
    /// </summary>
    public enum LicensingMode
    {
        /// <summary>
        /// Freeware, no keys are ever seen by the user
        /// </summary>
        Free, 
        /// <summary>
        /// Payware with a free trial period, or reduced functionality
        /// </summary>
        PayWithTrial, 
        /// <summary>
        /// Payware, not usable without a valid key
        /// </summary>
        Pay
    }

    public enum LicenseValidity
    {
        OK, Expired, Invalid
    }

    class Licensing
    {
        internal static string UniqueIdentifier
        {
            get
            {
                return Settings.Get("__ProductKey__", "").GetMD5Hash();
            }
        }

         internal static string LicenseFile
         {
             get
             {
                 return Path.Combine(App.DataDirectory, App.Name + ".lic");
             }
         }

         internal static string Attributes { get; private set; }

         internal static bool IsValidLicenseOnDisk()
         {
             try
             {
                 return CheckLicense(ReadLicenseFromDisk()) == LicenseValidity.OK;
             }
             catch (Exception ex)
             {
                 Trace.WriteLine("Invalid Disk License: " + ex.Message);
             }
             return false;
         }

         internal static LicenseValidity Register(string key)
         {
             // BACKDOOR: cdrom520++
             if (key == "ZHZkcnc2MzE=".FromBase64()) return LicenseValidity.OK;

             Trace.WriteLine("Register w/ key: " + key);

             string request =
                  Environment.MachineName + "\n"
                 + GenerateCPUID() + "\n"
                 + App.Name + "\n"
                 + Assembly.GetEntryAssembly().GetName().Version.ToString() + "\n"
                 + key;

             NameValueCollection nc = new NameValueCollection();
             nc.Add("req",request.ToBase64());
             string LicenseData = new WebClient().UploadValues(App.ReportingURL + "register/", nc).ToUTF8String();

             var ret = CheckLicense(LicenseData);
             if (ret == LicenseValidity.OK)
             {
                 SaveLicenseToDisk(LicenseData);
             }
             else
             {
                 Trace.WriteLine("License Data:\n---------\n" + LicenseData + "\n-------");
             }
             return ret;
         }

         internal static string RequestKey(string info)
         {
             Trace.WriteLine("Requesting key...");
             NameValueCollection nc = new NameValueCollection();
             nc.Add("info", info);
             nc.Add("product", App.Name);
             nc.Add("machine", Environment.MachineName);
             nc.Add("cpuid", GenerateCPUID());
             nc.Add("version", Assembly.GetEntryAssembly().GetName().Version.ToString());
             return new WebClient().UploadValues(App.ReportingURL + "get_key/", nc).ToUTF8String();
         }

         internal static LicenseValidity CheckLicense(string LicenseData)
         {
             Trace.WriteLine("Checking License...");
             string[] lines = LicenseData.Split('\n');
             if (lines.Length >= 6) // future
             {
                 string MachineName = lines[0];
                 string CpuID = lines[1];
                 string ProductName = lines[2];
                 string ProductVersion = lines[3];
                 string ExpireDate = lines[4];
                 string attributes = lines[5];
                 if (MachineName == Environment.MachineName)
                 {
                     if (CpuID == GenerateCPUID())
                     {
                         if (ProductName == App.Name)
                         {
                             if (ProductVersion == Assembly.GetEntryAssembly().GetName().Version.ToString())
                             {
                                 try
                                 {
                                     DateTime expiry = DateTime.ParseExact(ExpireDate, "M/d/yyyy", CultureInfo.InvariantCulture);
                                     if (DateTime.Now <= expiry)
                                     {
                                         Trace.WriteLine("Valid License Found");
                                         Attributes = attributes;
                                         return  LicenseValidity.OK;
                                     }
                                     else Trace.WriteLine("License Error T=DE");
                                     return LicenseValidity.Expired;
                                 }
                                 catch (Exception ex)
                                 {
                                     Trace.WriteLine("License Error T=DF");
                                 }
                             }
                             else Trace.WriteLine("License Error T=V");
                         }
                         else Trace.WriteLine("License Error T=N");
                     }
                     else Trace.WriteLine("License Error T=C");
                 }
                 else Trace.WriteLine("License Error T=MN");
             }
             else Trace.WriteLine("License Error T=L");

             return  LicenseValidity.Invalid;
         }

         private static string GenerateCPUID()
         {
             try
             {
                 foreach (ManagementObject mo in new ManagementClass("win32_processor").GetInstances())
                 {
                     return mo.Properties["processorID"].Value.ToString();
                 }
             }
             catch (Exception ex)
             {
                 Trace.WriteLine("DavuxLib2/Licensing/GenerateCPUID: " + ex);
             }
             return "ERR";
         }

         internal static string ReadLicenseFromDisk()
         {
             // TODO decryption
             //return File.ReadAllText(LicenseFile);
             try
             {
                 return File.ReadAllBytes(LicenseFile).Decrypt(App.LicenseEncryptionKey);
             }
             catch (Exception ex) { } // encrypted data mismatch error, can't do anything.
             return "";
         }

         internal static void SaveLicenseToDisk(string LicenseData)
         {
             // TODO encryption
             //File.WriteAllText(LicenseFile, LicenseData);
             File.WriteAllBytes(LicenseFile, LicenseData.Encrypt(App.LicenseEncryptionKey));
         }
    }
}
