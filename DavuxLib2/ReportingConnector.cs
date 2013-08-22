using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Collections.Specialized;
using DavuxLib2.Extensions;
using System.Diagnostics;

namespace DavuxLib2
{
    class ReportingConnector
    {
        internal static void UploadCrashReport(string reportText)
        {
            UploadReport("crash/", reportText);
        }

        internal static void UploadUsageReport(string reportText)
        {
            UploadReport("usage/", reportText);
        }

        private static void UploadReport(string service, string reportText)
        {
            NameValueCollection nc = new NameValueCollection();

            nc.Add("product", App.Name);
            nc.Add("report", reportText.ToBase64());
            nc.Add("id", Licensing.UniqueIdentifier);
            string ret = new WebClient().UploadValues(
                App.ReportingURL + service, nc).ToUTF8String();
            Trace.WriteLine(service + " Report Response: " + ret);
        }
    }
}
