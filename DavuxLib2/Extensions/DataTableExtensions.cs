using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace DavuxLib2.Extensions
{
    public static class DataTableExtensions
    {
        public static String ToCSV(this DataTable dt)
        {
            StringBuilder sb = new StringBuilder();

            for (int x = 0; x < dt.Columns.Count; x++)
            {
                if (x != 0)
                    sb.Append(",");
                sb.Append(CheckCommansInField(dt.Columns[x].ColumnName));
            }
            sb.AppendLine();
            foreach (DataRow row in dt.Rows)
            {
                for (int x = 0; x < dt.Columns.Count; x++)
                {
                    if (x != 0)
                        sb.Append(",");
                    sb.Append(CheckCommansInField(row[dt.Columns[x]].ToString()));
                }

                sb.AppendLine();
            }
            return sb.ToString();
        }

        static string CheckCommansInField(string field)
        {
            if (field.IndexOf(',') > -1)
            {
                field = "\"" + field + "\"";
            }
            return field;
        }
    }
}
