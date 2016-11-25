using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace DefaultActivities
{
    public class FileRegisterList
    {
        public static readonly string TypeName = "dbo.FileList";
        public static readonly DataColumn[] Columns = {
           new DataColumn("Name",typeof(String)),
           new DataColumn("Path",typeof(String)),
           new DataColumn("Source",typeof(String))
        };

        public string Name { get; set; }
        public string Path { get; set; }
        public string Source { get; set; }


        public static DataTable ToDataTable(IEnumerable<string> files, string source)
        {
            DataTable dt = new DataTable(TypeName);
            dt.Columns.AddRange(Columns);
            foreach (var file in files)
            {
                var row = dt.NewRow();
                row[0] = System.IO.Path.GetFileName(file);
                row[1] = file;
                row[2] = source;

                dt.Rows.Add(row);
            }
            return dt;

        }
    }

}
