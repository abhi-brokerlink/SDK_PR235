using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;

namespace EpicIntegrator
{
    public class EmployeeService
    {

        static string AuthenticationKey;
        static string DataBase;
        static string ConnectionString = ConfigurationManager.ConnectionStrings["EpicBDE"].ConnectionString;
        CBLServiceReference.MessageHeader oMessageHeader;

        public EmployeeService()
        {
            AuthenticationKey = ConfigurationManager.AppSettings["AppliedSDKKey"];
            DataBase = ConfigurationManager.AppSettings["AppliedSDKDatabase"];
            oMessageHeader = new CBLServiceReference.MessageHeader();
            oMessageHeader.AuthenticationKey = AuthenticationKey;
            oMessageHeader.DatabaseName = DataBase;
        }

        public List<EpicIntegrator.Employee> GetEmployeeSource(string query)
        {
            SqlConnection conn = new SqlConnection(ConnectionString);
            List<EpicIntegrator.Employee> emp = new List<EpicIntegrator.Employee>();

            SqlCommand command = new SqlCommand(query, conn);
            conn.Open();

            SqlDataReader rdr = command.ExecuteReader();

            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    emp.Add(new EpicIntegrator.Employee()
                    {
                        EmployeeID = Int32.Parse(rdr["EmployeeID"].ToString()),
                        EmailAddress = rdr["EmailAddress"].ToString(),
                        MobileNumber = rdr["MobileNumber"].ToString()
                    });
                }
            }
            else
            {
                return null;
            }
            rdr.Close();
            return emp;
        }

        public CBLServiceReference.Client GetEmployeeInfo(string EmployeeID)
        {
            CBLServiceReference.EpicSDK_2017_02Client SDK = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.Employee2 oEmployee = new CBLServiceReference.Employee2();
            CBLServiceReference.Get_EmployeeResponse1 oGetEmployee = new CBLServiceReference.Get_EmployeeResponse1();

            try
            {
                oGetEmployee.Get_EmployeeResult = SDK.Get_Employee(oMessageHeader, EmployeeID, string.Empty, CBLServiceReference.EmployeeGetType.EmployeeID, true, false, 0);
                SDK.Close();

                oEmployee = oGetEmployee.Get_EmployeeResult.Employees.First();
                Console.WriteLine(oEmployee.AccountName);
                Console.WriteLine(oEmployee.AccountValue.BusinessJobTitle);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();
            return null;
        }

        public string UpdateEmployeeInfo(string EmployeeID, string JobTitle)
        {
            CBLServiceReference.EpicSDK_2017_02Client SDK = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.Employee2 oEmployee = new CBLServiceReference.Employee2();
            CBLServiceReference.Get_EmployeeResponse1 oGetEmployee = new CBLServiceReference.Get_EmployeeResponse1();

            try
            {
                oGetEmployee.Get_EmployeeResult = SDK.Get_Employee(oMessageHeader, EmployeeID, string.Empty, CBLServiceReference.EmployeeGetType.EmployeeID, true, false, 0);

                oEmployee = oGetEmployee.Get_EmployeeResult.Employees.First();
                oEmployee.AccountValue.BusinessJobTitle = JobTitle;
                SDK.Update_Employee(oMessageHeader, oEmployee);
                Console.WriteLine(oEmployee.AccountValue.BusinessJobTitle);
                SDK.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();
            return null;
        }

    }

    public class Employee
    {
        public int EmployeeID { get; set; }
        public string EmailAddress { get; set; }
        public string MobileNumber { get; set; }
        public string MobileExtension { get; set; }
        public string BusinessNumber { get; set; }
        public string BusinessExtension { get; set; }
        public AccountStructureItem AccountStructure { get; set; }
    }

    public class AccountStructureItem
    {
        public string AgencyCode { get; set; }
        public string BranchCode { get; set; }
        public string DepartmentCode { get; set; }
        public int Flag { get; set; }
        public string ProfitCenterCode { get; set; }

    }
}
