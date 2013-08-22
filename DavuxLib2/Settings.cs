using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using DavuxLib2.Extensions;
using System.Security;
using System.Security.Cryptography;

namespace DavuxLib2
{
    /// <summary>
    /// Application Settings
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Represents a single setting
        /// </summary>
        public struct ConfValue
        {
            public string Key { get; set; }
            public string Value { get; set; }
        }

        static Settings()
        {
            // this hack sucks.  required for GvAutoResponder
            CaseInsensitive = true;
        }

        /// <summary>
        /// Save settings to disk each time a value is modified
        /// </summary>
        public static bool SaveOnModify = true;

        /// <summary>
        /// The filename appended to App.DataDirectory for storing settings.
        /// </summary>
        public static string FileName = "settings.xml";

        /// <summary>
        /// Raw settings data, do not introduce null values into this collection.
        /// </summary>
        public static Dictionary<string, string> Data = new Dictionary<string, string>();

        #region Disk Operations (Init: Load, Save: Save)

        internal static void Init()
        {
            try
            {
                var settings = File.ReadAllText(FullPath).FromXML<List<ConfValue>>();
                foreach (var kv in settings)
                {
                    Data.Add(kv.Key, kv.Value);
                }
                Initialized = true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("DavuxLib2/Settings/ Can't load: " + ex.Message);
            }
            
        }

        public static void Save()
        {
            try
            {
                // IDictionary cannot be serialized, convert to a List first
                List<ConfValue> settings = new List<ConfValue>();

                foreach (var entry in Data)
                {
                    settings.Add( new ConfValue { 
                        Key = entry.Key, Value = entry.Value });
                }
                File.WriteAllText(FullPath, settings.ToXml());
            }
            catch (Exception ex)
            {
                Trace.WriteLine("DavuxLib2/Settings/ Can't Save: " + ex);
            }
        }

        internal static string FullPath
        {
            get { return Path.Combine(App.DataDirectory, FileName); }
        }

        #endregion

        #region Settings Getters (string, bool, int, Dictionary<string,string>, T)

        public static string Get(string key)
        {
            return Get(key, "");
        }

        public static string Get(string key, string def)
        {
            return Get<string>(key.ToLower(), def);
        }

        public static int Get(string key, int def)
        {
            int val = def;
            return int.TryParse(Get(key), out val) ? val : def;
        }

        public static bool Get(string key, bool def)
        {
            return Get(key, def ? "YES" : "NO") == "YES" ? true : false;
        }

        public static Dictionary<string, string> Get(string key, Dictionary<string, string> def)
        {
            List<ConfValue> t = Get<List<ConfValue>>(key, new List<ConfValue>());
            if (t.Count > 0)
            {
                Dictionary<string, string> ret = new Dictionary<string, string>();
                foreach (ConfValue kvp in t)
                {
                    if (kvp.Key != null)
                    {
                        if (!ret.Keys.Contains(kvp.Key))
                        {
                            ret.Add(kvp.Key, kvp.Value);
                        }
                    }
                }
                return ret;
            }
            return def;
        }

        public static T Get<T>(string key, T def)
        {
            if (CaseInsensitive)
            {
                key = key.ToLower();
            }
            T o = def;
            string value;
            if (Data.TryGetValue(key, out value))
            {
                if (typeof(T) == typeof(string))
                {
                    o = (T)(object)value;
                }
                else
                {
                    try
                    {
                        o = value.FromBase64().FromXML<T>();
                    }
                    catch (Exception ex)
                    {
                        if (o == null)
                        {
                            Trace.WriteLine("DavuxLib2/Settings/Get<?> Can't Save: " + ex);
                        }
                        else
                        {
                            Trace.WriteLine("DavuxLib2/Settings/Get<" + o.GetType() + "> Can't Save: " + ex);
                        }
                    }
                }
            }
            else // if the value doesn't exist, create/set it.
            {
                Set<T>(key, def);
            }
            return o;
        }

        #endregion


        public static SecureString GetSecure(string key, string def, string pw)
        {
            string str = Get(key, "");
            if (string.IsNullOrEmpty(str)) return def.ToSecureString();

            try
            {
                byte[] bytes = Convert.FromBase64String(str);
                return bytes.Decrypt(pw).ToSecureString();
            }
            catch (FormatException)
            {
                // is it a regular string? (not base64)
                return str.ToSecureString();
            }
            catch (CryptographicException)
            {
                // is it a regular string? (invalid length)
                return str.ToSecureString();
            }
            catch (Exception)
            {
                return def.ToSecureString();
            }
        }

        public static void SetSecure(string key, SecureString value, string pw)
        {
            Set(key, Convert.ToBase64String(value.ToStringUnSecure().Encrypt(pw)));
        }

        #region Settings Setters (string, bool, int, Dictionary<string,string>, T)

        public static void Set(string key, bool value)
        {
            Set(key, value ? "YES" : "NO");
        }

        public static void Set(string key, bool? value)
        {
            Set(key, value == true ? "YES" : "NO");
        }

        public static void Set(string key, int value)
        {
            Set(key, value.ToString());
        }

        public static void Set(string key, string value)
        {
            Set<string>(key.ToLower(), value);
        }

        public static void Set(string key, Dictionary<string, string> value)
        {
            List<ConfValue> ret = new List<ConfValue>();
            foreach (string k in value.Keys)
            {
                ret.Add(new ConfValue { Key = k, Value = value[k] });
            }
            Set<List<ConfValue>>(key, ret);
        }

        /// <summary>
        /// Overwrite the current value for key with an XML-Serialized copy of value.
        /// </summary>
        /// <typeparam name="T">A type which may be XML serialized.</typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set<T>(string key, T value)
        {
            if (CaseInsensitive)
            {
                key = key.ToLower();
            }
            if (Data.Keys.Contains(key))
            {
                // update existing key
                if (typeof(T) == typeof(string))
                {
                    Data[key] = value.ToString();
                }
                else
                {
                    Data[key] = value.ToXml().ToBase64();
                }
            }
            else
            {
                // create new entry
                if (typeof(T) == typeof(string))
                {
                    Data.Add(key, value.ToString());
                }
                else
                {
                    Data.Add(key, value.ToXml().ToBase64());
                }
            }
            if (SaveOnModify) Save();
        }

        #endregion

        public static int Increment(string key)
        {
            int val = Get(key, 0);
            val++;
            Set(key, val);
            return val;
        }

        public static bool Initialized { get; private set; }

        // this is a hack for GVAutoResponder
        // i don't think i can ever phase it out, since it would break upgrades
        public static bool CaseInsensitive { get; set; }
    }
}
