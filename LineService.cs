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
    public class LineService
    {

        static string AuthenticationKey;
        static string DataBase;
        static string ConnectionString = ConfigurationManager.ConnectionStrings["EpicBDE"].ConnectionString;
        CBLServiceReference.MessageHeader oMessageHeader;

        public LineService()
        {
            AuthenticationKey = ConfigurationManager.AppSettings["AppliedSDKKey"];
            DataBase = ConfigurationManager.AppSettings["AppliedSDKDatabase"];
            oMessageHeader = new CBLServiceReference.MessageHeader();
            oMessageHeader.AuthenticationKey = AuthenticationKey;
            oMessageHeader.DatabaseName = DataBase;
        }

        public void GetLineInfo(string policyId)
        {
            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();

            var oService = new CBLServiceReference.EpicSDK_2018_01Client();
            oLineFilter.PolicyID = Int32.Parse(policyId);

            try
            {
                oLineResult = oService.Get_Line(oMessageHeader, oLineFilter, 0);
                //foreach (CBLServiceReference.Line oLine in oLineResult.Lines) {
                Console.WriteLine("Line Type Code: " + oLineResult.Lines[0].LineTypeCode.ToString());
                Console.WriteLine("Default Commission Agreement: " + oLineResult.Lines[0].DefaultCommissionAgreement.ToString());
                Console.ReadKey();
                oService.Close();
            }
            catch (Exception ex)
            {
                oService.Abort();
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }
        }
        public void UpdateLineDefaultCommissionAgreement(string lineId)
        {
            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();

            var oService = new CBLServiceReference.EpicSDK_2018_01Client();
            oLineFilter.LineID = Int32.Parse(lineId);

            try
            {
                oLineResult = oService.Get_Line(oMessageHeader, oLineFilter, 0);
                CBLServiceReference.Line1 oLine = oLineResult.Lines.First();
                

                if (oLine.DefaultCommissionAgreement == false)
                {
                    oLine.DefaultCommissionAgreement = true;
                }
                oService.Update_Line(oMessageHeader, oLine);
                //Console.WriteLine("Default Commission Agreement: " + oLineResult.Lines[0].DefaultCommissionAgreement.ToString());
                //Console.ReadKey();
                oService.Close();
            }
            catch (Exception ex)
            {
                oService.Abort();
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadLine();
            }
        }
    }
}
