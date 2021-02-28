using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EpicIntegrator
{
    public class Helper
    {
        public static string ReadSqlFile(string sqlFile)
        {
            string sql = string.Empty;
            try
            {
                sql = System.IO.File.ReadAllText(@sqlFile);
            }
            catch
            {
                sql = "SELECT 0";
            }
            return sql;
        }
    }
}
