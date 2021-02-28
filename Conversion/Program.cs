using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Data.SqlClient;
using System.Threading;


// PR235 - Conversion Code
// - By Abhishek Chhibber (achhibber@brokerlink.ca)
// V-1.0 - Feb 24, 2021


// ****IMPORTANT****:
//Before running the code, check the following:
//Check if any SQL or Updates are commented out, or their switches are off
//



namespace EpicIntegrator
{
    class Program
    {
        static void Main(string[] args)
        {
            RunConversion();

            //For Testing Purposes:
            //PartTesting(5985829, 5985860);
            //DeletePolicy();

        }


        // SOURCEAPPMAPPING = 5985829
        // SOURCEAPPMAPPING_ACZZ0 = 5985860
        // Short Form = 5985847
        // SDK_B1 = 5986034
        // zz0 = 5985860
        // BSCA2.0 MApping = 5985821
        // CSFA MApping (both) = 5985847
        // BSCA1.0 = 5985855


        static void DeletePolicy ()
        {
            List<int> PoliciesToDelete = new List<int>() { 5986198 };
            
            EpicIntegrator.ConversionService cs = new EpicIntegrator.ConversionService();
            foreach (int pol in PoliciesToDelete) { cs.DeletePolicy(pol); }
            Console.ReadKey();
        }

        static void PartTesting(int oPolId,  int nPolId)
        {
            EpicIntegrator.ConversionService cs = new EpicIntegrator.ConversionService();

            cs.GetPolicyList();
            //cs.LongShortFormUpdate(oPolId, nPolId);
            //cs.UpdateLine(oPolId, nPolId);
            //cs.ReadMCS(oPolId, nPolId);
            //cs.CustomFormOrSupplimentalScreen(oPolId);
            Console.ReadKey();
        }

        // -  Testing One Page
        static string AuthenticationKey;
        static string DataBase;
        static string ConnectionString = ConfigurationManager.ConnectionStrings["CBLReporting"].ConnectionString;
        CBLServiceReference.MessageHeader oMessageHeader;
        SqlConnection conn = new SqlConnection(ConnectionString);

        public Program()
        {
            AuthenticationKey = ConfigurationManager.AppSettings["AppliedSDKKey"];
            DataBase = ConfigurationManager.AppSettings["AppliedSDKDatabase"];
            oMessageHeader = new CBLServiceReference.MessageHeader();
            oMessageHeader.AuthenticationKey = AuthenticationKey;
            oMessageHeader.DatabaseName = DataBase;
            //CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
        }

        


 

        static void RunConversion()
        {
            EpicIntegrator.ConversionService cs = new EpicIntegrator.ConversionService();
            List<Tuple<int, int>> polList = new List<Tuple<int, int>>();
            CBLServiceReference.Policy pol = new CBLServiceReference.Policy();
            CBLServiceReference.Line1 lne = new CBLServiceReference.Line1();
            CBLServiceReference.MultiCarrierSchedule mcs = new CBLServiceReference.MultiCarrierSchedule();
            CBLServiceReference.Applicant9 acs = new CBLServiceReference.Applicant9();
            CBLServiceReference.EmployeeClass emp = new CBLServiceReference.EmployeeClass();
           
            // Get all policies
            polList = cs.GetPolicyList();
            if (polList == null)
            {
                Console.WriteLine("No record to process!");
                Console.ReadKey();
                return;
            }
            //Reading all policies one at a time


            foreach (Tuple<int, int> poli in polList)
                //Parallel.ForEach(polList, poli =>
           {
                int OldPolID = poli.Item1;
                int CFormExists = poli.Item2;

                Console.WriteLine("*-*-*-*-*");
                Console.WriteLine("Old Policy ID: " + OldPolID);

                // Create a new policy

                Tuple<int, bool> NewPolicyResult = cs.CreatePolicy(OldPolID); //finalcheck
                int NewPolId = NewPolicyResult.Item1;
                Console.WriteLine("New Pol ID: " + NewPolId);
                bool HasMCS = NewPolicyResult.Item2;
                // wait 3 seconds for new policy data to commit to DB
                System.Threading.Thread.Sleep(2000);

                // Fred's Pre Policy insert SSR adjustments come here

                //Update Line
                cs.UpdateLine(OldPolID, NewPolId);

                //Update Line PRBR
                cs.UpdateLinePRBR(OldPolID, NewPolId);


                //Read MCS
                if (HasMCS == true)
                {
                    cs.ReadMCS(OldPolID, NewPolId);
                }
                if (CFormExists == 1)
                {
                    //Update Policy Info
                    cs.UpdatePolicyInfoApplicatLocation(OldPolID, NewPolId);

                    // Update Other Long Form Details
                    cs.UpdateOtherLongFormDetails(OldPolID, NewPolId);

                    // Update Long Form / Short Form / BSCA1
                    cs.LongShortFormUpdate(OldPolID, NewPolId);
                }

                // Fred's Post Policy insert SSR adjustments come here

                if (CFormExists == 1)
                {
                    cs.CFYesFinalUpdate(OldPolID);
                }
                else if (CFormExists == 0)
                {
                    cs.CFNoFinalUpdate(OldPolID);
                }

                Console.WriteLine("Completed Policy - " + OldPolID);
            }

            //); // Parallel threading for each ending
            Console.WriteLine("*-*-*-DONE*-*-*");
            Console.ReadKey();
        }
        

        //cs.CustomFormOrSupplimentalScreen(OldPolID);

        //public static List<Tuple<int, int>> GetPolicyList()
        //{
        //    SqlConnection conn = new SqlConnection(ConnectionString); //Used as a public connection
        //    List<Tuple<int, int>> pols = new List<Tuple<int, int>>();
        //    string query = @"SELECT OldPolID, CFormExists FROM [CBL_Reporting].[dbo].[PR235Status] where ConversionSuccessful != 1;"; //finalcheck
        //    SqlCommand command = new SqlCommand(query, conn);
        //    conn.Open();


        //    SqlDataReader rdr = command.ExecuteReader();

        //    if (rdr.HasRows)
        //    {
        //        while (rdr.Read())
        //        {
        //            int PolID = Convert.ToInt32(rdr["OldPolID"].ToString());
        //            int HasCForm = Convert.ToInt32(rdr["CFormExists"].ToString());
        //            var PolicyResult = Tuple.Create<int, int>(PolID, HasCForm);
        //            pols.Add(PolicyResult);
        //            Console.WriteLine(PolicyResult);
        //        }
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //    rdr.Close();
        //    return pols;
        //}


    }

}
