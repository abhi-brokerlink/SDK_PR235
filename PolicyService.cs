using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace EpicIntegrator
{
    public class PolicyService
    {
        static string AuthenticationKey;
        static string DataBase;
        static string ConnectionString = ConfigurationManager.ConnectionStrings["CBLReporting"].ConnectionString;
        CBLServiceReference.MessageHeader oMessageHeader;
        int IPolicyID;
        
        

        public PolicyService()
        {
            AuthenticationKey = ConfigurationManager.AppSettings["AppliedSDKKey"];
            DataBase = ConfigurationManager.AppSettings["AppliedSDKDatabase"];
            oMessageHeader = new CBLServiceReference.MessageHeader();
            oMessageHeader.AuthenticationKey = AuthenticationKey;
            oMessageHeader.DatabaseName = DataBase;
        }

        //public List<EpicIntegrator.Policy> GetPolicyFields(int CustID)
        //{

        //}

        public void TestPolicyConnect (int OldPolID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.Policy oPol = new CBLServiceReference.Policy();
            oPolicyFilter.PolicyID = OldPolID;
            oPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, oPolicyFilter, 0);
            oPol = oPolicyResult.Policies[0];

            Console.WriteLine(oPol.PolicyID);
            Console.WriteLine(oPol.PolicyNumber);

            Console.WriteLine(oPol.Description);
            //string newDesc = oPol.Description + "_x";

            oPol.Description = "Commercial General Liability";
            Console.WriteLine(oPol.Description);
            EpicSDKClient.Update_Policy(oMessageHeader, oPol);

        }


        public List<EpicIntegrator.Policy> GetPolicySQL(string polNum)
        {
            SqlConnection conn = new SqlConnection(ConnectionString);
            List<EpicIntegrator.Policy> pols = new List<EpicIntegrator.Policy>();
            List<EpicIntegrator.Policy> polFirst = new List<EpicIntegrator.Policy>();

            string SQL = System.IO.File.ReadAllText(@"C:\Users\Abhishek\Documents\Abhi _ IMP\Sep29\SQLpolDetails.txt");
            string query = SQL + polNum+"';";
            //Console.WriteLine(query);
            SqlCommand command = new SqlCommand(query, conn);
            conn.Open();
            SqlDataReader rdr = command.ExecuteReader();
            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    pols.Add(new EpicIntegrator.Policy()
                    {
                        AccountID = Convert.ToInt32(rdr["UniqEntity"].ToString()),
                        EffectiveDate = rdr["EffectiveDate"] as DateTime? ?? (DateTime?)null,
                        ExpirationDate = rdr["ExpirationDate"] as DateTime? ?? (DateTime?)null,
                        PolicyNumber = rdr["PolicyNumber"].ToString(),
                        CdPolicyLineTypeCode = rdr["CdPolicyLineTypeCode"].ToString(),
                        DescriptionOf = rdr["DescriptionOf"].ToString(),
                        CdLineStatusCode = rdr["CdLineStatusCode"].ToString(),
                        AgencyCode = rdr["AgencyCode"].ToString(),
                        DepartmentCode = rdr["DepartmentCode"].ToString(),
                        BranchCode = rdr["BranchCode"].ToString(),
                        ProfitCenterCode = rdr["ProfitCenterCode"].ToString(),
                        CdStateCodeIssuing = rdr["CdStateCodeIssuing"].ToString(),
                        ICOLookupCode = rdr["LookupCode"].ToString(),

                    });
                }
            }
            else
            {
                return null;
            }
            rdr.Close();
            polFirst.Add(pols[0]);
            return polFirst;

        }

        public CBLServiceReference.Policy GetPolicy(int CustID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.Policy pol = new CBLServiceReference.Policy();

            oPolicyFilter.ClientID = CustID;
            //oPolicyFilter.ExpirationDateBegins > DateTime.Now.Date;

            oPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, oPolicyFilter, 0);

            Console.WriteLine(oPolicyResult.Policies.Length);
            Console.WriteLine(oPolicyResult.Policies[0]);
            Console.WriteLine(oPolicyResult.Policies[1]);
            Console.WriteLine(oPolicyResult.Policies[2]);
            pol = oPolicyResult.Policies[0];

            return pol;

        }

        public int InsertPolicy(int CustID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.Policy pol = new CBLServiceReference.Policy();

            pol.AccountID = CustID;
            pol.EffectiveDate = new DateTime(2020, 10, 01);
            pol.ExpirationDate = new DateTime(2021, 09, 30);
            pol.IssuingCompanyLookupCode = "SASKA1";
            pol.StatusCode = "NEW";
            pol.PolicyTypeCode = "BLIA";
            pol.Description = "Commercial General Liability/PAL/Special Events";
            pol.LineTypeCode = "BLIA";
            pol.IssuingLocationCode = "ON";
            pol.AgencyCode = "66";
            pol.BranchCode = "HIG";
            pol.DepartmentCode = "CL";
            pol.ProfitCenterCode = "CIPU";
            pol.PolicyNumber = "TEST123AC";
            IPolicyID = EpicSDKClient.Insert_Policy(oMessageHeader, pol);
            Console.WriteLine(IPolicyID);
            //EpicSDKClient.Close();
            return IPolicyID;


        }

        public int InsertPolicySQL(string polNum)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.Policy pol = new CBLServiceReference.Policy();

            SqlConnection conn = new SqlConnection(ConnectionString);
            //List<EpicIntegrator.Policy> pols = new List<EpicIntegrator.Policy>();
            //List<EpicIntegrator.Policy> polFirst = new List<EpicIntegrator.Policy>();


            string SQL = System.IO.File.ReadAllText(@"C:\Users\Abhishek\Documents\Abhi _ IMP\Sep29\SQLpolDetails.txt");
            string query = SQL + polNum + "';";
            //Console.WriteLine(query);
            SqlCommand command = new SqlCommand(query, conn);
            conn.Open();
            SqlDataReader rdr = command.ExecuteReader();
            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    pol.AccountID = Convert.ToInt32(rdr["UniqEntity"].ToString());
                    //pol.EffectiveDate = new DateTime(2000, 01, 01);
                    //pol.ExpirationDate = new DateTime(2000, 01, 01);

                    pol.EffectiveDate = Convert.ToDateTime(rdr["EffectiveDate"]);
                    pol.ExpirationDate = Convert.ToDateTime(rdr["ExpirationDate"]);
                    pol.IssuingCompanyLookupCode = rdr["LookupCode"].ToString();
                    pol.StatusCode = rdr["CdLineStatusCode"].ToString();
                    pol.PolicyTypeCode = "BLIA";
                    pol.Description = "Commercial General Liability/PAL/Special Events";
                    pol.LineTypeCode = "BLIA";
                    pol.IssuingLocationCode = rdr["CdStateCodeIssuing"].ToString();
                    pol.AgencyCode = rdr["AgencyCode"].ToString();
                    pol.BranchCode = rdr["BranchCode"].ToString();
                    pol.DepartmentCode = rdr["DepartmentCode"].ToString();
                    pol.ProfitCenterCode = rdr["ProfitCenterCode"].ToString();
                    pol.PolicyNumber = rdr["PolicyNumber"].ToString() + "SDK";
                    
                    IPolicyID = EpicSDKClient.Insert_Policy(oMessageHeader, pol);
                    
                }
            }
            else
            {
                IPolicyID = 0;
                
            }
            rdr.Close();
            return IPolicyID;
            
        }
            





    }

    public class Policy
    {
        public int AccountID { get; set; }
        public int IPolicyID { get; set; }
        public int LineID { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string PolicyNumber { get; set; }
        public string CdPolicyLineTypeCode { get; set; }
        public string DescriptionOf { get; set; }
        public string CdLineStatusCode { get; set; }
        public string AgencyCode { get; set; }
        public string DepartmentCode { get; set; }
        public string BranchCode { get; set; }
        public string ProfitCenterCode { get; set; }
        public string CdStateCodeIssuing { get; set; }
        public string ICOLookupCode { get; set; }



    }
}
