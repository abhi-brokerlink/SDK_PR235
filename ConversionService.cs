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

//change 

namespace EpicIntegrator
{
    public class ConversionService
    {
        static string AuthenticationKey;
        static string DataBase;
        static string ConnectionString = ConfigurationManager.ConnectionStrings["CBLReporting"].ConnectionString;
        CBLServiceReference.MessageHeader oMessageHeader;
        public SqlConnection conn = new SqlConnection(ConnectionString);
        int NewPolicyID;
        public int PolicyCreated = 0;
        public int LineUpdated = 0;
        public int CformUpdated = 0;

        // Switches (1/0) and vars
        public string IBCCodePath = @"C:\Users\Abhishek\Documents\Abhi _ IMP\Sep29\IBC_Codes.csv"; //final check
        public string DBtable = "[CBL_Reporting].[dbo].[PR235Status]";
        public string NewPolExtn = "_abhi2";
        public string ErrorString = "";
        public string ErrorFilePath = @"C:\Users\Abhishek\Documents\abc\SDKErrorLog_";

        public int CreatePolicySQLSwitch = 0;
        public int SDKCreatePolicySwitch = 1;
        public int SDKUpdatePolicySwitch = 1;
        public int SDKReadMCSSwitch = 1;
        public int MCSSQLUpdateSwitch = 0;
        public int SDKUpdatePRBR = 1;
        public int SDKUpdateLine = 1;
        public int SQLUpdateLineSwitch = 0;
        public int SDKPolInfoAppLocation = 1;
        public int SQLPolInfoAppLocation = 0;
        public int SDKOtherLFDet1 = 1;
        public int SDKOtherLFDet2 = 1;
        public int SDKOtherLFDet3 = 1;
        public int LongFormUpdateSwitch = 1;
        public int SQLLongFormUpdate = 0;
        public int ShortFormUpdateSwitch = 1;
        public int SQLShortFormUpdate = 0;
        public int BSCAUpdateSwitch = 1;
        public int SQLBSCAUpdate = 0;
        public int SQLPolInfoUpdatedSwitch = 0;
        public int FinalYesSQLSwitch = 0;

        public ConversionService()
        {
            AuthenticationKey = ConfigurationManager.AppSettings["AppliedSDKKey"];
            DataBase = ConfigurationManager.AppSettings["AppliedSDKDatabase"];
            oMessageHeader = new CBLServiceReference.MessageHeader();
            oMessageHeader.AuthenticationKey = AuthenticationKey;
            oMessageHeader.DatabaseName = DataBase;
            //CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
        }

        

        public List<Tuple<int , int>> GetPolicyList()
        {
            
            //SqlConnection conn = new SqlConnection(ConnectionString); //Used as a public connection
            List<Tuple<int, int>> pols = new List<Tuple<int, int>>();

            conn.Open();
            using (SqlCommand commandZero = conn.CreateCommand())
            {
                string sqlone = string.Format("SELECT OldPolID, CFormExists FROM {0} where NewPolID IS NULL;", DBtable);
                commandZero.CommandText = sqlone;              
                SqlDataReader rdr = commandZero.ExecuteReader();
                if (rdr.HasRows)
                {
                    while (rdr.Read())
                    {
                        int PolID = Convert.ToInt32(rdr["OldPolID"].ToString());
                        //Console.WriteLine(PolID);
                        int HasCForm = Convert.ToInt32(rdr["CFormExists"].ToString());
                        var PolicyResult = Tuple.Create<int, int>(PolID, HasCForm);
                        pols.Add(PolicyResult);
                    }
                }
                else
                {
                    return null;
                }
                rdr.Close();
            }
            conn.Close();


            return pols;
        }



        public Tuple<int, bool, string, string, int> CreatePolicy(int OldPolicyID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.Policy oPol = new CBLServiceReference.Policy();
            oPolicyFilter.PolicyID = OldPolicyID;
            oPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, oPolicyFilter, 0);
            oPol = oPolicyResult.Policies[0];

            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 lne = new CBLServiceReference.Line1();
            oLineFilter.PolicyID = OldPolicyID;
            oLineResult = EpicSDKClient.Get_Line(oMessageHeader, oLineFilter, 0);
            lne = oLineResult.Lines[0];

            

                // SQL-Commented out
                //conn.Open();
                //using (SqlCommand commandOne = conn.CreateCommand())
                //{

                //    string sqltwo = string.Format("update {0} set StartTime = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
                //    commandOne.CommandText = sqltwo;
                    
                //    commandOne.Parameters.AddWithValue("@OldPolID", OldPolicyID);
                //    commandOne.ExecuteNonQuery();
                //}
                //conn.Close();
            
            

            CBLServiceReference.Policy nPol = new CBLServiceReference.Policy();

            nPol.AccountID = oPol.AccountID;
            nPol.AgencyCode = oPol.AgencyCode;
            nPol.AnnualizedCommission = oPol.AnnualizedCommission;
            nPol.AnnualizedPremium = oPol.AnnualizedPremium;
            nPol.BillingModeOption = oPol.BillingModeOption;
            nPol.BranchCode = oPol.BranchCode;
            nPol.DepartmentCode = oPol.DepartmentCode;
            if (oPol.Description != "")
            {
                nPol.Description = oPol.Description;
            }
            else
            {
                nPol.Description = NewPolLineDesc(lne.LineTypeCode);
            }
            
            nPol.EffectiveDate = oPol.EffectiveDate;
            nPol.ExpirationDate = oPol.ExpirationDate;
            nPol.EstimatedCommission = oPol.EstimatedCommission;
            nPol.EstimatedPremium = oPol.EstimatedPremium;
            nPol.IssuingCompanyLookupCode = oPol.IssuingCompanyLookupCode;
            nPol.IssuingLocationCode = lne.IssuingLocationCode;
            Console.WriteLine(lne.LineTypeCode);
            // TO DO
            nPol.LineTypeCode = NewPolLineType(lne.LineTypeCode);
            //nPol.LineTypeCode = NewPolLineType(oPol.PolicyTypeCode);
            int PolNumLength = oPol.PolicyNumber.Length;
            if (PolNumLength > 18)
            {
                nPol.PolicyNumber = oPol.PolicyNumber.Substring(0, 18) + NewPolExtn;
            }
            else if (PolNumLength < 19)
            {
                nPol.PolicyNumber = oPol.PolicyNumber + NewPolExtn;
            }

            //nPol.PolicyTypeCode = NewPolLineType(oPol.PolicyTypeCode);
            nPol.PolicyTypeCode = NewPolLineType(lne.LineTypeCode);
            nPol.ProfitCenterCode = lne.ProfitCenterCode;
            nPol.StatusCode = lne.StatusCode;
            nPol.Source = oPol.Source;

            bool MCSExists = oPol.MultiCarrierSchedule;

            
                int NewPolicyIDx = EpicSDKClient.Insert_Policy(oMessageHeader, nPol);
                System.Threading.Thread.Sleep(3000);
                //Console.WriteLine("This is new: "+NewPolicyIDx);
                PolicyCreated = 1;
            
            

            var PolicyResult = Tuple.Create<int, bool, string, string, int>(NewPolicyIDx, MCSExists, nPol.PolicyTypeCode, nPol.PolicyNumber, OldPolicyID);

            //SQL-Commented out
            //if (CreatePolicySQLSwitch == 1)
            //{
            //    conn.Open();
            //    using (SqlCommand commandTwo = conn.CreateCommand())
            //    {
            //        string sqlnine = string.Format("update {0} set NewPolID = @NewPolID, HasMCS = @hasMCS, NewPolLineTypeCode = @NewPolLineTypeCode, NewPolNum = @NewPolNum, NewPolicyInserted = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
            //        commandTwo.CommandText = sqlnine;
                    
            //        commandTwo.Parameters.AddWithValue("@NewPolID", NewPolicyIDx);
            //        commandTwo.Parameters.AddWithValue("@hasMCS", MCSExists);
            //        commandTwo.Parameters.AddWithValue("@NewPolLineTypeCode", nPol.PolicyTypeCode);
            //        commandTwo.Parameters.AddWithValue("@NewPolNum", nPol.PolicyNumber);
            //        commandTwo.Parameters.AddWithValue("@OldPolID", OldPolicyID);
            //        commandTwo.ExecuteNonQuery();
            //    }
            //    conn.Close();
            //}
            


            //Console.WriteLine("MCS Answer: "+ MCSExists);
            return PolicyResult;
            
        }




        // For each policy, get all the Policy and Line level content
        public CBLServiceReference.Policy GetPolicyDetails(int UniqPolicy)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            List<EpicIntegrator.Policy> polList = new List<EpicIntegrator.Policy>();
            CBLServiceReference.Policy pol = new CBLServiceReference.Policy();
            oPolicyFilter.PolicyID = UniqPolicy;
            oPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, oPolicyFilter, 0);

            //Console.WriteLine(oPolicyResult.Policies.Length);
            pol = oPolicyResult.Policies[0];

            return pol;
        }


        // we don't need this menthod as all the params are copied over in CreatePolicy
        public bool UpdatePolicy(int OldPolicyID, int NewPolicyID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.Policy oPol = new CBLServiceReference.Policy();

            CBLServiceReference.PolicyFilter nPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.PolicyGetResult nPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.Policy nPol = new CBLServiceReference.Policy();

            oPolicyFilter.PolicyID = OldPolicyID;
            oPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, oPolicyFilter, 0);
            oPol = oPolicyResult.Policies[0];

            nPolicyFilter.PolicyID = NewPolicyID;
            nPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, nPolicyFilter, 0);
            nPol = nPolicyResult.Policies[0];

            nPol.Source = oPol.Source;
            nPol.AnnualizedPremium = oPol.AnnualizedPremium;
            nPol.EstimatedPremium = oPol.EstimatedPremium;
            nPol.AnnualizedCommission = oPol.AnnualizedCommission;
            nPol.EstimatedCommission = oPol.EstimatedCommission;

            bool MCSExists = oPol.MultiCarrierSchedule;

            if (SDKUpdatePolicySwitch == 1)
            {
                EpicSDKClient.Update_Policy(oMessageHeader, nPol);
            }

            //SQL-Commented out

            //if (SQLPolInfoUpdatedSwitch == 1)
            //{
            //    conn.Open();
            //    using (SqlCommand commandMCS = conn.CreateCommand())
            //    {
            //        string sqlten = string.Format("update {0} set HasMCS = @hasMCS, PolicyInfoUpdated= GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
            //        commandMCS.CommandText = sqlten;


            //        commandMCS.Parameters.AddWithValue("@hasMCS", MCSExists);
            //        commandMCS.Parameters.AddWithValue("@OldPolID", OldPolicyID);
            //        commandMCS.ExecuteNonQuery();
            //    }
            //    conn.Close();
            //}
            
           

            //Console.WriteLine(MCSExists);
            return MCSExists;

            //Console.ReadKey();
        }



        public void ReadMCS(int OldPolicyID, int NewPolicyID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.MultiCarrierScheduleGetResult oMCSResult = new CBLServiceReference.MultiCarrierScheduleGetResult();
            CBLServiceReference.MultiCarrierScheduleGetType MCStype = new CBLServiceReference.MultiCarrierScheduleGetType();
            MCStype = CBLServiceReference.MultiCarrierScheduleGetType.PolicyID;
            CBLServiceReference.MultiCarrierSchedule oMCS = new CBLServiceReference.MultiCarrierSchedule();
            CBLServiceReference.PolicyFilter nPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.PolicyGetResult nPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.Policy nPol = new CBLServiceReference.Policy();
            nPolicyFilter.PolicyID = NewPolicyID;
            nPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, nPolicyFilter, 0);
            nPol = nPolicyResult.Policies[0];

            bool MCSExists = nPol.MultiCarrierSchedule;
            if (MCSExists == false)
            {
                // It reads False MCS in new policy, to check if any pre-existing MCS are there > So that Activate function below will switch from 0 to 1
                EpicSDKClient.Action_Policy_ActivateInactivateMultiCarrierSchedule(oMessageHeader, NewPolicyID);


                oMCSResult = EpicSDKClient.Get_Policy_MultiCarrierSchedule(oMessageHeader, OldPolicyID, false, MCStype, 0);
                // testing feedback done
                //Console.WriteLine(oMCSResult.TotalPages);
                if (oMCSResult.TotalPages > 0)
                {

                    int MCSCount = oMCSResult.MultiCarrierSchedules.Count();
                    //Console.WriteLine("MCS Count " + MCSCount);
                    if (MCSCount > 0)
                    {
                        for (int i = 0; i < MCSCount; i++)
                        {

                            CBLServiceReference.MultiCarrierSchedule nMCS = new CBLServiceReference.MultiCarrierSchedule();
                            //CBLServiceReference.ProducerBrokerCommissionItems MCSPBC = new CBLServiceReference.ProducerBrokerCommissionItems();
                            //CBLServiceReference.ProducerBrokerCommissionItem MCSPBI = new CBLServiceReference.ProducerBrokerCommissionItem();
                            oMCS = oMCSResult.MultiCarrierSchedules[i];
                            //nMCS.PolicyLineOption.Value = oMCS.PolicyLineOption.Value;
                            nMCS.PolicyID = NewPolicyID;
                            nMCS.Active = true;

                            nMCS.IssuingCompanyLookupCode = oMCS.IssuingCompanyLookupCode;
                            nMCS.PremiumPayableLookupCode = oMCS.PremiumPayableLookupCode;
                            nMCS.PremiumPayableTypeCode = oMCS.PremiumPayableTypeCode;
                            nMCS.PremiumPayableContractID = oMCS.PremiumPayableContractID;
                            nMCS.ParticipationPercentage = oMCS.ParticipationPercentage;
                            nMCS.PolicyNumber = oMCS.PolicyNumber;
                            nMCS.AgencyCommissionTypeCode = oMCS.AgencyCommissionTypeCode;
                            nMCS.AgencyCommissionPercentage = oMCS.AgencyCommissionPercentage;

                        



                        
                            // Testing feedback done -  inserting PR/BR commission schedules inside MCS schedules

                            int MCSPRBRCommCount = oMCS.ProducerBrokerCommissions.Count;
                            for (int m = 0; m < MCSPRBRCommCount; m++)
                            {
                                CBLServiceReference.ProducerBrokerCommissionItems2 MCSPBC = new CBLServiceReference.ProducerBrokerCommissionItems2();
                                CBLServiceReference.ProducerBrokerCommissionItem2 MCSPBI = new CBLServiceReference.ProducerBrokerCommissionItem2();
                                nMCS.ProducerBrokerCommissions = MCSPBC;
                                MCSPBC.Add(MCSPBI);

                                MCSPBI.ContractID = oMCS.ProducerBrokerCommissions[m].ContractID;
                                MCSPBI.ProducerBrokerCode = oMCS.ProducerBrokerCommissions[m].ProducerBrokerCode;
                                MCSPBI.LookupCode = oMCS.ProducerBrokerCommissions[m].LookupCode;
                                MCSPBI.ProductionCredit = oMCS.ProducerBrokerCommissions[m].ProductionCredit;
                                MCSPBI.OrderNumber = oMCS.ProducerBrokerCommissions[m].OrderNumber;
                                MCSPBI.Flag = CBLServiceReference.Flags22.Insert;
                            }
                        
                            if (SDKReadMCSSwitch == 1)
                            {
                                EpicSDKClient.Insert_Policy_MultiCarrierSchedule(oMessageHeader, nMCS);
                            }
                            



                        }
                    }
                }
            }
            //SQL-Commented out
            //if (MCSSQLUpdateSwitch == 1)
            //{
            //    conn.Open();
            //    using (SqlCommand commandMCSupdate = conn.CreateCommand())
            //    {
            //        string sqlthree = string.Format("update {0} set MCSUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
            //        commandMCSupdate.CommandText = sqlthree;
                    
            //        commandMCSupdate.Parameters.AddWithValue("@OldPolID", OldPolicyID);
            //        commandMCSupdate.ExecuteNonQuery();
            //    }
            //    conn.Close();
            //}
            
            Console.WriteLine("MCS Read");
        }




        public void UpdateLinePRBR(int OldPolicyID, int NewPolicyID)
        {

            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 olne = new CBLServiceReference.Line1();

            oLineFilter.PolicyID = OldPolicyID;
            oLineResult = EpicSDKClient.Get_Line(oMessageHeader, oLineFilter, 0);
            olne = oLineResult.Lines[0];

            CBLServiceReference.LineGetResult1 nLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter nLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 nlne = new CBLServiceReference.Line1();

            nLineFilter.PolicyID = NewPolicyID;
            nLineResult = EpicSDKClient.Get_Line(oMessageHeader, nLineFilter, 0);
            nlne = nLineResult.Lines[0];
            if (olne.BillingValue.InvoiceToType == "Broker")
            {
                int PRBRCountNew = nlne.ProducerBrokerCommissionsValue.Commissions.Count;
                for (int j = 0; j < PRBRCountNew; j++)
                {
                    string oldPRBRLookup = olne.ProducerBrokerCommissionsValue.Commissions[j].LookupCode;
                    for (int k = 0; k < PRBRCountNew; k++)
                    {
                        if (nlne.ProducerBrokerCommissionsValue.Commissions[k].LookupCode == oldPRBRLookup)
                        {
                            nlne.ProducerBrokerCommissionsValue.Commissions[k].ProductionCredit = olne.ProducerBrokerCommissionsValue.Commissions[j].ProductionCredit;
                            nlne.ProducerBrokerCommissionsValue.Commissions[k].CommissionAgreementID = olne.ProducerBrokerCommissionsValue.Commissions[j].CommissionAgreementID;
                            nlne.ProducerBrokerCommissionsValue.Commissions[k].Flag = CBLServiceReference.Flags6.Update;

                        }
                    }
                }
            }
            if (SDKUpdatePRBR == 1)
            {
                EpicSDKClient.Update_Line(oMessageHeader, nlne);
            }
            
            Console.WriteLine("PR/BR updated");
        }


        public void UpdateLine(int OldPolicyID, int NewPolicyID)
        {
            int olPolId = OldPolicyID;
            int nwPolId = NewPolicyID;

            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 olne = new CBLServiceReference.Line1();

            

            oLineFilter.PolicyID = OldPolicyID;
            oLineResult = EpicSDKClient.Get_Line(oMessageHeader, oLineFilter, 0);
            olne = oLineResult.Lines[0];

            CBLServiceReference.LineGetResult1 nLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter nLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 nlne = new CBLServiceReference.Line1();

            nLineFilter.PolicyID = NewPolicyID;
            nLineResult = EpicSDKClient.Get_Line(oMessageHeader, nLineFilter, 0);
            nlne = nLineResult.Lines[0];


            nlne.BillingModeOption.OptionName = olne.BillingModeOption.OptionName;
            // Testing Feedback done - Payable Contract
            if (olne.BillingModeOption.OptionName == "AgencyBilled")
            {
                if (nlne.BillingModeOption.OptionName == "AgencyBilled")
                {
                    if (olne.PayableContractID != null)
                    {
                        nlne.PayableContractID = olne.PayableContractID;
                    }
                }
            }
            nlne.BillingModeOption.Value = olne.BillingModeOption.Value;
            nlne.PremiumPayableTypeCode = olne.PremiumPayableTypeCode;
            nlne.PremiumPayableLookupCode = olne.PremiumPayableLookupCode;
            nlne.BilledPremium = olne.BilledPremium;
            nlne.BilledCommission = olne.BilledCommission;
            nlne.IssuingCompanyLookupCode = olne.IssuingCompanyLookupCode;

            //Console.WriteLine("Aggreement ID " + nlne.AgreementID);
            //Console.WriteLine("DefaultComm Agg " + nlne.DefaultCommissionAgreement);

            try
            {
                
                //nlne.DefaultCommissionAgreement = olne.DefaultCommissionAgreement; //Default Commission Agreement checkbox commented out a per Erich's email
                //nlne.AgreementID = olne.AgreementID;
                nlne.AgencyCommissionType = olne.AgencyCommissionType;
                nlne.AgencyCommissionPercent = olne.AgencyCommissionPercent;
                nlne.AgencyCommissionAmount = olne.AgencyCommissionAmount;
                // Testing Feedback done
                if (olne.AgreementID > 0)
                {
                    nlne.AgreementID = olne.AgreementID;
                }
                


            }
            catch (Exception e)
            {
                string e1 = OldPolicyID + " | No Commission Agreement | " + e;
                ErrorString = ErrorString + e1 + System.Environment.NewLine;
                Console.WriteLine(e1);
            }
            finally
            {


                nlne.EstimatedPremium = olne.EstimatedPremium;
                nlne.EstimatedCommission = olne.EstimatedCommission;
                nlne.AnnualizedPremium = olne.AnnualizedPremium;
                nlne.AnnualizedCommission = olne.AnnualizedCommission;
                nlne.BillingValue.BillingPlan = olne.BillingValue.BillingPlan;
                nlne.BillingValue.TaxOptionCode = olne.BillingValue.TaxOptionCode;
                nlne.BillingValue.InvoiceToType = olne.BillingValue.InvoiceToType;
                if (olne.BillingValue.InvoiceToType == "Broker")
                {
                    nlne.BillingValue.InvoiceToAccountLookupCode = olne.BillingValue.InvoiceToAccountLookupCode;
                    
                    nlne.BillingValue.BillBrokerNet = olne.BillingValue.BillBrokerNet;
                    nlne.BillingValue.LoanNumber = olne.BillingValue.LoanNumber;
                    nlne.BillingValue.InvoiceToDeliveryMethod = olne.BillingValue.InvoiceToDeliveryMethod;


                    // adjust existing producer
                    string ExistingBrokerLookupCode = nlne.ProducerBrokerCommissionsValue.Commissions[0].LookupCode;
                    int PRBRCount = olne.ProducerBrokerCommissionsValue.Commissions.Count;

                    if (PRBRCount > 0)
                    {
                        if (PRBRCount > nlne.ProducerBrokerCommissionsValue.Commissions.Count)
                        {
                            //AddPRBR(olne, nlne);
                            for (int i = 0; i < PRBRCount; i++)
                            {

                                if (olne.ProducerBrokerCommissionsValue.Commissions[i].LookupCode != ExistingBrokerLookupCode)
                                {

                                    // Importnant: Code assumes that the default broker code is at Order number 0 in both source and destination policies
                                    //Console.WriteLine(olne.ProducerBrokerCommissionsValue.Commissions[i].LookupCode + " - code NOT matches");
                                    CBLServiceReference.CommissionItem ci = new CBLServiceReference.CommissionItem();
                                    nlne.ProducerBrokerCommissionsValue.Commissions.Add(ci);
                                    //Console.WriteLine(olne.ProducerBrokerCommissionsValue.Commissions[i].ProducerBrokerCode);
                                    //Console.WriteLine(olne.ProducerBrokerCommissionsValue.Commissions[i].CommissionAgreementID);
                                    nlne.ProducerBrokerCommissionsValue.Commissions[i].ProducerBrokerCode = olne.ProducerBrokerCommissionsValue.Commissions[i].ProducerBrokerCode;
                                    nlne.ProducerBrokerCommissionsValue.Commissions[i].LookupCode = olne.ProducerBrokerCommissionsValue.Commissions[i].LookupCode;
                                    //Testing Feedback Done
                                    nlne.ProducerBrokerCommissionsValue.Commissions[i].CommissionAgreementID = olne.ProducerBrokerCommissionsValue.Commissions[i].CommissionAgreementID;
                                    nlne.ProducerBrokerCommissionsValue.Commissions[i].OrderNumber = olne.ProducerBrokerCommissionsValue.Commissions[i].OrderNumber;
                                    nlne.ProducerBrokerCommissionsValue.Commissions[i].Flag = CBLServiceReference.Flags6.Insert;
                                }

                            }


                        }
                        //Update PRBR Production Credits
                    }
                    nlne.BillingValue.InvoiceToContactName = olne.BillingValue.InvoiceToContactName;

                }
                else if (olne.BillingValue.InvoiceToType == "Client")
                {
                    nlne.BillingValue.LoanNumber = olne.BillingValue.LoanNumber;
                    nlne.BillingValue.InvoiceToDeliveryMethod = olne.BillingValue.InvoiceToDeliveryMethod;
                    nlne.PremiumPayableLookupCode = olne.PremiumPayableLookupCode;
                    
                    int PRBRCount = olne.ProducerBrokerCommissionsValue.Commissions.Count;
                    if (PRBRCount > 0)
                    {
                        
                        int PRBRCountNew = nlne.ProducerBrokerCommissionsValue.Commissions.Count;
                        if (PRBRCountNew < PRBRCount)
                        {
                            for (int i = 0; i < PRBRCount; i++)
                            {
                                CBLServiceReference.CommissionItem ci = new CBLServiceReference.CommissionItem();
                                nlne.ProducerBrokerCommissionsValue.Commissions.Add(ci);
                                nlne.ProducerBrokerCommissionsValue.Commissions[i].ProducerBrokerCode = olne.ProducerBrokerCommissionsValue.Commissions[i].ProducerBrokerCode;
                                nlne.ProducerBrokerCommissionsValue.Commissions[i].LookupCode = olne.ProducerBrokerCommissionsValue.Commissions[i].LookupCode;
                                nlne.ProducerBrokerCommissionsValue.Commissions[i].ProductionCredit = olne.ProducerBrokerCommissionsValue.Commissions[i].ProductionCredit;
                                //Testing Feedback Done
                                nlne.ProducerBrokerCommissionsValue.Commissions[i].CommissionAgreementID = olne.ProducerBrokerCommissionsValue.Commissions[i].CommissionAgreementID;
                                //Console.WriteLine(olne.ProducerBrokerCommissionsValue.Commissions[i].CommissionAgreementID);
                                nlne.ProducerBrokerCommissionsValue.Commissions[i].OrderNumber = olne.ProducerBrokerCommissionsValue.Commissions[i].OrderNumber;
                                nlne.ProducerBrokerCommissionsValue.Commissions[i].Flag = CBLServiceReference.Flags6.Insert;
                                //Console.WriteLine("eeee" + olne.ProducerBrokerCommissionsValue.Commissions[i].ProducerBrokerCode);
                            }

                        }

                    }
                    // testing feedback done - Billing to address different
                    if (olne.BillingValue.InvoiceToContactName != "")
                    {
                        nlne.BillingValue.InvoiceToContactName = olne.BillingValue.InvoiceToContactName;
                    }
                }
                // TODO  
                int ADCCount = olne.AgencyDefinedCategoryItems.Count;
                if (ADCCount > 0)
                {
                    int ADCCountNew = nlne.AgencyDefinedCategoryItems.Count;
                    if (ADCCountNew < ADCCount)
                    {
                        for (int i = 0; i < ADCCount; i++)
                        {
                            CBLServiceReference.AgencyDefinedCodeItem adci = new CBLServiceReference.AgencyDefinedCodeItem(); // needed to create a placeholder at index 0
                            nlne.AgencyDefinedCategoryItems.Add(adci);

                            nlne.AgencyDefinedCategoryItems[i].ADCCategory = olne.AgencyDefinedCategoryItems[i].ADCCategory;
                            nlne.AgencyDefinedCategoryItems[i].ADCOption = olne.AgencyDefinedCategoryItems[i].ADCOption;
                            nlne.AgencyDefinedCategoryItems[i].Flag = CBLServiceReference.Flags1.Insert;
                        }
                    }

                }
                nlne.HistoryValue.Comments = olne.HistoryValue.Comments;
                nlne.HistoryValue.DateFirstWritten = olne.HistoryValue.DateFirstWritten;

                if (SDKUpdateLine == 1)
                {
                    EpicSDKClient.Update_Line(oMessageHeader, nlne);
                    LineUpdated = 1;
                }


                //SQL-Commented out
                //if (SQLUpdateLineSwitch == 1)
                //{
                //    conn.Open();
                //    using (SqlCommand commandUpdateLine = conn.CreateCommand())
                //    {
                //        string sqlfour = string.Format("update {0} set LineInfoUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
                //        commandUpdateLine.CommandText = sqlfour;
                        
                //        commandUpdateLine.Parameters.AddWithValue("@OldPolID", OldPolicyID);
                //        commandUpdateLine.ExecuteNonQuery();
                //    }
                //    conn.Close();
                //}



                Console.WriteLine("Line Updated");
                //Console.ReadKey();
            }






        }

        public CBLServiceReference.Line1 GetLineDetails(int UniqPolicy)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 lne = new CBLServiceReference.Line1();
            oLineFilter.PolicyID = UniqPolicy;
            oLineResult = EpicSDKClient.Get_Line(oMessageHeader, oLineFilter, 0);
            lne = oLineResult.Lines[0];
            return lne;
        }




        public void UpdatePolicyInfoApplicatLocation(int OldPolicyID, int NewPolicyID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyInformationGetType PolGetType = new CBLServiceReference.PolicyInformationGetType();
            PolGetType = CBLServiceReference.PolicyInformationGetType.PolicyID;
            CBLServiceReference.Applicant9 oApplicant = new CBLServiceReference.Applicant9();
            List<CBLServiceReference.Applicant9> oApplicantList = new List<CBLServiceReference.Applicant9>();

            CBLServiceReference.Applicant9 nApplicant = new CBLServiceReference.Applicant9();

            oApplicant = EpicSDKClient.Get_PolicyInformation_ApplicantLocations(oMessageHeader, OldPolicyID, PolGetType)[0];
            nApplicant = EpicSDKClient.Get_PolicyInformation_ApplicantLocations(oMessageHeader, NewPolicyID, PolGetType)[0];


            nApplicant.Name = oApplicant.Name;
            nApplicant.MailingAddress = oApplicant.MailingAddress;
            nApplicant.Website = oApplicant.Website;
            nApplicant.PhoneNumber = oApplicant.PhoneNumber;
            nApplicant.Email = oApplicant.Email;
            nApplicant.SICCode = oApplicant.SICCode;
            nApplicant.NAICSCode = oApplicant.NAICSCode;
            nApplicant.FEIN = oApplicant.FEIN;
            nApplicant.NatureOfBusiness = oApplicant.NatureOfBusiness;
            nApplicant.MarketType = oApplicant.MarketType;
            nApplicant.MarketSize = oApplicant.MarketSize;
            nApplicant.CompanyRevenue = oApplicant.CompanyRevenue;
            nApplicant.CompanyPayroll = oApplicant.CompanyPayroll;
            nApplicant.CompanyPayrollCycle = oApplicant.CompanyPayrollCycle;
            nApplicant.BusinessStartedDate = oApplicant.BusinessStartedDate;
            nApplicant.BrokerOfRecordDate = oApplicant.BrokerOfRecordDate;

            int LocationCount = oApplicant.LocationItems.Count;
            if (LocationCount > 0)
            {
                for (int i = 0; i < LocationCount; i++)
                {
                    CBLServiceReference.LocationItem LocItem = new CBLServiceReference.LocationItem();
                    nApplicant.LocationItems.Add(LocItem);
                    nApplicant.LocationItems[i].LocationID = oApplicant.LocationItems[i].LocationID;
                    nApplicant.LocationItems[i].LocationNumber = oApplicant.LocationItems[i].LocationNumber;
                    nApplicant.LocationItems[i].Address = oApplicant.LocationItems[i].Address;
                    nApplicant.LocationItems[i].Type = oApplicant.LocationItems[i].Type;
                    nApplicant.LocationItems[i].PayrollCycle = oApplicant.LocationItems[i].PayrollCycle;
                    nApplicant.LocationItems[i].Flag = CBLServiceReference.Flags130.Insert;
                }
            }

            if (SDKPolInfoAppLocation == 1)
            {
                EpicSDKClient.Update_PolicyInformation_ApplicantLocations(oMessageHeader, nApplicant);
            }

            //SQL-Commented out
            //if (SQLPolInfoAppLocation == 1)
            //{
            //    conn.Open();
            //    using (SqlCommand commandAppLoc = conn.CreateCommand())
            //    {
            //        string sqlfive = string.Format("update {0} set PolAppLocUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
            //        commandAppLoc.CommandText = sqlfive;
                    
            //        commandAppLoc.Parameters.AddWithValue("@OldPolID", OldPolicyID);
            //        commandAppLoc.ExecuteNonQuery();
            //    }
            //    conn.Close();
            //}
            


            Console.WriteLine("Prop info Updated");
        }

        public void UpdateOtherLongFormDetails(int OldPolicyID, int NewPolicyID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.LineGetResult1 nLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter nLineFilter = new CBLServiceReference.LineFilter();




            // Get Line ID
            oLineFilter.PolicyID = OldPolicyID;
            oLineResult = EpicSDKClient.Get_Line(oMessageHeader, oLineFilter, 0);
            int oLineID = oLineResult.Lines[0].LineID;

            nLineFilter.PolicyID = NewPolicyID;
            nLineResult = EpicSDKClient.Get_Line(oMessageHeader, nLineFilter, 0);
            int nLineID = nLineResult.Lines[0].LineID;

            // Additional Coverage
            List<CBLServiceReference.AdditionalCoverage4> OAddCov = new List<CBLServiceReference.AdditionalCoverage4>();
            Array oAddCovArray = OAddCov.ToArray();
            oAddCovArray = EpicSDKClient.Get_CustomForm_AdditionalCoverage(oMessageHeader, oLineID, CBLServiceReference.AdditionalCoverageGetType4.LineID);

            foreach (CBLServiceReference.AdditionalCoverage4 oAddCovItem in oAddCovArray)
            {

                CBLServiceReference.AdditionalCoverage4 nAddCovItem = new CBLServiceReference.AdditionalCoverage4();
                nAddCovItem.RiskNumber = oAddCovItem.RiskNumber;
                nAddCovItem.Code = oAddCovItem.Code;
                nAddCovItem.Description = oAddCovItem.Description;
                nAddCovItem.Limit = oAddCovItem.Limit;
                nAddCovItem.Deductible = oAddCovItem.Deductible;
                nAddCovItem.Premium = oAddCovItem.Premium;
                nAddCovItem.Remarks = oAddCovItem.Remarks;
                nAddCovItem.LineID = nLineID;
                if (SDKOtherLFDet1 == 1)
                {
                    EpicSDKClient.Insert_CustomForm_AdditionalCoverage(oMessageHeader, nAddCovItem);
                }
                

            }

            //Additional Interests
            List<CBLServiceReference.AdditionalInterest7> OAddInt = new List<CBLServiceReference.AdditionalInterest7>();
            Array oAddIntArray = OAddInt.ToArray();
            oAddIntArray = EpicSDKClient.Get_CustomForm_AdditionalInterest(oMessageHeader, oLineID, CBLServiceReference.AdditionalInterestGetType5.LineID);

            foreach (CBLServiceReference.AdditionalInterest7 oAddIntItem in oAddIntArray)
            {
                CBLServiceReference.AdditionalInterest7 nAddIntItem = new CBLServiceReference.AdditionalInterest7();
                nAddIntItem.RiskNumber = oAddIntItem.RiskNumber;
                nAddIntItem.LookupCode = oAddIntItem.LookupCode;
                nAddIntItem.Name = oAddIntItem.Name;
                nAddIntItem.Address = oAddIntItem.Address;
                nAddIntItem.PhoneNumber = oAddIntItem.PhoneNumber;
                nAddIntItem.Rank = oAddIntItem.Rank;
                nAddIntItem.InterestCode = oAddIntItem.InterestCode;
                nAddIntItem.InterestIfOther = oAddIntItem.InterestIfOther;
                nAddIntItem.ReferenceLoanNumber = oAddIntItem.ReferenceLoanNumber;
                nAddIntItem.CertificateRequired = oAddIntItem.CertificateRequired;
                nAddIntItem.LineID = nLineID;
                if (SDKOtherLFDet2 == 1)
                {
                    EpicSDKClient.Insert_CustomForm_AdditionalInterest(oMessageHeader, nAddIntItem);
                }
                
            }

            //Remarks
            List<CBLServiceReference.Remark9> ORemList = new List<CBLServiceReference.Remark9>();
            Array oRemArray = ORemList.ToArray();
            oRemArray = EpicSDKClient.Get_CustomForm_Remark(oMessageHeader, oLineID, CBLServiceReference.RemarkGetType5.LineID);

            foreach (CBLServiceReference.Remark9 oRem in oRemArray)
            {
                CBLServiceReference.Remark9 nRem = new CBLServiceReference.Remark9();
                nRem.DateEntered = oRem.DateEntered;
                nRem.Description = oRem.Description;
                nRem.Note = oRem.Note;
                nRem.PrintOnForm = oRem.PrintOnForm;
                nRem.Timestamp = oRem.Timestamp;
                nRem.LineID = nLineID;
                if (SDKOtherLFDet3 == 1)
                {
                    EpicSDKClient.Insert_CustomForm_Remark(oMessageHeader, nRem);
                }
                
                //Console.WriteLine("done");
            }



        }


        public CBLServiceReference.EmployeeClass GetPolInfoEmployee(int UniqPolicy)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyInformationGetType PolGetType = new CBLServiceReference.PolicyInformationGetType();
            PolGetType = CBLServiceReference.PolicyInformationGetType.PolicyID;
            CBLServiceReference.EmployeeClass Employee = new CBLServiceReference.EmployeeClass();

            Employee = EpicSDKClient.Get_PolicyInformation_EmployeeClass(oMessageHeader, UniqPolicy, PolGetType)[0];
            return Employee;
        }


        public void DeletePolicy (int PolicyID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.Policy pol = new CBLServiceReference.Policy();
            EpicSDKClient.Delete_Policy(oMessageHeader, PolicyID);
            Console.WriteLine("Policy Deleted: " + PolicyID);
            
        }

        public static string NewPolLineType(string OldPolLineType)
        {
            string newPolTypeCode = "";
            switch (OldPolLineType)
            {
                case "CCGL":
                    newPolTypeCode = "BLIA";
                    break;
                case "CBOC":
                    newPolTypeCode = "BCRI";
                    break;
                case "CBOI":
                    newPolTypeCode = "BEBI";
                    break;
                case "CBUI":
                    newPolTypeCode = "BBUI";
                    break;
                case "CBUW":
                    newPolTypeCode = "BBUI";
                    break;
                case "CCAR":
                    newPolTypeCode = "BTRA";
                    break;
                case "CCON":
                    newPolTypeCode = "BPAC";
                    break;
                case "CCRM":
                    newPolTypeCode = "BCRI";
                    break;
                case "CCYB":
                    newPolTypeCode = "BCYB";
                    break;
                case "CDEO":
                    newPolTypeCode = "BDOL";
                    break;
                case "CDOL":
                    newPolTypeCode = "BDOL";
                    break;
                case "CEMP":
                    newPolTypeCode = "BEPL";
                    break;
                case "CENV":
                    newPolTypeCode = "BEIL";
                    break;
                case "CEOL":
                    newPolTypeCode = "BEOL";
                    break;
                case "CEQF":
                    newPolTypeCode = "BCEF";
                    break;
                case "CEXL":
                    newPolTypeCode = "BUEL";
                    break;
                case "CHBB":
                    newPolTypeCode = "BPAC";
                    break;
                case "CLEL":
                    newPolTypeCode = "BEIL";
                    break;
                case "CLEO":
                    newPolTypeCode = "BLIA";
                    break;
                case "CMAP":
                    newPolTypeCode = "BWAT";
                    break;
                case "CMUP":
                    newPolTypeCode = "BPRO";
                    break;
                case "COCA":
                    newPolTypeCode = "BOCA";
                    break;
                case "COMT":
                    newPolTypeCode = "BTRA";
                    break;
                case "CPAC":
                    newPolTypeCode = "BPAC";
                    break;
                case "CPAL":
                    newPolTypeCode = "BLIA";
                    break;
                case "CPRO":
                    newPolTypeCode = "BPRO";
                    break;
                case "CPRZ":
                    newPolTypeCode = "BLIA";
                    break;
                case "CSEL":
                    newPolTypeCode = "BLIA";
                    break;
                case "CSHR":
                    newPolTypeCode = "BLIA";
                    break;
                case "CSPO":
                    newPolTypeCode = "BLIA";
                    break;
                case "CTRI":
                    newPolTypeCode = "BACC";
                    break;
                case "CUMB":
                    newPolTypeCode = "BUEL";
                    break;
                case "CVAB":
                    newPolTypeCode = "BPAC";
                    break;
                case "CVAT":
                    newPolTypeCode = "BPRO";
                    break;
                case "CWAT":
                    newPolTypeCode = "BWAT";
                    break;
                case "CWCB":
                    newPolTypeCode = "BLIA";
                    break;
                case "CWUL":
                    newPolTypeCode = "BWUL";
                    break;
                case "CACC":
                    newPolTypeCode = "BACC";
                    break;
                case "CAVI":
                    newPolTypeCode = "BAVI";
                    break;
                case "CBOA":
                    newPolTypeCode = "BBON";
                    break;
                case "CBOD":
                    newPolTypeCode = "BBOD";
                    break;
                case "CBOF":
                    newPolTypeCode = "BBOF";
                    break;
                case "CBON":
                    newPolTypeCode = "BBON";
                    break;
                case "CLEG":
                    newPolTypeCode = "BLEG";
                    break;
                case "CMAL":
                    newPolTypeCode = "BMAL";
                    break;
                case "CMOR":
                    newPolTypeCode = "BMOR";
                    break;
                case "CNOA":
                    newPolTypeCode = "BNOA";
                    break;
                case "CBBP":
                    newPolTypeCode = "BBBP";
                    break;
                case "CFGS":
                    newPolTypeCode = "BFGS";
                    break;
                case "CFPF":
                    newPolTypeCode = "BFPF";
                    break;
                case "CFPS":
                    newPolTypeCode = "BFPS";
                    break;
                case "CMAB":
                    newPolTypeCode = "BMAB";
                    break;


            }
            return newPolTypeCode;
        }

        public static string NewPolLineDesc(string OldPolLineType)
        {
            string newPolDesc = "";
            switch (OldPolLineType)
            {
                case "CBOC":
                    newPolDesc = "Crime";
                    break;
                case "CBOI":
                    newPolDesc = "Equipment Breakdown/Boiler";
                    break;
                case "CBUI":
                    newPolDesc = "Builders Risk";
                    break;
                case "CBUW":
                    newPolDesc = "Builders Risk and Wrap Up";
                    break;
                case "CCGL":
                    newPolDesc = "Commercial General Liability/PAL/Special Events";
                    break;
                case "CCAR":
                    newPolDesc = "Cargo";
                    break;
                case "CCON":
                    newPolDesc = "Commercial Condominium";
                    break;
                case "CCRM":
                    newPolDesc = "Crime";
                    break;
                case "CCYB":
                    newPolDesc = "Cyber Risk Liability";
                    break;
                case "CDEO":
                    newPolDesc = "Directors & Officers Liability";
                    break;
                case "CDOL":
                    newPolDesc = "Directors & Officers Liability";
                    break;
                case "CEMP":
                    newPolDesc = "Commercial Employment Practice";
                    break;
                case "CENV":
                    newPolDesc = "Environmental Impairment Liability";
                    break;
                case "CEOL":
                    newPolDesc = "Errors & Omissions Liability";
                    break;
                case "CEQF":
                    newPolDesc = "Contractors Equipment Floater";
                    break;
                case "CEXL":
                    newPolDesc = "Excess Liability";
                    break;
                case "CHBB":
                    newPolDesc = "Commercial Package";
                    break;
                case "CLEL":
                    newPolDesc = "Environmental Impairment Liability";
                    break;
                case "CLEO":
                    newPolDesc = "CGL and E&O Liability";
                    break;
                case "CMAP":
                    newPolDesc = "Marine P & I";
                    break;
                case "CMUP":
                    newPolDesc = "Commercial Property Insurance";
                    break;
                case "COCA":
                    newPolDesc = "Ocean Marine Cargo";
                    break;
                case "COMT":
                    newPolDesc = "Cargo/Motor Truck Cargo/Transit";
                    break;
                case "CPAC":
                    newPolDesc = "Commercial Package";
                    break;
                case "CPAL":
                    newPolDesc = "Party Alcohol Liability";
                    break;
                case "CPRO":
                    newPolDesc = "Commercial Property Insurance";
                    break;
                case "CPRZ":
                    newPolDesc = "Prize Indemnity";
                    break;
                case "CSEL":
                    newPolDesc = "Special Event Liability";
                    break;
                case "CSHR":
                    newPolDesc = "Ship Repairer's Liability";
                    break;
                case "CSPO":
                    newPolDesc = "Sport Liability";
                    break;
                case "CTRI":
                    newPolDesc = "Commercial Accident";
                    break;
                case "CUMB":
                    newPolDesc = "Umbrella Liability";
                    break;
                case "CVAB":
                    newPolDesc = "Commercial Vacant Building";
                    break;
                case "CVAT":
                    newPolDesc = "Commercial Vacation Trailer";
                    break;
                case "CWAT":
                    newPolDesc = "Commercial Watercraft and/or Marine P & I";
                    break;
                case "CWCB":
                    newPolDesc = "Worker's Compensation Benefits";
                    break;
                case "CWUL":
                    newPolDesc = "Wrap up Liability";
                    break;
                case "CACC":
                    newPolDesc = "Commercial Accident";
                    break;
                case "CAVI":
                    newPolDesc = "Commercial Aviation";
                    break;
                case "CBOA":
                    newPolDesc = "Administration Bond";
                    break;
                case "CBOD":
                    newPolDesc = "Deposit Bond";
                    break;
                case "CBOF":
                    newPolDesc = "Final Bond";
                    break;
                case "CBON":
                    newPolDesc = "Bond - Administration/Misc";
                    break;
                case "CLEG":
                    newPolDesc = "Legal Expense";
                    break;
                case "CMAL":
                    newPolDesc = "Medical Malpractice";
                    break;
                case "CMOR":
                    newPolDesc = "Animal Mortality";
                    break;
                case "CNOA":
                    newPolDesc = "Commercial Non-Owned Auto";
                    break;
                case "CBBP":
                    newPolDesc = "Boat Builder's Package";
                    break;
                case "CFGS":
                    newPolDesc = "Fisherman's Gear & Shed Program";
                    break;
                case "CFPF":
                    newPolDesc = "Fish Plant Fire";
                    break;
                case "CFPS":
                    newPolDesc = "Commercial Fish Plant Stock";
                    break;
                case "CMAB":
                    newPolDesc = "Marine Builder's Risk";
                    break;

            }
            return newPolDesc;
        }

        public static (int, int, int, int) MiscCovField(int MiscCounter)
        {
            string CovCodeNum = "";
            string DedNum = "";
            string LimitNum = "";
            string PremNum = "";
            switch (MiscCounter)
            {
                case 0:
                    CovCodeNum = "";
                    DedNum = "";
                    LimitNum = "";
                    PremNum = "";
                    break;
                case 1:
                    CovCodeNum = "205";
                    DedNum = "197";
                    LimitNum = "189";
                    PremNum = "215";
                    break;
                case 2:
                    CovCodeNum = "204";
                    DedNum = "196";
                    LimitNum = "188";
                    PremNum = "214";
                    break;
                case 3:
                    CovCodeNum = "203";
                    DedNum = "195";
                    LimitNum = "187";
                    PremNum = "213";
                    break;
                case 4:
                    CovCodeNum = "202";
                    DedNum = "194";
                    LimitNum = "186";
                    PremNum = "212";
                    break;
                case 5:
                    CovCodeNum = "201";
                    DedNum = "193";
                    LimitNum = "185";
                    PremNum = "211";
                    break;                    
            }
            return (Convert.ToInt32(CovCodeNum.ToString()), Convert.ToInt32(DedNum.ToString()), Convert.ToInt32(LimitNum.ToString()), Convert.ToInt32(PremNum.ToString()));
        }


        public static (int, int, int, int) FreeFormField(int FFCounter)
        {
            string FFTextNum = "";
            string FFDedNum = "";
            string FFLimitNum = "";
            string FFPremNum = "";
            switch (FFCounter)
            {
                case 0:
                    FFTextNum = "";
                    FFDedNum = "";
                    FFLimitNum = "";
                    FFPremNum = "";
                    break;
                case 1:
                    FFTextNum = "199";
                    FFDedNum = "191";
                    FFLimitNum = "183";
                    FFPremNum = "209";
                    break;
                case 2:
                    FFTextNum = "198";
                    FFDedNum = "190";
                    FFLimitNum = "182";
                    FFPremNum = "208";
                    break;
                case 3:
                    FFTextNum = "173";
                    FFDedNum = "166";
                    FFLimitNum = "165";
                    FFPremNum = "164";
                    break;

            }
            return (Convert.ToInt32(FFTextNum.ToString()), Convert.ToInt32(FFDedNum.ToString()), Convert.ToInt32(FFLimitNum.ToString()), Convert.ToInt32(FFPremNum.ToString()));
        }

        public static (int, int, int, int, int, int) ContEqupField(int CEQCounter)
        {
            string CEDropDown = "";
            string CECovForm = "";
            string CEDed = "";
            string CECoIns = "";
            string CELimit = "";
            string CEPrem = "";
            switch (CEQCounter)
            {
                case 0:
                    CEDropDown = "";
                    CECovForm = "";
                    CEDed = "";
                    CECoIns = "";
                    CELimit = "";
                    CEPrem = "";
                    break;
                case 1:
                    CEDropDown = "162";
                    CECovForm = "154";
                    CEDed = "138";
                    CECoIns = "130";
                    CELimit = "123";
                    CEPrem = "115";
                    break;
                case 2:
                    CEDropDown = "161";
                    CECovForm = "153";
                    CEDed = "137";
                    CECoIns = "129";
                    CELimit = "122";
                    CEPrem = "113";
                    break;
                case 3:
                    CEDropDown = "160";
                    CECovForm = "152";
                    CEDed = "136";
                    CECoIns = "128";
                    CELimit = "121";
                    CEPrem = "111";
                    break;
                case 4:
                    CEDropDown = "159";
                    CECovForm = "151";
                    CEDed = "135";
                    CECoIns = "127";
                    CELimit = "120";
                    CEPrem = "109";
                    break;
                case 5:
                    CEDropDown = "158";
                    CECovForm = "150";
                    CEDed = "134";
                    CECoIns = "126";
                    CELimit = "119";
                    CEPrem = "107";
                    break;
            }
            return (Convert.ToInt32(CEDropDown.ToString()), Convert.ToInt32(CECovForm.ToString()), Convert.ToInt32(CEDed.ToString()), Convert.ToInt32(CECoIns.ToString()), Convert.ToInt32(CELimit.ToString()), Convert.ToInt32(CEPrem.ToString()));
        }

        // Cargo schedules
        public static (int, int, int, int, int, int, int) CargoFields(int Ccounter)
        {
            string Cyear = "";
            string Cmake = "";
            string Cmodel = "";
            string Cvin = "";
            string Cdeduct = "";
            string Climit = "";
            string Cpremium = "";
            switch (Ccounter)
            {
                // Testing Feedback done
                //case 0:
                //    Cyear = "71";
                //    Cmake = "65";
                //    Cmodel = "58";
                //    Cvin = "51";
                //    Cdeduct = "37";
                //    Climit = "30";
                //    Cpremium = "7";
                //    break;
                case 0:
                    Cyear = "70";
                    Cmake = "64";
                    Cmodel = "57";
                    Cvin = "50";
                    Cdeduct = "36";
                    Climit = "29";
                    Cpremium = "6";
                    break;
                case 1:
                    Cyear = "69";
                    Cmake = "63";
                    Cmodel = "56";
                    Cvin = "49";
                    Cdeduct = "35";
                    Climit = "28";
                    Cpremium = "5";
                    break;
                case 2:
                    Cyear = "18";
                    Cmake = "62";
                    Cmodel = "55";
                    Cvin = "48";
                    Cdeduct = "34";
                    Climit = "27";
                    Cpremium = "4";
                    break;
                case 3:
                    Cyear = "68";
                    Cmake = "61";
                    Cmodel = "54";
                    Cvin = "47";
                    Cdeduct = "33";
                    Climit = "26";
                    Cpremium = "3";
                    break;
                case 4:
                    Cyear = "67";
                    Cmake = "60";
                    Cmodel = "53";
                    Cvin = "46";
                    Cdeduct = "32";
                    Climit = "25";
                    Cpremium = "2";
                    break;
                case 5:
                    Cyear = "66";
                    Cmake = "59";
                    Cmodel = "52";
                    Cvin = "45";
                    Cdeduct = "31";
                    Climit = "24";
                    Cpremium = "1";
                    break;
            }
            return (Convert.ToInt32(Cyear.ToString()), Convert.ToInt32(Cmake.ToString()), Convert.ToInt32(Cmodel.ToString()), Convert.ToInt32(Cvin.ToString()), Convert.ToInt32(Cdeduct.ToString()), Convert.ToInt32(Climit.ToString()), Convert.ToInt32(Cpremium.ToString()));
        }

        public static (int, int, int) ProLibNums (int RowNum)
        {
            string ded = "";
            string lim = "";
            string pre = "";
            switch (RowNum)
            {
                case 62:
                    ded = "65";
                    lim = "64";
                    pre = "63";
                    break;
                case 40:
                    ded = "61";
                    lim = "54";
                    pre = "47";
                    break;
                case 39:
                    ded = "60";
                    lim = "53";
                    pre = "46";
                    break;
                case 38:
                    ded = "59";
                    lim = "52";
                    pre = "45";
                    break;
                case 37:
                    ded = "58";
                    lim = "51";
                    pre = "44";
                    break;
                case 36:
                    ded = "57";
                    lim = "50";
                    pre = "43";
                    break;
                case 35:
                    ded = "56";
                    lim = "49";
                    pre = "42";
                    break;
                case 34:
                    ded = "55";
                    lim = "48";
                    pre = "41";
                    break;
            }
            return (Convert.ToInt32(ded.ToString()), Convert.ToInt32(lim.ToString()), Convert.ToInt32(pre.ToString()));
        }

        public static (int, int, int, int) BSCA2ProLib2 (int counter)
        {
            string liab = "";
            string ded = "";
            string lim = "";
            string pre = "";
            switch (counter)
            {
                case 1:
                    liab = "90";
                    ded = "102";
                    lim = "105";
                    pre = "109";
                    break;
                case 2:
                    liab = "88";
                    ded = "117";
                    lim = "101";
                    pre = "103";
                    break;
            }
            return (Convert.ToInt32(liab.ToString()), Convert.ToInt32(ded.ToString()), Convert.ToInt32(lim.ToString()), Convert.ToInt32(pre.ToString()));
        }

        public static (int, int, int, int) BSCA2ProLib4(int counter)
        {
            string liab = "";
            string ded = "";
            string lim = "";
            string pre = "";
            switch (counter)
            {
                case 1:
                    liab = "57";
                    ded = "72";
                    lim = "51";
                    pre = "98";
                    break;
                case 2:
                    liab = "59";
                    ded = "73";
                    lim = "52";
                    pre = "91";
                    break;
                case 3:
                    liab = "64";
                    ded = "74";
                    lim = "53";
                    pre = "87";
                    break;
                case 4:
                    liab = "65";
                    ded = "75";
                    lim = "54";
                    pre = "86";
                    break;
                case 5:
                    liab = "67";
                    ded = "77";
                    lim = "55";
                    pre = "85";
                    break;
                case 6:
                    liab = "69";
                    ded = "79";
                    lim = "56";
                    pre = "83";
                    break;
            }
            return (Convert.ToInt32(liab.ToString()), Convert.ToInt32(ded.ToString()), Convert.ToInt32(lim.ToString()),  Convert.ToInt32(pre.ToString()));
        }


        public static (int, int, int, int, int, int) SFPropertyMapping(string itemValue)
        {
            string CovForm = "";
            string Val = "";
            string CoIns = "";
            string Ded = "";
            string Limit = "";
            string Prem = "";
            switch (itemValue)
            {
                case "1":
                    CovForm = "200";
                    Val = "180";
                    CoIns = "158";
                    Ded = "137";
                    Limit = "116";
                    Prem = "72";
                    break;
                case "2A":
                    CovForm = "199";
                    Val = "179";
                    CoIns = "157";
                    Ded = "136";
                    Limit = "115";
                    Prem = "70";
                    break;
                case "3":
                    CovForm = "198";
                    Val = "178";
                    CoIns = "156";
                    Ded = "135";
                    Limit = "114";
                    Prem = "68";
                    break;
                case "6":
                    CovForm = "197";
                    Val = "177";
                    CoIns = "155";
                    Ded = "134";
                    Limit = "113";
                    Prem = "66";
                    break;
                case "7":
                    CovForm = "196";
                    Val = "176";
                    CoIns = "154";
                    Ded = "133";
                    Limit = "112";
                    Prem = "43";
                    break;
                case "23":
                    CovForm = "194";
                    Val = "174";
                    CoIns = "152";
                    Ded = "131";
                    Limit = "110";
                    Prem = "62";
                    break;
                case "21":
                    CovForm = "193";
                    Val = "173";
                    CoIns = "151";
                    Ded = "130";
                    Limit = "109";
                    Prem = "60";
                    break;
                case "22":
                    CovForm = "192";
                    Val = "172";
                    CoIns = "150";
                    Ded = "129";
                    Limit = "108";
                    Prem = "58";
                    break;
                case "5":
                    CovForm = "191";
                    Val = "171";
                    CoIns = "149";
                    Ded = "128";
                    Limit = "107";
                    Prem = "56";
                    break;


            }
            return (Convert.ToInt32(CovForm.ToString()), Convert.ToInt32(Val.ToString()), Convert.ToInt32(CoIns.ToString()), Convert.ToInt32(Ded.ToString()), Convert.ToInt32(Limit.ToString()), Convert.ToInt32(Prem.ToString()));
        }
        public static string SFPropertyDesc (string code)
        {
            string desc = "";
            switch (code)
            {
                case "20":
                    desc = "Broad Form Extensions";
                    break;
                case "1":
                    desc = "Building";
                    break;
                case "7":
                    desc = "Contents of Every Description";
                    break;
                case "16":
                    desc = "Contractor's Equip - Rental Equipment";
                    break;
                case "15":
                    desc = "Contractor's Equipment";
                    break;
                case "17":
                    desc = "Contractor's Equipment - Rental Reimbursement";
                    break;
                case "21":
                    desc = "Earthquake";
                    break;
                case "10":
                    desc = "EDP - Data/Media";
                    break;
                case "9":
                    desc = "EDP - Equipment";
                    break;
                case "13":
                    desc = "EDP - Extra Expense";
                    break;
                case "11":
                    desc = "EDP - Laptop(s)";
                    break;
                case "14":
                    desc = "EDP - Mechanical Breakdown";
                    break;
                case "12":
                    desc = "EDP - Transit";
                    break;
                case "2A":
                    desc = "Equipment";
                    break;
                case "22":
                    desc = "Flood";
                    break;
                case "26":
                    desc = "Installation Floater";
                    break;
                case "4":
                    desc = "Inventory";
                    break;
                case "8":
                    desc = "Office Contents";
                    break;
                case "25":
                    desc = "Other";
                    break;
                case "6":
                    desc = "Property of Every Description (POED)";
                    break;
                case "24":
                    desc = "Rental Income";
                    break;
                case "23":
                    desc = "Sewer Backup";
                    break;
                case "3":
                    desc = "Stock";
                    break;
                case "5":
                    desc = "Tenants/Leasehold Improvements";
                    break;
                case "18":
                    desc = "Tool Floater";
                    break;
                case "19":
                    desc = "Transportation Floater";
                    break;

            }
            return desc;
        }

        public static (int, int, int, int, int, int, int) SFFreeFormMapping(int SFMCounter)
        {
            string ValField = "";
            string CovForm = "";
            string Val = "";
            string CoIns = "";
            string Ded = "";
            string Limit = "";
            string Prem = "";
            switch (SFMCounter)
            {
                case 1:
                    ValField = "41";
                    CovForm = "188";
                    Val = "168";
                    CoIns = "146";
                    Ded = "125";
                    Limit = "104";
                    Prem = "52";
                    break;
                case 2:
                    ValField = "92";
                    CovForm = "187";
                    Val = "167";
                    CoIns = "145";
                    Ded = "124";
                    Limit = "103";
                    Prem = "51";
                    break;
                case 3:
                    ValField = "91";
                    CovForm = "186";
                    Val = "166";
                    CoIns = "144";
                    Ded = "123";
                    Limit = "102";
                    Prem = "50";
                    break;
                case 4:
                    ValField = "90";
                    CovForm = "185";
                    Val = "165";
                    CoIns = "143";
                    Ded = "122";
                    Limit = "101";
                    Prem = "49";
                    break;
                case 5:
                    ValField = "89";
                    CovForm = "184";
                    Val = "164";
                    CoIns = "142";
                    Ded = "121";
                    Limit = "100";
                    Prem = "48";
                    break;
                case 6:
                    ValField = "88";
                    CovForm = "183";
                    Val = "163";
                    CoIns = "141";
                    Ded = "120";
                    Limit = "99";
                    Prem = "47";
                    break;
                case 7:
                    ValField = "87";
                    CovForm = "182";
                    Val = "162";
                    CoIns = "140";
                    Ded = "119";
                    Limit = "98";
                    Prem = "46";
                    break;
                case 8:
                    ValField = "86";
                    CovForm = "181";
                    Val = "161";
                    CoIns = "139";
                    Ded = "118";
                    Limit = "97";
                    Prem = "45";
                    break;
                case 9:
                    ValField = "85";
                    CovForm = "160";
                    Val = "159";
                    CoIns = "138";
                    Ded = "117";
                    Limit = "96";
                    Prem = "44";
                    break;

            }
            return (Convert.ToInt32(ValField.ToString()), Convert.ToInt32(CovForm.ToString()), Convert.ToInt32(Val.ToString()), Convert.ToInt32(CoIns.ToString()), Convert.ToInt32(Ded.ToString()), Convert.ToInt32(Limit.ToString()), Convert.ToInt32(Prem.ToString()));
        }

        public static (int, int, int, int, int, int) SFBIMapping(int SFBIcounter)
        {
            string CovVal = "";
            string Indem = "";
            string Wait = "";
            string CoIns = "";
            string Limit = "";
            string Prem = "";
            switch (SFBIcounter)
            {
                case 1:
                    CovVal = "252";
                    Indem = "251";
                    Wait = "238";
                    CoIns = "241";
                    Limit = "249";
                    Prem = "239";
                    break;
                case 2:
                    CovVal = "232";
                    Indem = "228";
                    Wait = "220";
                    CoIns = "234";
                    Limit = "226";
                    Prem = "224";
                    break;
                case 3:
                    CovVal = "236";
                    Indem = "221";
                    Wait = "240";
                    CoIns = "246";
                    Limit = "217";
                    Prem = "250";
                    break;
            }
            return (Convert.ToInt32(CovVal.ToString()), Convert.ToInt32(Indem.ToString()), Convert.ToInt32(Wait.ToString()), Convert.ToInt32(CoIns.ToString()), Convert.ToInt32(Limit.ToString()), Convert.ToInt32(Prem.ToString()));
        }

        public static (int, int, int) EBdescMapping (int EBElement)
        {
            string EBDded = "";
            string EBDlimit = "";
            string EBDprem = "";
            switch (EBElement)
            {
                case 255:
                    EBDded = "270";
                    EBDlimit = "260";
                    EBDprem = "248";
                    break;
                case 242:
                    EBDded = "269";
                    EBDlimit = "233";
                    EBDprem = "227";
                    break;
                case 265:
                    EBDded = "252";
                    EBDlimit = "250";
                    EBDprem = "263";
                    break;
                case 241:
                    EBDded = "237";
                    EBDlimit = "232";
                    EBDprem = "226";
                    break;
                case 254:
                    EBDded = "261";
                    EBDlimit = "264";
                    EBDprem = "247";
                    break;
                case 240:
                    EBDded = "236";
                    EBDlimit = "231";
                    EBDprem = "225";
                    break;
                case 262:
                    EBDded = "251";
                    EBDlimit = "249";
                    EBDprem = "258";
                    break;
                case 239:
                    EBDded = "235";
                    EBDlimit = "230";
                    EBDprem = "224";
                    break;
                case 253:
                    EBDded = "266";
                    EBDlimit = "259";
                    EBDprem = "246";
                    break;
                case 238:
                    EBDded = "234";
                    EBDlimit = "228";
                    EBDprem = "223";
                    break;
            }
            return (Convert.ToInt32(EBDded.ToString()), Convert.ToInt32(EBDlimit.ToString()), Convert.ToInt32(EBDprem.ToString()));
        }

        public static (int, int, int) SFliabMapping(int LiabIndex)
        {
            string CGLded = "";
            string CGLlimit = "";
            string CGLprem = "";
            switch (LiabIndex)
            {
                case 91:
                    CGLded = "83";
                    CGLlimit = "79";
                    CGLprem = "81";
                    break;
                case 88:
                    CGLded = "82";
                    CGLlimit = "78";
                    CGLprem = "80";
                    break;
                case 87:
                    CGLded = "58";
                    CGLlimit = "57";
                    CGLprem = "56";
                    break;
                case 90:
                    CGLded = "61";
                    CGLlimit = "60";
                    CGLprem = "59";
                    break;
                case 89:
                    CGLded = "64";
                    CGLlimit = "63";
                    CGLprem = "62";
                    break;
                case 86:
                    CGLded = "67";
                    CGLlimit = "66";
                    CGLprem = "65";
                    break;
                case 85:
                    CGLded = "77";
                    CGLlimit = "69";
                    CGLprem = "68";
                    break;
                case 45:
                    CGLded = "41";
                    CGLlimit = "40";
                    CGLprem = "39";
                    break;
                case 44:
                    CGLded = "38";
                    CGLlimit = "37";
                    CGLprem = "36";
                    break;

            }
            return (Convert.ToInt32(CGLded.ToString()), Convert.ToInt32(CGLlimit.ToString()), Convert.ToInt32(CGLprem.ToString()));
        }


        public static (int, int, int, int) SFmiscCrimeMapping (int SFMCcounter)
        {
            string MCCoverageCode = "";
            string MCded = "";
            string MClimit = "";
            string MCprem = "";
            switch (SFMCcounter)
            {
                case 1:
                    MCCoverageCode = "205";
                    MCded = "197";
                    MClimit = "189";
                    MCprem = "215";
                    break;
                case 2:
                    MCCoverageCode = "204";
                    MCded = "196";
                    MClimit = "188";
                    MCprem = "214";
                    break;
                case 3:
                    MCCoverageCode = "203";
                    MCded = "195";
                    MClimit = "187";
                    MCprem = "213";
                    break;
                case 4:
                    MCCoverageCode = "202";
                    MCded = "194";
                    MClimit = "186";
                    MCprem = "212";
                    break;
                case 5:
                    MCCoverageCode = "201";
                    MCded = "193";
                    MClimit = "185";
                    MCprem = "211";
                    break;
                case 6:
                    MCCoverageCode = "200";
                    MCded = "192";
                    MClimit = "184";
                    MCprem = "212";
                    break;
                case 7:
                    MCCoverageCode = "172";
                    MCded = "170";
                    MClimit = "169";
                    MCprem = "168";
                    break;
            }
            return (Convert.ToInt32(MCCoverageCode.ToString()), Convert.ToInt32(MCded.ToString()), Convert.ToInt32(MClimit.ToString()), Convert.ToInt32(MCprem.ToString()));
        }

        public static string SFCrimeCoding (string OldSFCode)
        {
            string NewSFCode = "";
            switch (OldSFCode)
            {
                case "202":
                    NewSFCode = "ED";
                    break;
                case "207":
                    NewSFCode = "ED";
                    break;
                case "204":
                    NewSFCode = "MOC";
                    break;
                case "205":
                    NewSFCode = "DFC";
                    break;
                case "203":
                    NewSFCode = "LI";
                    break;
                case "206":
                    NewSFCode = "Damage to Building by Burglary or Robbery";
                    break;
                case "209":
                    NewSFCode = "Destruction Bond";
                    break;
                case "208":
                    NewSFCode = "Disappearance";
                    break;
                case "201":
                    NewSFCode = "Dishonesty";
                    break;
            }
            return NewSFCode;
        }


        public static string SFCECoding(string OldSFCECode)
        {
            string NewSFCECode = "";
            switch (OldSFCECode)
            {
                case "15":
                    NewSFCECode = "CEF";
                    break;
                case "26":
                    NewSFCECode = "IF";
                    break;
                case "18":
                    NewSFCECode = "TFM";
                    break;
                case "16":
                    NewSFCECode = "RCE";
                    break;
                case "17":
                    NewSFCECode = "Contractor's Equip - Rental Reimbursement";
                    break;
            }
            return NewSFCECode;
        }


        public static (int, int, int, int, int, int, int) SFMCContEquipMap (int CEindex)
        {
            string SCEcode = "";
            string SCEcovform = "";
            string SCEval = "";
            string SCEded = "";
            string SCEcoin = "";
            string SCElimit = "";
            string SCEprem = "";
            switch (CEindex)
            {
                case 1:
                    SCEcode = "162";
                    SCEcovform = "154";
                    SCEval = "146";
                    SCEded = "138";
                    SCEcoin = "130";
                    SCElimit = "123";
                    SCEprem = "115";
                    break;
                case 2:
                    SCEcode = "161";
                    SCEcovform = "153";
                    SCEval = "145";
                    SCEded = "137";
                    SCEcoin = "129";
                    SCElimit = "122";
                    SCEprem = "113";
                    break;
                case 3:
                    SCEcode = "160";
                    SCEcovform = "152";
                    SCEval = "144";
                    SCEded = "136";
                    SCEcoin = "128";
                    SCElimit = "121";
                    SCEprem = "111";
                    break;
                case 4:
                    SCEcode = "159";
                    SCEcovform = "151";
                    SCEval = "143";
                    SCEded = "135";
                    SCEcoin = "127";
                    SCElimit = "120";
                    SCEprem = "109";
                    break;
                case 5:
                    SCEcode = "158";
                    SCEcovform = "150";
                    SCEval = "142";
                    SCEded = "134";
                    SCEcoin = "126";
                    SCElimit = "119";
                    SCEprem = "107";
                    break;
                case 6:
                    SCEcode = "157";
                    SCEcovform = "149";
                    SCEval = "141";
                    SCEded = "133";
                    SCEcoin = "125";
                    SCElimit = "118";
                    SCEprem = "105";
                    break;
            }
            return (Convert.ToInt32(SCEcode.ToString()), Convert.ToInt32(SCEcovform.ToString()), Convert.ToInt32(SCEval.ToString()), Convert.ToInt32(SCEded.ToString()), Convert.ToInt32(SCEcoin.ToString()), Convert.ToInt32(SCElimit.ToString()), Convert.ToInt32(SCEprem.ToString()));
        }




            
        public bool LongShortFormUpdate(int oPolId, int nPolId)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.PolicyFilter oPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.PolicyGetResult oPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.Policy oPol = new CBLServiceReference.Policy();
            oPolicyFilter.PolicyID = oPolId;
            oPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, oPolicyFilter, 0);
            oPol = oPolicyResult.Policies[0];
            CBLServiceReference.LineGetResult1 oLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter oLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 olne = new CBLServiceReference.Line1();
            oLineFilter.PolicyID = oPolId;
            oLineResult = EpicSDKClient.Get_Line(oMessageHeader, oLineFilter, 0);
            olne = oLineResult.Lines[0];
            int oLineID = olne.LineID;
            bool InitialLSFormStatus = false;
            

            CBLServiceReference.PolicyFilter nPolicyFilter = new CBLServiceReference.PolicyFilter();
            CBLServiceReference.PolicyGetResult nPolicyResult = new CBLServiceReference.PolicyGetResult();
            CBLServiceReference.Policy nPol = new CBLServiceReference.Policy();
            nPolicyFilter.PolicyID = nPolId;
            nPolicyResult = EpicSDKClient.Get_Policy(oMessageHeader, nPolicyFilter, 0);
            nPol = nPolicyResult.Policies[0];
            CBLServiceReference.LineGetResult1 nLineResult = new CBLServiceReference.LineGetResult1();
            CBLServiceReference.LineFilter nLineFilter = new CBLServiceReference.LineFilter();
            CBLServiceReference.Line1 nlne = new CBLServiceReference.Line1();
            nLineFilter.PolicyID = nPolId;
            nLineResult = EpicSDKClient.Get_Line(oMessageHeader, nLineFilter, 0);
            nlne = nLineResult.Lines[0];
            int nLineID = nlne.LineID;

            CBLServiceReference.Get_CustomFormResponse oCFormResponse = new CBLServiceReference.Get_CustomFormResponse();
            oCFormResponse.Get_CustomFormResult = EpicSDKClient.Get_CustomForm(oMessageHeader, oLineID);
            CBLServiceReference.CustomForm oCFR = oCFormResponse.Get_CustomFormResult[0];

            CBLServiceReference.Get_CustomFormResponse nCFormResponse = new CBLServiceReference.Get_CustomFormResponse();
            nCFormResponse.Get_CustomFormResult = EpicSDKClient.Get_CustomForm(oMessageHeader, nLineID);
            CBLServiceReference.CustomForm nCFR = nCFormResponse.Get_CustomFormResult[0];

            CBLServiceReference.SupplementalScreen oSupScr = new CBLServiceReference.SupplementalScreen();
            int SchduleScreenCount = EpicSDKClient.Get_CustomForm_SupplementalScreen(oMessageHeader, oLineID, 0, CBLServiceReference.SupplementalScreenGetType.LineID).Length;


            var IBCreader = new StreamReader(File.OpenRead(IBCCodePath));
            List<string> IBCList = new List<string>();
            while (!IBCreader.EndOfStream)
            {
                var line = IBCreader.ReadLine();
                var values = line.Split(';');

                IBCList.Add(values[0]);
            }

           



            string LiabOccurVal = oCFR.NonScheduledScreens[2].Fields[111].FieldValue;
            if (LiabOccurVal.Trim() != "") //Checking if it's a long form
            {
                Console.WriteLine("This is a Long Form");
                Console.WriteLine("*-*-*-*-*-*-*-*-*-*");

                // Client Information
                try
                {

                
                    //1
                    string nLegalEntity = "";
                    switch (oCFR.NonScheduledScreens[0].Fields[32].FieldValue)
                    {
                        case "CP":
                            nLegalEntity = "COR";
                            break;
                        case "IN":
                            nLegalEntity = "IND";
                            break;
                        case "JV":
                            nLegalEntity = "JV";
                            break;
                        case "PT":
                            nLegalEntity = "LLP";
                            break;
                        case "LP":
                            nLegalEntity = "LLP";
                            break;
                        default:
                            nLegalEntity = "OTH";
                            break;
                    }
                    nCFR.NonScheduledScreens[0].Fields[50].FieldValue = nLegalEntity;
                    //2
                    nCFR.NonScheduledScreens[0].Fields[51].FieldValue = oCFR.NonScheduledScreens[0].Fields[36].FieldValue+" "+ oCFR.NonScheduledScreens[0].Fields[35].FieldValue+" "+ oCFR.NonScheduledScreens[0].Fields[37].FieldValue;
                    //3 - N/A
                    //4 
                    if (oCFR.ScheduledScreens[3].Items.Count > 0)
                    {
                        nCFR.NonScheduledScreens[0].Fields[52].FieldValue = oCFR.ScheduledScreens[3].Items[0][11].FieldValue;
                    }
                    //5 - N/A
                    //6
                    // Testing Feedback - Done
                    if (oCFR.ScheduledScreens[3].Items.Count > 0)
                    {
                        string IBCpresent = oCFR.ScheduledScreens[3].Items[0][12].FieldValue;
                        if (IBCpresent != "")
                        {
                            string oIBC = oCFR.ScheduledScreens[3].Items[0][12].FieldValue.Trim();
                            if (IBCList.Contains(oIBC))
                            {
                                if (oIBC.Length < 4)
                                {
                                    oIBC = 0 + oIBC;
                                }
                                nCFR.NonScheduledScreens[0].Fields[53].FieldValue = oIBC;

                            }
                        }
                    }
                

                
                    //7 N/A
                    //8 N/A
                    //9
                    if (oCFR.ScheduledScreens[3].Items.Count > 0)
                    {
                        string BusStartDate = oCFR.ScheduledScreens[3].Items[0][8].FieldValue;
                        if (BusStartDate != "")
                        {
                            BusStartDate = BusStartDate.Substring(BusStartDate.Length - 4);
                            nCFR.NonScheduledScreens[0].Fields[37].FieldValue = BusStartDate;
                        }
                    
                    }
                    //10
                    nCFR.NonScheduledScreens[0].Fields[36].FieldValue = oCFR.NonScheduledScreens[0].Fields[34].FieldValue;
                    //11 N/A
                    //12 N/A
                    //13 N/A
                    //14 N/A
                    //15
                    if (oCFR.ScheduledScreens[3].Items.Count > 0)
                    {
                        int AnnualReceiptsCanada = 0;
                        int AnnualReceiptsUS = 0;
                        int AnnualReceiptsForeign = 0;
                        if (oCFR.ScheduledScreens[3].Items[0][6].FieldValue != "")
                        {
                            AnnualReceiptsCanada = Convert.ToInt32(oCFR.ScheduledScreens[3].Items[0][6].FieldValue.ToString());
                        }
                        if (oCFR.ScheduledScreens[3].Items[0][3].FieldValue != "")
                        {
                            AnnualReceiptsUS = Convert.ToInt32(oCFR.ScheduledScreens[3].Items[0][3].FieldValue.ToString());
                        }
                        if (oCFR.ScheduledScreens[3].Items[0][4].FieldValue != "")
                        {
                            AnnualReceiptsForeign = Convert.ToInt32(oCFR.ScheduledScreens[3].Items[0][4].FieldValue.ToString());
                        }
                        int TotalRevenue = AnnualReceiptsCanada + AnnualReceiptsUS + AnnualReceiptsForeign;
                        nCFR.NonScheduledScreens[0].Fields[38].FieldValue = TotalRevenue.ToString();
                    }
                    //16 N/A
                    //17 N/A
                    //18 N/A
                    //19 N/A
                    //20 N/A
                    //21 N/A
                    //22 N/A
                    //23
                    if (oCFR.ScheduledScreens[3].Items.Count > 0)
                    {
                        int AnnualPayCanada = 0;
                        int AnnualPayUS = 0;
                        int AnnualPayForeign = 0;
                        if (oCFR.ScheduledScreens[3].Items[0][7].FieldValue != "")
                        {
                            AnnualPayCanada = Convert.ToInt32(oCFR.ScheduledScreens[3].Items[0][7].FieldValue.ToString());
                        }
                        if (oCFR.ScheduledScreens[3].Items[0][9].FieldValue != "")
                        {
                            AnnualPayUS = Convert.ToInt32(oCFR.ScheduledScreens[3].Items[0][9].FieldValue.ToString());
                        }
                        if (oCFR.ScheduledScreens[3].Items[0][13].FieldValue != "")
                        {
                            AnnualPayForeign = Convert.ToInt32(oCFR.ScheduledScreens[3].Items[0][13].FieldValue.ToString());
                        }
                        int TotalPay = AnnualPayCanada + AnnualPayUS + AnnualPayForeign;
                        nCFR.NonScheduledScreens[0].Fields[44].FieldValue = TotalPay.ToString();
                    }
                    //24 N/A
                    //25 N/A
                    //26 N/A
                    //27 N/A
                    //28 N/A
                
                    if (oCFR.ScheduledScreens[1].Items.Count > 0)
                    {
                        //29
                        nCFR.NonScheduledScreens[0].Fields[28].FieldValue = "Yes";


                        // Ordering losses by recent date of Loss
                        int LossItemCount = oCFR.ScheduledScreens[1].Items.Count;
                        var lossList = new List<Dictionary<string, dynamic>>();
                        for (int i = 0; i < LossItemCount; i++)
                        {
                            string date = oCFR.ScheduledScreens[1].Items[i][1].FieldValue;
                            if (date != "")
                            {
                                DateTime iDate = DateTime.ParseExact(date, "MM/dd/yyyy",
                                               System.Globalization.CultureInfo.InvariantCulture);
                                lossList.Add(new Dictionary<string, dynamic>()
                                {   {"Counter", i },
                                    {"LossDate", iDate}
                                });
                            }
                        

                        }
                        var SortedLossList = lossList.OrderByDescending(x => x["LossDate"]);
                        List<int> LossCounter = new List<int>();
                        foreach (Dictionary<string, dynamic> d in SortedLossList)
                        { LossCounter.Add(d["Counter"]);
                        }

                        int MostRecentLoss = LossCounter[0];
                        //30
                        nCFR.NonScheduledScreens[0].Fields[27].FieldValue = oCFR.ScheduledScreens[1].Items[MostRecentLoss][1].FieldValue;
                        //31 - N/A
                        //32
                        nCFR.NonScheduledScreens[0].Fields[23].FieldValue = oCFR.ScheduledScreens[1].Items[MostRecentLoss][7].FieldValue;
                        //33
                        nCFR.NonScheduledScreens[0].Fields[20].FieldValue = oCFR.ScheduledScreens[1].Items[MostRecentLoss][4].FieldValue;
                        //34
                        if (oCFR.ScheduledScreens[1].Items[MostRecentLoss][0].FieldValue == "C")
                        {
                            nCFR.NonScheduledScreens[0].Fields[18].FieldValue = "YES";
                        }
                        else
                        {
                            nCFR.NonScheduledScreens[0].Fields[18].FieldValue = "NO";
                        }
                        if (LossCounter.Count > 1)
                        {
                            int SecondRecentLoss = LossCounter[1];
                            //35
                            nCFR.NonScheduledScreens[0].Fields[26].FieldValue = oCFR.ScheduledScreens[1].Items[SecondRecentLoss][1].FieldValue;
                            //36 - N/A
                            //37
                            nCFR.NonScheduledScreens[0].Fields[22].FieldValue = oCFR.ScheduledScreens[1].Items[SecondRecentLoss][7].FieldValue;
                            //38
                            nCFR.NonScheduledScreens[0].Fields[19].FieldValue = oCFR.ScheduledScreens[1].Items[SecondRecentLoss][4].FieldValue;
                            //39
                            if (oCFR.ScheduledScreens[1].Items[SecondRecentLoss][0].FieldValue == "C")
                            {
                                nCFR.NonScheduledScreens[0].Fields[17].FieldValue = "YES";
                            }
                            else
                            {
                                nCFR.NonScheduledScreens[0].Fields[17].FieldValue = "NO";
                            }
                        }
                        //40-57 - N/A
                  
                    }
                    //Console.WriteLine("Client Infomation Done");
                    if (LongFormUpdateSwitch == 1)
                    {
                        EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check                        
                    }
                }
                catch (Exception e)
                {
                    string e2 = oPolId + " | Long Form - Client Info failed | " + e;
                    ErrorString = ErrorString + e2 + System.Environment.NewLine;
                    Console.WriteLine(e2);
                }

                // COPE
                try
                {

                
                    // Get number of locations
                    int LocationCount = oCFR.ScheduledScreens[0].Items.Count;
                    // Read for each property
                    for (int i = 0; i < LocationCount; i++)
                    {
                        // First insert a Location Item
                        string SID = nCFR.ScheduledScreens[0].ScheduleID;
                        CBLServiceReference.FieldItems[] FF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, SID);
                        nCFR.ScheduledScreens[0].Items.Insert(i, FF[0]);
                        // Add fields for a given location
                        //1
                        nCFR.ScheduledScreens[0].Items[i][36].FieldValue = oCFR.ScheduledScreens[0].Items[i][114].FieldValue;
                        //2
                        nCFR.ScheduledScreens[0].Items[i][25].FieldValue = oCFR.ScheduledScreens[0].Items[i][112].FieldValue;
                        //3
                        string BuildingType = "";
                        switch (oCFR.ScheduledScreens[0].Items[i][106].FieldValue)
                        {
                            case "I":
                                BuildingType = "IP";
                                break;
                            case "S":
                                BuildingType = "RSP";
                                break;
                            case "E":
                                BuildingType = "SM";
                                break;
                            case "OT":
                                BuildingType = "OT";
                                break;
                            case "D":
                                BuildingType = "OT";
                                break;
                            default:
                                BuildingType = "OT";
                                break;
                        }
                        nCFR.ScheduledScreens[0].Items[i][35].FieldValue = BuildingType;
                        //4
                        nCFR.ScheduledScreens[0].Items[i][34].FieldValue = oCFR.ScheduledScreens[0].Items[i][109].FieldValue;
                        //5 - 8 - N/A
                        //9
                        nCFR.ScheduledScreens[0].Items[i][29].FieldValue = oCFR.ScheduledScreens[0].Items[i][140].FieldValue;
                        //10 - N/A
                        //11
                        nCFR.ScheduledScreens[0].Items[i][27].FieldValue = oCFR.ScheduledScreens[0].Items[i][111].FieldValue;
                        //12 - N/A
                        //13
                        nCFR.ScheduledScreens[0].Items[i][24].FieldValue = oCFR.ScheduledScreens[0].Items[i][134].FieldValue;
                        //14
                        nCFR.ScheduledScreens[0].Items[i][23].FieldValue = oCFR.ScheduledScreens[0].Items[i][137].FieldValue;
                        //15
                        nCFR.ScheduledScreens[0].Items[i][22].FieldValue = oCFR.ScheduledScreens[0].Items[i][153].FieldValue;
                        //16
                        if (oCFR.ScheduledScreens[0].Items[i][150].FieldValue == "m2")
                        {
                            nCFR.ScheduledScreens[0].Items[i][21].FieldValue = "Sq Metre";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][150].FieldValue == "sq ft")
                        {
                            nCFR.ScheduledScreens[0].Items[i][21].FieldValue = "Sq Feet";
                        }
                        //17
                        nCFR.ScheduledScreens[0].Items[i][20].FieldValue = oCFR.ScheduledScreens[0].Items[i][152].FieldValue;
                        //18
                        if (oCFR.ScheduledScreens[0].Items[i][149].FieldValue == "m2")
                        {
                            nCFR.ScheduledScreens[0].Items[i][19].FieldValue = "Sq Metre";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][149].FieldValue == "sq ft")
                        {
                            nCFR.ScheduledScreens[0].Items[i][19].FieldValue = "Sq Feet";
                        }
                        //19
                        //if (oCFR.ScheduledScreens[0].Items[i][148].FieldValue != "")
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "FR";
                        //}
                        //else if (oCFR.ScheduledScreens[0].Items[i][147].FieldValue != "")
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MC";
                        //}
                        //else if (oCFR.ScheduledScreens[0].Items[i][163].FieldValue != "")
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MS";
                        //}
                        //else if (oCFR.ScheduledScreens[0].Items[i][146].FieldValue != "")
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "NC";
                        //}
                        //else if (oCFR.ScheduledScreens[0].Items[i][145].FieldValue != "")
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MV";
                        //}
                        //else if (oCFR.ScheduledScreens[0].Items[i][162].FieldValue != "")
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "FM";
                        //}
                        // testing feedback done
                        // Ranking system replaced by normal match to 100 
                        if (oCFR.ScheduledScreens[0].Items[i][148].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "FR";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][147].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MC";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][163].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MS";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][146].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "NC";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][145].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MV";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][162].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "FM";
                        }
                        //20 - N/A
                        //21
                        string HeatingSystem = oCFR.ScheduledScreens[0].Items[i][128].FieldValue;
                        string FuelType = oCFR.ScheduledScreens[0].Items[i][126].FieldValue;
                        if (HeatingSystem == "F" && FuelType == "N")
                        {
                            nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "CGF";
                        }
                        else if (HeatingSystem == "F" && FuelType == "O")
                        {
                            nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "COF";
                        }
                        else if (FuelType == "E")
                        {
                            nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "EH";
                        }
                        else if (HeatingSystem == "B" || HeatingSystem == "W" || HeatingSystem == "R")
                        {
                            nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "CHW";
                        }
                        //22
                        // Testing feedback Done
                        if (oCFR.ScheduledScreens[0].Items[i][158].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][17].FieldValue = "W";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][159].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][17].FieldValue = "CP";
                        }
                        //23
                        // Get a list of dict with new codeds > sort by desc > Pick first code
                        //var RoofList = new List<(string, int)>();

                        //if (oCFR.ScheduledScreens[0].Items[i][195].FieldValue != "")
                        //{
                        //    RoofList.Add(("CJ", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][195].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][181].FieldValue != "")
                        //{
                        //    RoofList.Add(("SD", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][181].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][191].FieldValue != "")
                        //{
                        //    RoofList.Add(("WJ", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][191].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][204].FieldValue != "")
                        //{
                        //    RoofList.Add(("O", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][204].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][201].FieldValue != "")
                        //{
                        //    RoofList.Add(("O", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][201].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][189].FieldValue != "")
                        //{
                        //    RoofList.Add(("O", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][189].FieldValue.ToString())));
                        //}
                        //RoofList = RoofList.OrderByDescending(t => t.Item2).ToList();
                        //if (RoofList.Count > 0)
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][14].FieldValue = RoofList[0].Item1;
                        //}
                        // testing feedback done
                        // Ranking system replaced by normal match to 100 
                        if (oCFR.ScheduledScreens[0].Items[i][195].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][14].FieldValue = "CJ";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][181].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][14].FieldValue = "SD";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][191].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][14].FieldValue = "WJ";
                        }


                        //24
                        // Get a list of dict with new codeds > sort by desc > Pick first code
                        //var PlumbingList = new List<(string, int)>();

                        //if (oCFR.ScheduledScreens[0].Items[i][202].FieldValue != "")
                        //{
                        //    PlumbingList.Add(("1", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][202].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][179].FieldValue != "")
                        //{
                        //    PlumbingList.Add(("12", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][179].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][183].FieldValue != "")
                        //{
                        //    PlumbingList.Add(("4", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][183].FieldValue.ToString())));
                        //}
                        //PlumbingList = PlumbingList.OrderByDescending(t => t.Item2).ToList();
                        //if (PlumbingList.Count > 0)
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][11].FieldValue = PlumbingList[0].Item1;
                        //}
                        // testing feedback done
                        // Ranking system replaced by normal match to 100 
                        if (oCFR.ScheduledScreens[0].Items[i][202].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][11].FieldValue = "1";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][179].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][11].FieldValue = "12";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][183].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][11].FieldValue = "4";
                        }
                        //25
                        if (oCFR.ScheduledScreens[0].Items[i][124].FieldValue != "" || oCFR.ScheduledScreens[0].Items[i][143].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[0].Items[i][16].FieldValue = "Yes";
                        }
                        //26
                        if (oCFR.ScheduledScreens[0].Items[i][168].FieldValue != "C")
                        {
                            nCFR.ScheduledScreens[0].Items[i][13].FieldValue = "B";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][168].FieldValue != "F")
                        {
                            nCFR.ScheduledScreens[0].Items[i][13].FieldValue = "B";
                        }
                        //27
                        // Get a list of dict with new codeds > sort by desc > Pick first code
                        //var WiringList = new List<(string, int)>();
                        //if (oCFR.ScheduledScreens[0].Items[i][200].FieldValue != "")
                        //{
                        //    WiringList.Add(("A", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][200].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][178].FieldValue != "")
                        //{
                        //    WiringList.Add(("C", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][178].FieldValue.ToString())));
                        //}
                        //if (oCFR.ScheduledScreens[0].Items[i][184].FieldValue != "")
                        //{
                        //    WiringList.Add(("KT", Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][184].FieldValue.ToString())));
                        //}
                        //WiringList = WiringList.OrderByDescending(t => t.Item2).ToList();
                        //if (WiringList.Count > 0)
                        //{
                        //    nCFR.ScheduledScreens[0].Items[i][10].FieldValue = WiringList[0].Item1;
                        //}

                        // testing feedback done
                        // Ranking system replaced by normal match to 100
                        if (oCFR.ScheduledScreens[0].Items[i][200].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][10].FieldValue = "A";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][178].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][10].FieldValue = "C";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][184].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][10].FieldValue = "KT";
                        }


                        //28 - N/A
                        //29 - 30
                        if (oCFR.ScheduledScreens[0].Items[i][89].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][8].FieldValue = oCFR.ScheduledScreens[0].Items[i][91].FieldValue;
                        }
                        else
                        {
                            nCFR.ScheduledScreens[0].Items[i][4].FieldValue = oCFR.ScheduledScreens[0].Items[i][91].FieldValue;
                        }
                        //31 - 32
                        if (oCFR.ScheduledScreens[0].Items[i][78].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][7].FieldValue = oCFR.ScheduledScreens[0].Items[i][80].FieldValue;
                        }
                        else
                        {
                            nCFR.ScheduledScreens[0].Items[i][3].FieldValue = oCFR.ScheduledScreens[0].Items[i][80].FieldValue;
                        }
                        //33 - 34
                        if (oCFR.ScheduledScreens[0].Items[i][82].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][6].FieldValue = oCFR.ScheduledScreens[0].Items[i][83].FieldValue;
                        }
                        else
                        {
                            nCFR.ScheduledScreens[0].Items[i][2].FieldValue = oCFR.ScheduledScreens[0].Items[i][83].FieldValue;
                        }
                        //35 - 36
                        if (oCFR.ScheduledScreens[0].Items[i][66].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[0].Items[i][5].FieldValue = oCFR.ScheduledScreens[0].Items[i][69].FieldValue;
                        }
                        else
                        {
                            nCFR.ScheduledScreens[0].Items[i][1].FieldValue = oCFR.ScheduledScreens[0].Items[i][69].FieldValue;
                        }
                        //37 - N/A
                        //38 - 39
                        if (oCFR.ScheduledScreens[0].Items[i][17].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[0].Items[i][39].FieldValue = "Yes";
                            nCFR.ScheduledScreens[0].Items[i][45].FieldValue = oCFR.ScheduledScreens[0].Items[i][17].FieldValue;
                        }
                        //40
                        if (oCFR.ScheduledScreens[0].Items[i][37].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[0].Items[i][38].FieldValue = "ULC";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][118].FieldValue == "C")
                        {
                            nCFR.ScheduledScreens[0].Items[i][38].FieldValue = "M";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][118].FieldValue == "L")
                        {
                            nCFR.ScheduledScreens[0].Items[i][38].FieldValue = "L";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][118].FieldValue == "N")
                        {
                            nCFR.ScheduledScreens[0].Items[i][38].FieldValue = "N";
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][118].FieldValue == "OT")
                        {
                            string RemarkVal = nCFR.ScheduledScreens[0].Items[i][42].FieldValue;
                            nCFR.ScheduledScreens[0].Items[i][42].FieldValue = RemarkVal + " -- Long form application had Alarm System - Other, BSCA2.0 does not have this option.";
                        }
                        //41
                        if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue != "")
                        {
                            if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue != "N/A" || oCFR.ScheduledScreens[0].Items[i][43].FieldValue != "n/a")
                            {
                                if (oCFR.ScheduledScreens[0].Items[i][44].FieldValue != "")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][41].FieldValue = "Yes";
                                }
                            }
                        }
                        //42
                        // Testing feedback done
                        if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "N/A" || oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "n/a")
                        {
                            nCFR.ScheduledScreens[0].Items[i][37].FieldValue = "0";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "1")
                        {
                            nCFR.ScheduledScreens[0].Items[i][37].FieldValue = "2";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "2")
                        {
                            nCFR.ScheduledScreens[0].Items[i][37].FieldValue = "3";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "3")
                        {
                            nCFR.ScheduledScreens[0].Items[i][37].FieldValue = "4";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "4")
                        {
                            nCFR.ScheduledScreens[0].Items[i][37].FieldValue = "5";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "5" || oCFR.ScheduledScreens[0].Items[i][43].FieldValue == "6")
                        {
                            nCFR.ScheduledScreens[0].Items[i][37].FieldValue = "6";
                        }
                        //43
                        int HyderantDistance = 0;
                        if (oCFR.ScheduledScreens[0].Items[i][54].FieldValue == "m" && oCFR.ScheduledScreens[0].Items[i][56].FieldValue != "")
                        {
                            HyderantDistance = Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][56].FieldValue.ToString());
                        }
                        else if (oCFR.ScheduledScreens[0].Items[i][54].FieldValue == "ft" && oCFR.ScheduledScreens[0].Items[i][56].FieldValue != "")
                        {
                            int HydFt = Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][56].FieldValue.ToString());
                            HyderantDistance = (int)Math.Round(HydFt * 0.30480370641307);
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][56].FieldValue == "")
                        {
                            nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "NH";
                        }
                        else
                        {
                            if (HyderantDistance >= 0 && HyderantDistance < 101)
                            {
                                nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "100";
                            }
                            else if (HyderantDistance > 100 && HyderantDistance < 301)
                            {
                                nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "300";
                            }
                            else
                            {
                                nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "300+";
                            }
                        }
                        //44
                        if (oCFR.ScheduledScreens[0].Items[i][53].FieldValue != "" && oCFR.ScheduledScreens[0].Items[i][55].FieldValue != "")
                        {
                            int FireHallDis = 0;
                            if (oCFR.ScheduledScreens[0].Items[i][53].FieldValue == "m" && oCFR.ScheduledScreens[0].Items[i][55].FieldValue != "")
                            {
                                FireHallDis = (int)Math.Round(Convert.ToDouble(oCFR.ScheduledScreens[0].Items[i][55].FieldValue.ToString()) / 1000);
                            }
                            else if (oCFR.ScheduledScreens[0].Items[i][53].FieldValue == "ft" && oCFR.ScheduledScreens[0].Items[i][55].FieldValue != "")
                            {
                                int FHFt = Convert.ToInt32(oCFR.ScheduledScreens[0].Items[i][55].FieldValue.ToString());
                                FireHallDis = (int)Math.Round((FHFt * 0.30480370641307) / 1000);
                            }
                            if (FireHallDis <= 2.5)
                            {
                                nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "-2.5";
                            }
                            else if (FireHallDis > 2.5 && FireHallDis <= 5)
                            {
                                nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "-5";
                            }
                            else if (FireHallDis > 5 && FireHallDis <= 8)
                            {
                                nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "-8";
                            }
                            else
                            {
                                nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "+8";
                            }
                        }
                    
                        //45
                        if (oCFR.ScheduledScreens[0].Items[i][61].FieldValue == "N")
                        {
                            nCFR.ScheduledScreens[0].Items[i][43].FieldValue = "N";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][61].FieldValue == "C")
                        {
                            nCFR.ScheduledScreens[0].Items[i][43].FieldValue = "M";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][61].FieldValue == "L")
                        {
                            nCFR.ScheduledScreens[0].Items[i][43].FieldValue = "L";
                        }
                        if (oCFR.ScheduledScreens[0].Items[i][61].FieldValue == "OT")
                        {
                            string RemarkVal = nCFR.ScheduledScreens[0].Items[i][42].FieldValue;
                            nCFR.ScheduledScreens[0].Items[i][42].FieldValue = RemarkVal + " -- Fire Alarm system type - other on long form application is not supported on BSCA2.0 application";
                        }

                    } // For statement ends here for adding a schedule item in COPE
                      //Console.WriteLine("COPE Done");
                    if (LongFormUpdateSwitch == 1)
                    {
                        EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check                        
                    }

                }
                catch (Exception e)
                {
                    string e3 = oPolId + " | Long Form - COPE failed | " + e;
                    ErrorString = ErrorString + e3 + System.Environment.NewLine;

                    Console.WriteLine(e3);
                }

                //Property
                try
                {

                
                    // Get number of locations
                    int PropLocationCount = oCFR.ScheduledScreens[2].Items.Count;
                    // Read for each property
                    for (int i = 0; i < PropLocationCount; i++)
                    {
                        // First insert a Location Item
                        string PSID = nCFR.ScheduledScreens[1].ScheduleID;
                        CBLServiceReference.FieldItems[] PFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, PSID);
                        nCFR.ScheduledScreens[1].Items.Insert(i, PFF[0]);
                        // Add fields for a given location
                        ////186
                        string OldPropNum = oCFR.ScheduledScreens[2].Items[i][21].FieldValue;
                        nCFR.ScheduledScreens[1].Items[i][254].FieldValue = OldPropNum;
                        //187
                        var locList = new List<(string, string)>();
                        int locListCount = oCFR.ScheduledScreens[0].Items.Count;
                        for (int j = 0; j < locListCount; j++)
                        {
                            locList.Add((oCFR.ScheduledScreens[0].Items[j][114].FieldValue, oCFR.ScheduledScreens[0].Items[j][112].FieldValue));
                        }
                        foreach (var locItem in locList)
                        {
                            if (locItem.Item1 == OldPropNum)
                            {
                                nCFR.ScheduledScreens[1].Items[i][253].FieldValue = locItem.Item2;
                            }
                        }
                        //1
                        if (oCFR.ScheduledScreens[2].Items[i][313].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][200].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][313].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][200].FieldValue = "Named Perils";
                        }
                        //2
                        if (oCFR.ScheduledScreens[2].Items[i][297].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][199].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][297].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][199].FieldValue = "Named Perils";
                        }
                        //3
                        if (oCFR.ScheduledScreens[2].Items[i][315].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][198].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][315].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][198].FieldValue = "Named Perils";
                        }
                        //4 - N/A
                        //5
                        if (oCFR.ScheduledScreens[2].Items[i][298].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][196].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][298].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][196].FieldValue = "Named Perils";
                        }
                        //6 - 9 - N/A
                        //10
                        if (oCFR.ScheduledScreens[2].Items[i][309].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][191].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][309].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][191].FieldValue = "Named Perils";
                        }
                        //11
                        if (oCFR.ScheduledScreens[2].Items[i][316].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][190].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][316].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][190].FieldValue = "Named Perils";
                        }
                        //12 - N/A
                        //13
                        if (oCFR.ScheduledScreens[2].Items[i][164].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][188].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][164].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][188].FieldValue = "Named Perils";
                        }
                        //14
                        if (oCFR.ScheduledScreens[2].Items[i][161].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][187].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][161].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][187].FieldValue = "Named Perils";
                        }
                        //15
                        if (oCFR.ScheduledScreens[2].Items[i][162].FieldValue == "BR")
                        {
                            nCFR.ScheduledScreens[1].Items[i][186].FieldValue = "Broad Form";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][162].FieldValue == "NP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][186].FieldValue = "Named Perils";
                        }
                        //16 - 21 - N/A
                        // testing feedback done
                        //22
                        if (oCFR.ScheduledScreens[2].Items[i][230].FieldValue == "ACV" || oCFR.ScheduledScreens[2].Items[i][230].FieldValue == "ACVP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][180].FieldValue = "ACV";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][230].FieldValue == "RC" || oCFR.ScheduledScreens[2].Items[i][230].FieldValue == "RCP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][180].FieldValue = "RC";
                        }
                        //23
                        if (oCFR.ScheduledScreens[2].Items[i][275].FieldValue == "ACV" || oCFR.ScheduledScreens[2].Items[i][275].FieldValue == "ACVP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][179].FieldValue = "ACV";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][275].FieldValue == "RC" || oCFR.ScheduledScreens[2].Items[i][275].FieldValue == "RCP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][179].FieldValue = "RC";
                        }
                        //24
                        if (oCFR.ScheduledScreens[2].Items[i][272].FieldValue == "ACV" || oCFR.ScheduledScreens[2].Items[i][272].FieldValue == "ACVP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][178].FieldValue = "ACV";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][272].FieldValue == "RC" || oCFR.ScheduledScreens[2].Items[i][272].FieldValue == "RCP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][178].FieldValue = "RC";
                        }
                        //25 - N/A
                        //26
                        if (oCFR.ScheduledScreens[2].Items[i][187].FieldValue == "ACV" || oCFR.ScheduledScreens[2].Items[i][187].FieldValue == "ACVP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][176].FieldValue = "ACV";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][187].FieldValue == "RC" || oCFR.ScheduledScreens[2].Items[i][187].FieldValue == "RCP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][176].FieldValue = "RC";
                        }
                        //27
                        if (oCFR.ScheduledScreens[2].Items[i][203].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][158].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][203].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][158].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][203].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][158].FieldValue = "80%";
                        }
                        //28
                        if (oCFR.ScheduledScreens[2].Items[i][233].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][157].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][233].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][157].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][233].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][157].FieldValue = "80%";
                        }
                        //29
                        if (oCFR.ScheduledScreens[2].Items[i][228].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][156].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][228].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][156].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][228].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][156].FieldValue = "80%";
                        }
                        //30 - N/A
                        //31
                        if (oCFR.ScheduledScreens[2].Items[i][270].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][154].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][270].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][154].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][270].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][154].FieldValue = "80%";
                        }
                        //32 - 35 - N/A
                        //36
                        if (oCFR.ScheduledScreens[2].Items[i][268].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][149].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][268].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][149].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][268].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][149].FieldValue = "80%";
                        }
                        //37
                        if (oCFR.ScheduledScreens[2].Items[i][255].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][148].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][255].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][148].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][255].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][148].FieldValue = "80%";
                        }
                        //38 - N/A
                        //39
                        if (oCFR.ScheduledScreens[2].Items[i][163].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][146].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][163].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][146].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][163].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][146].FieldValue = "80%";
                        }
                        //40
                        if (oCFR.ScheduledScreens[2].Items[i][158].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][145].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][158].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][145].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][158].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][145].FieldValue = "80%";
                        }
                        //41
                        if (oCFR.ScheduledScreens[2].Items[i][160].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][144].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][160].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][144].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][160].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][144].FieldValue = "80%";
                        }
                        //42 - 44C - N/A
                        //45
                        nCFR.ScheduledScreens[1].Items[i][137].FieldValue = oCFR.ScheduledScreens[2].Items[i][202].FieldValue;
                        //46
                        nCFR.ScheduledScreens[1].Items[i][136].FieldValue = oCFR.ScheduledScreens[2].Items[i][225].FieldValue;
                        //47
                        nCFR.ScheduledScreens[1].Items[i][135].FieldValue = oCFR.ScheduledScreens[2].Items[i][217].FieldValue;
                        //48 - N/A
                        //49
                        nCFR.ScheduledScreens[1].Items[i][133].FieldValue = oCFR.ScheduledScreens[2].Items[i][246].FieldValue;
                        //50 - N/A
                        //51
                        nCFR.ScheduledScreens[1].Items[i][131].FieldValue = oCFR.ScheduledScreens[2].Items[i][114].FieldValue;
                        //52
                        nCFR.ScheduledScreens[1].Items[i][130].FieldValue = oCFR.ScheduledScreens[2].Items[i][122].FieldValue;
                        //53 - N/A
                        //54
                        nCFR.ScheduledScreens[1].Items[i][129].FieldValue = oCFR.ScheduledScreens[2].Items[i][118].FieldValue;
                        //55
                        //56
                        nCFR.ScheduledScreens[1].Items[i][128].FieldValue = oCFR.ScheduledScreens[2].Items[i][244].FieldValue;
                        //57
                        nCFR.ScheduledScreens[1].Items[i][127].FieldValue = oCFR.ScheduledScreens[2].Items[i][212].FieldValue;
                        //58
                        //59
                        nCFR.ScheduledScreens[1].Items[i][125].FieldValue = oCFR.ScheduledScreens[2].Items[i][159].FieldValue;
                        //60
                        nCFR.ScheduledScreens[1].Items[i][124].FieldValue = oCFR.ScheduledScreens[2].Items[i][156].FieldValue;
                        //61
                        nCFR.ScheduledScreens[1].Items[i][123].FieldValue = oCFR.ScheduledScreens[2].Items[i][157].FieldValue;
                        //62
                        nCFR.ScheduledScreens[1].Items[i][122].FieldValue = oCFR.ScheduledScreens[2].Items[i][90].FieldValue;
                        //63
                        nCFR.ScheduledScreens[1].Items[i][121].FieldValue = oCFR.ScheduledScreens[2].Items[i][108].FieldValue;
                        //64
                        nCFR.ScheduledScreens[1].Items[i][120].FieldValue = oCFR.ScheduledScreens[2].Items[i][106].FieldValue;
                        //64A - 64C - N/A
                        //65
                        nCFR.ScheduledScreens[1].Items[i][116].FieldValue = oCFR.ScheduledScreens[2].Items[i][236].FieldValue;
                        //66
                        nCFR.ScheduledScreens[1].Items[i][115].FieldValue = oCFR.ScheduledScreens[2].Items[i][276].FieldValue;
                        //67
                        nCFR.ScheduledScreens[1].Items[i][114].FieldValue = oCFR.ScheduledScreens[2].Items[i][282].FieldValue;
                        //68 - N/A
                        //69
                        nCFR.ScheduledScreens[1].Items[i][112].FieldValue = oCFR.ScheduledScreens[2].Items[i][245].FieldValue;
                        //70 - N/A
                        //71
                        nCFR.ScheduledScreens[1].Items[i][110].FieldValue = oCFR.ScheduledScreens[2].Items[i][113].FieldValue;
                        //72
                        nCFR.ScheduledScreens[1].Items[i][109].FieldValue = oCFR.ScheduledScreens[2].Items[i][121].FieldValue;
                        //73
                        nCFR.ScheduledScreens[1].Items[i][108].FieldValue = oCFR.ScheduledScreens[2].Items[i][117].FieldValue;
                        //74
                        nCFR.ScheduledScreens[1].Items[i][107].FieldValue = oCFR.ScheduledScreens[2].Items[i][242].FieldValue;
                        //75
                        nCFR.ScheduledScreens[1].Items[i][106].FieldValue = oCFR.ScheduledScreens[2].Items[i][211].FieldValue;
                        //76 - N/A
                        //77
                        nCFR.ScheduledScreens[1].Items[i][104].FieldValue = oCFR.ScheduledScreens[2].Items[i][155].FieldValue;
                        //78
                        nCFR.ScheduledScreens[1].Items[i][103].FieldValue = oCFR.ScheduledScreens[2].Items[i][151].FieldValue;
                        //79
                        nCFR.ScheduledScreens[1].Items[i][102].FieldValue = oCFR.ScheduledScreens[2].Items[i][154].FieldValue;
                        //80
                        nCFR.ScheduledScreens[1].Items[i][101].FieldValue = oCFR.ScheduledScreens[2].Items[i][89].FieldValue;
                        //81
                        nCFR.ScheduledScreens[1].Items[i][100].FieldValue = oCFR.ScheduledScreens[2].Items[i][103].FieldValue;
                        //82
                        nCFR.ScheduledScreens[1].Items[i][99].FieldValue = oCFR.ScheduledScreens[2].Items[i][101].FieldValue;
                        //82A - 82C - N/A
                        //83
                        nCFR.ScheduledScreens[1].Items[i][72].FieldValue = oCFR.ScheduledScreens[2].Items[i][261].FieldValue;
                        //84
                        nCFR.ScheduledScreens[1].Items[i][70].FieldValue = oCFR.ScheduledScreens[2].Items[i][224].FieldValue;
                        //85
                        nCFR.ScheduledScreens[1].Items[i][68].FieldValue = oCFR.ScheduledScreens[2].Items[i][216].FieldValue;
                        //86 - N/A
                        //87 
                        nCFR.ScheduledScreens[1].Items[i][43].FieldValue = oCFR.ScheduledScreens[2].Items[i][205].FieldValue;
                        //88 - N/A
                        //89
                        nCFR.ScheduledScreens[1].Items[i][62].FieldValue = oCFR.ScheduledScreens[2].Items[i][111].FieldValue;
                        //90
                        nCFR.ScheduledScreens[1].Items[i][60].FieldValue = oCFR.ScheduledScreens[2].Items[i][119].FieldValue;
                        //91
                        nCFR.ScheduledScreens[1].Items[i][58].FieldValue = oCFR.ScheduledScreens[2].Items[i][115].FieldValue;
                        //92
                        nCFR.ScheduledScreens[1].Items[i][56].FieldValue = oCFR.ScheduledScreens[2].Items[i][204].FieldValue;
                        //93
                        nCFR.ScheduledScreens[1].Items[i][54].FieldValue = oCFR.ScheduledScreens[2].Items[i][199].FieldValue;
                        //94 - N/A
                        //95
                        nCFR.ScheduledScreens[1].Items[i][52].FieldValue = oCFR.ScheduledScreens[2].Items[i][149].FieldValue;
                        //96
                        nCFR.ScheduledScreens[1].Items[i][51].FieldValue = oCFR.ScheduledScreens[2].Items[i][147].FieldValue;
                        //97
                        nCFR.ScheduledScreens[1].Items[i][50].FieldValue = oCFR.ScheduledScreens[2].Items[i][148].FieldValue;
                        //98
                        nCFR.ScheduledScreens[1].Items[i][49].FieldValue = oCFR.ScheduledScreens[2].Items[i][87].FieldValue;
                        //99
                        nCFR.ScheduledScreens[1].Items[i][48].FieldValue = oCFR.ScheduledScreens[2].Items[i][93].FieldValue;
                        //100
                        nCFR.ScheduledScreens[1].Items[i][47].FieldValue = oCFR.ScheduledScreens[2].Items[i][91].FieldValue;
                        //100A - 102 - N/A
                        //103
                        if (oCFR.ScheduledScreens[2].Items[i][345].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][252].FieldValue = "GE";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][359].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][252].FieldValue = "P";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][377].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][252].FieldValue = "ALS";
                        }
                        //104
                        if (oCFR.ScheduledScreens[2].Items[i][348].FieldValue == "12" || oCFR.ScheduledScreens[2].Items[i][363].FieldValue == "12" || oCFR.ScheduledScreens[2].Items[i][372].FieldValue == "12")
                        {
                            nCFR.ScheduledScreens[1].Items[i][251].FieldValue = "12 Months";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][348].FieldValue == "18" || oCFR.ScheduledScreens[2].Items[i][363].FieldValue == "18" || oCFR.ScheduledScreens[2].Items[i][372].FieldValue == "18")
                        {
                            nCFR.ScheduledScreens[1].Items[i][251].FieldValue = "18 Months";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][348].FieldValue == "24" || oCFR.ScheduledScreens[2].Items[i][363].FieldValue == "24" || oCFR.ScheduledScreens[2].Items[i][372].FieldValue == "24")
                        {
                            nCFR.ScheduledScreens[1].Items[i][251].FieldValue = "24 Months";
                        }
                        //105 - N/A
                        //106
                        if (oCFR.ScheduledScreens[2].Items[i][375].FieldValue == "100" || oCFR.ScheduledScreens[2].Items[i][342].FieldValue == "100" || oCFR.ScheduledScreens[2].Items[i][362].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][241].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][375].FieldValue == "90" || oCFR.ScheduledScreens[2].Items[i][342].FieldValue == "90" || oCFR.ScheduledScreens[2].Items[i][362].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][241].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][375].FieldValue == "80" || oCFR.ScheduledScreens[2].Items[i][342].FieldValue == "80" || oCFR.ScheduledScreens[2].Items[i][362].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][241].FieldValue = "80%";
                        }
                        //107
                        if (oCFR.ScheduledScreens[2].Items[i][345].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][249].FieldValue = oCFR.ScheduledScreens[2].Items[i][345].FieldValue;
                        }
                        else if (oCFR.ScheduledScreens[2].Items[i][359].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][249].FieldValue = oCFR.ScheduledScreens[2].Items[i][359].FieldValue;
                        }
                        else if (oCFR.ScheduledScreens[2].Items[i][377].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][249].FieldValue = oCFR.ScheduledScreens[2].Items[i][377].FieldValue;
                        }
                        //108
                        if (oCFR.ScheduledScreens[2].Items[i][343].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][239].FieldValue = oCFR.ScheduledScreens[2].Items[i][343].FieldValue;
                        }
                        else if (oCFR.ScheduledScreens[2].Items[i][358].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][239].FieldValue = oCFR.ScheduledScreens[2].Items[i][358].FieldValue;
                        }
                        else if (oCFR.ScheduledScreens[2].Items[i][370].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][239].FieldValue = oCFR.ScheduledScreens[2].Items[i][370].FieldValue;
                        }
                        //109, 110 - N/A
                        //111
                        if (oCFR.ScheduledScreens[2].Items[i][341].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][234].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][341].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][234].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][341].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][234].FieldValue = "80%";
                        }
                        //112
                        nCFR.ScheduledScreens[1].Items[i][226].FieldValue = oCFR.ScheduledScreens[2].Items[i][339].FieldValue;
                        //113
                        nCFR.ScheduledScreens[1].Items[i][224].FieldValue = oCFR.ScheduledScreens[2].Items[i][337].FieldValue;
                        //114, 115 - N/A
                        //116
                        if (oCFR.ScheduledScreens[2].Items[i][357].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][246].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][357].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][246].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][357].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][246].FieldValue = "80%";
                        }
                        //117
                        nCFR.ScheduledScreens[1].Items[i][217].FieldValue = oCFR.ScheduledScreens[2].Items[i][332].FieldValue;
                        //118
                        nCFR.ScheduledScreens[1].Items[i][250].FieldValue = oCFR.ScheduledScreens[2].Items[i][331].FieldValue;
                        //119
                        if (oCFR.ScheduledScreens[2].Items[i][339].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][232].FieldValue = "PR";
                        }
                        //120
                        if (oCFR.ScheduledScreens[2].Items[i][332].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][236].FieldValue = "EE";
                        }
                        //121
                        if (oCFR.ScheduledScreens[2].Items[i][335].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][222].FieldValue = "Rental Income";
                        }
                        //122, 123 - N/A
                        //124
                        if (oCFR.ScheduledScreens[2].Items[i][379].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][218].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][379].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][218].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][379].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][218].FieldValue = "80%";
                        }
                        //125
                        nCFR.ScheduledScreens[1].Items[i][248].FieldValue = oCFR.ScheduledScreens[2].Items[i][335].FieldValue;
                        //126
                        nCFR.ScheduledScreens[1].Items[i][245].FieldValue = oCFR.ScheduledScreens[2].Items[i][334].FieldValue;
                        //127
                        // testing feedback done
                        nCFR.ScheduledScreens[1].Items[i][231].FieldValue = oCFR.ScheduledScreens[2].Items[i][350].FieldValue;
                        //128, 129 - N/A
                        //130
                        if (oCFR.ScheduledScreens[2].Items[i][374].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][230].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][374].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][230].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][374].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][230].FieldValue = "80%";
                        }
                        //131
                        nCFR.ScheduledScreens[1].Items[i][233].FieldValue = oCFR.ScheduledScreens[2].Items[i][321].FieldValue;
                        //132
                        nCFR.ScheduledScreens[1].Items[i][229].FieldValue = oCFR.ScheduledScreens[2].Items[i][318].FieldValue;
                        //133
                        // testing feedback done
                        nCFR.ScheduledScreens[1].Items[i][215].FieldValue = oCFR.ScheduledScreens[2].Items[i][364].FieldValue;
                        //134, 135 - N/A
                        //136
                        if (oCFR.ScheduledScreens[2].Items[i][324].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][207].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][324].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][207].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][324].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][207].FieldValue = "80%";
                        }
                        //137
                        nCFR.ScheduledScreens[1].Items[i][205].FieldValue = oCFR.ScheduledScreens[2].Items[i][352].FieldValue;
                        //138
                        nCFR.ScheduledScreens[1].Items[i][203].FieldValue = oCFR.ScheduledScreens[2].Items[i][317].FieldValue;
                        //139
                        // testing feedback done
                        nCFR.ScheduledScreens[1].Items[i][214].FieldValue = oCFR.ScheduledScreens[2].Items[i][349].FieldValue;
                        //140, 141 - N/A
                        //142
                        if (oCFR.ScheduledScreens[2].Items[i][353].FieldValue == "100")
                        {
                            nCFR.ScheduledScreens[1].Items[i][206].FieldValue = "100%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][353].FieldValue == "90")
                        {
                            nCFR.ScheduledScreens[1].Items[i][206].FieldValue = "90%";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][353].FieldValue == "80")
                        {
                            nCFR.ScheduledScreens[1].Items[i][206].FieldValue = "80%";
                        }
                        //143
                        nCFR.ScheduledScreens[1].Items[i][204].FieldValue = oCFR.ScheduledScreens[2].Items[i][320].FieldValue;
                        //144
                        nCFR.ScheduledScreens[1].Items[i][202].FieldValue = oCFR.ScheduledScreens[2].Items[i][373].FieldValue;
                        //145-146 - N/A
                        //147
                        if (oCFR.ScheduledScreens[2].Items[i][86].FieldValue == "Opt1")
                        {
                            nCFR.ScheduledScreens[1].Items[i][38].FieldValue = "BM1";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][86].FieldValue == "Opt2")
                        {
                            nCFR.ScheduledScreens[1].Items[i][38].FieldValue = "BM2";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][86].FieldValue == "Opt3")
                        {
                            nCFR.ScheduledScreens[1].Items[i][38].FieldValue = "BM3";
                        }
                        //149 - N/A
                        //150
                        nCFR.ScheduledScreens[1].Items[i][33].FieldValue = oCFR.ScheduledScreens[2].Items[i][82].FieldValue;
                        //151
                        nCFR.ScheduledScreens[1].Items[i][25].FieldValue = oCFR.ScheduledScreens[2].Items[i][78].FieldValue;
                        //152
                        nCFR.ScheduledScreens[1].Items[i][37].FieldValue = oCFR.ScheduledScreens[2].Items[i][70].FieldValue;
                        //153, 155, 156, 157, 158, 159, 161, 162, 163, 164, 165, 167, 168, 169, 170  - N/A
                        //171
                        nCFR.ScheduledScreens[1].Items[i][13].FieldValue = oCFR.ScheduledScreens[2].Items[i][85].FieldValue;
                        //173 - N/A
                        //174
                        nCFR.ScheduledScreens[1].Items[i][24].FieldValue = oCFR.ScheduledScreens[2].Items[i][81].FieldValue;
                        //175
                        nCFR.ScheduledScreens[1].Items[i][32].FieldValue = oCFR.ScheduledScreens[2].Items[i][77].FieldValue;
                        //176
                        nCFR.ScheduledScreens[1].Items[i][7].FieldValue = oCFR.ScheduledScreens[2].Items[i][69].FieldValue;
                        //177
                        nCFR.ScheduledScreens[1].Items[i][19].FieldValue = oCFR.ScheduledScreens[2].Items[i][83].FieldValue;
                        //179 - N/A
                        //180
                        nCFR.ScheduledScreens[1].Items[i][9].FieldValue = oCFR.ScheduledScreens[2].Items[i][79].FieldValue;
                        //181
                        nCFR.ScheduledScreens[1].Items[i][8].FieldValue = oCFR.ScheduledScreens[2].Items[i][75].FieldValue;
                        //182
                        nCFR.ScheduledScreens[1].Items[i][23].FieldValue = oCFR.ScheduledScreens[2].Items[i][67].FieldValue;
                        //183-185
                        //188
                        if (oCFR.ScheduledScreens[2].Items[i][242].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][95].FieldValue = "TLI";
                        }
                        //189
                        if (oCFR.ScheduledScreens[2].Items[i][211].FieldValue != "")
                        {
                            nCFR.ScheduledScreens[1].Items[i][94].FieldValue = "CG";
                        }
                        //190 - N/A
                        //191
                        nCFR.ScheduledScreens[1].Items[i][41].FieldValue = oCFR.ScheduledScreens[2].Items[i][256].FieldValue;
                        //192
                        nCFR.ScheduledScreens[1].Items[i][92].FieldValue = oCFR.ScheduledScreens[2].Items[i][145].FieldValue;
                        //193
                        nCFR.ScheduledScreens[1].Items[i][91].FieldValue = oCFR.ScheduledScreens[2].Items[i][144].FieldValue;
                        //194
                        nCFR.ScheduledScreens[1].Items[i][90].FieldValue = oCFR.ScheduledScreens[2].Items[i][136].FieldValue;
                        //195
                        nCFR.ScheduledScreens[1].Items[i][89].FieldValue = oCFR.ScheduledScreens[2].Items[i][135].FieldValue;
                        //196
                        nCFR.ScheduledScreens[1].Items[i][88].FieldValue = oCFR.ScheduledScreens[2].Items[i][134].FieldValue;
                        //197 - 200 - N/A
                        //201
                        if (oCFR.ScheduledScreens[2].Items[i][186].FieldValue == "ACV" || oCFR.ScheduledScreens[2].Items[i][186].FieldValue == "ACVP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][171].FieldValue = "ACV";
                        }
                        if (oCFR.ScheduledScreens[2].Items[i][186].FieldValue == "RC" || oCFR.ScheduledScreens[2].Items[i][186].FieldValue == "RCP")
                        {
                            nCFR.ScheduledScreens[1].Items[i][171].FieldValue = "RC";
                        }
                        //202 - 212 - N/A
                    }
                    //Console.WriteLine("Property Done");
                    if (LongFormUpdateSwitch == 1)
                    {
                        EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check                        
                    }
                }
                catch (Exception e)
                {
                    string e4 = oPolId + " | Long Form - Property failed | " + e;
                    ErrorString = ErrorString + e4 + System.Environment.NewLine;
                    Console.WriteLine(e4);
                }

                //Misc. Coverage
                try
                {


                    // For parts from Property Tab
                    // First read all the schedules from the source form = PropLocationCountMC
                    int PropLocationCountMC = oCFR.ScheduledScreens[2].Items.Count;


                    // Reading ED
                    int MicCovCounter = 0;
                    string EDDeductable = "";
                    string EDAmtInsurance = "";
                    string EDPremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        //Console.WriteLine(i);
                        if (oCFR.ScheduledScreens[2].Items[i][49].FieldValue != "")
                        {
                            EDDeductable = oCFR.ScheduledScreens[2].Items[i][58].FieldValue;
                            EDAmtInsurance = oCFR.ScheduledScreens[2].Items[i][49].FieldValue;
                            EDPremium = oCFR.ScheduledScreens[2].Items[i][29].FieldValue;
                            MicCovCounter++;
                            break;
                        }
                    }
                    if (EDAmtInsurance != "")
                    {
                        (int, int, int, int) EDTuple = MiscCovField(MicCovCounter);
                        nCFR.NonScheduledScreens[1].Fields[EDTuple.Item1].FieldValue = "ED";
                        nCFR.NonScheduledScreens[1].Fields[EDTuple.Item2].FieldValue = EDDeductable;
                        nCFR.NonScheduledScreens[1].Fields[EDTuple.Item3].FieldValue = EDAmtInsurance;
                        nCFR.NonScheduledScreens[1].Fields[EDTuple.Item4].FieldValue = EDPremium;
                    }

                    //Reading MOC
                    string MOCDeductable = "";
                    string MOCAmtInsurance = "";
                    string MOCPremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][51].FieldValue != "")
                        {
                            MOCDeductable = oCFR.ScheduledScreens[2].Items[i][60].FieldValue;
                            MOCAmtInsurance = oCFR.ScheduledScreens[2].Items[i][51].FieldValue;
                            MOCPremium = oCFR.ScheduledScreens[2].Items[i][31].FieldValue;
                            MicCovCounter++;
                            break;
                        }
                    }
                    if (MOCAmtInsurance != "")
                    {
                        (int, int, int, int) MOCTuple = MiscCovField(MicCovCounter);
                        nCFR.NonScheduledScreens[1].Fields[MOCTuple.Item1].FieldValue = "MOC";
                        nCFR.NonScheduledScreens[1].Fields[MOCTuple.Item2].FieldValue = MOCDeductable;
                        nCFR.NonScheduledScreens[1].Fields[MOCTuple.Item3].FieldValue = MOCAmtInsurance;
                        nCFR.NonScheduledScreens[1].Fields[MOCTuple.Item4].FieldValue = MOCPremium;
                    }

                    //Reading DFC
                    string DFCDeductable = "";
                    string DFCAmtInsurance = "";
                    string DFCPremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][50].FieldValue != "")
                        {
                            DFCDeductable = oCFR.ScheduledScreens[2].Items[i][59].FieldValue;
                            DFCAmtInsurance = oCFR.ScheduledScreens[2].Items[i][50].FieldValue;
                            DFCPremium = oCFR.ScheduledScreens[2].Items[i][30].FieldValue;
                            MicCovCounter++;
                            break;
                        }
                    }
                    if (DFCAmtInsurance != "")
                    {
                        (int, int, int, int) DFCTuple = MiscCovField(MicCovCounter);
                        nCFR.NonScheduledScreens[1].Fields[DFCTuple.Item1].FieldValue = "DFC";
                        nCFR.NonScheduledScreens[1].Fields[DFCTuple.Item2].FieldValue = DFCDeductable;
                        nCFR.NonScheduledScreens[1].Fields[DFCTuple.Item3].FieldValue = DFCAmtInsurance;
                        nCFR.NonScheduledScreens[1].Fields[DFCTuple.Item4].FieldValue = DFCPremium;
                    }

                    //Reading LI and LO
                    string LILODeductable = "";
                    string LILOAmtInsurance = "";
                    string LILOPremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][23].FieldValue != "")
                        {
                            LILODeductable = oCFR.ScheduledScreens[2].Items[i][62].FieldValue;
                            LILOAmtInsurance = oCFR.ScheduledScreens[2].Items[i][23].FieldValue;
                            LILOPremium = oCFR.ScheduledScreens[2].Items[i][42].FieldValue;
                            MicCovCounter++;
                            break;
                        }
                    }
                    if (LILOAmtInsurance != "")
                    {
                        (int, int, int, int) LITuple = MiscCovField(MicCovCounter);
                        nCFR.NonScheduledScreens[1].Fields[LITuple.Item1].FieldValue = "LI";
                        nCFR.NonScheduledScreens[1].Fields[LITuple.Item2].FieldValue = LILODeductable;
                        nCFR.NonScheduledScreens[1].Fields[LITuple.Item3].FieldValue = LILOAmtInsurance;
                        nCFR.NonScheduledScreens[1].Fields[LITuple.Item4].FieldValue = LILOPremium;
                        MicCovCounter++;
                        (int, int, int, int) LOTuple = MiscCovField(MicCovCounter);
                        nCFR.NonScheduledScreens[1].Fields[LOTuple.Item1].FieldValue = "LO";
                        nCFR.NonScheduledScreens[1].Fields[LOTuple.Item2].FieldValue = LILODeductable;
                        nCFR.NonScheduledScreens[1].Fields[LOTuple.Item3].FieldValue = LILOAmtInsurance;
                        nCFR.NonScheduledScreens[1].Fields[LOTuple.Item4].FieldValue = LILOPremium;
                    }


                    // Reading Free Form Crime Field
                    int FFCCCounter = 0;
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][46].FieldValue != "") { FFCCCounter++; }
                        if (oCFR.ScheduledScreens[2].Items[i][44].FieldValue != "") { FFCCCounter++; }
                        if (oCFR.ScheduledScreens[2].Items[i][45].FieldValue != "") { FFCCCounter++; }
                    }

                    //Console.WriteLine("Free Form Crime Field Counter: "+ FFCCCounter);

                    if (FFCCCounter > 0)
                    {
                        if (FFCCCounter < 4)
                        {
                            int FFCovCounter = 0;
                            for (int i = 0; i < PropLocationCountMC; i++)
                            {
                                // First Free Form
                                if (oCFR.ScheduledScreens[2].Items[i][46].FieldValue != "")
                                {
                                    FFCovCounter++;
                                    (int, int, int, int) FFTuple = FreeFormField(FFCovCounter);
                                    nCFR.NonScheduledScreens[1].Fields[FFTuple.Item1].FieldValue = oCFR.ScheduledScreens[2].Items[i][66].FieldValue; //name
                                    nCFR.NonScheduledScreens[1].Fields[FFTuple.Item2].FieldValue = oCFR.ScheduledScreens[2].Items[i][55].FieldValue; //ded
                                    nCFR.NonScheduledScreens[1].Fields[FFTuple.Item3].FieldValue = oCFR.ScheduledScreens[2].Items[i][46].FieldValue; //AmtInsurance
                                    nCFR.NonScheduledScreens[1].Fields[FFTuple.Item4].FieldValue = oCFR.ScheduledScreens[2].Items[i][26].FieldValue; //Premium
                                }
                                if (FFCovCounter == 3) { break; }
                                // Second Free Form
                                if (oCFR.ScheduledScreens[2].Items[i][44].FieldValue != "")
                                {
                                    FFCovCounter++;
                                    (int, int, int, int) FFFTuple = FreeFormField(FFCovCounter);
                                    nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item1].FieldValue = oCFR.ScheduledScreens[2].Items[i][64].FieldValue; //name
                                    nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item2].FieldValue = oCFR.ScheduledScreens[2].Items[i][53].FieldValue; //ded
                                    nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item3].FieldValue = oCFR.ScheduledScreens[2].Items[i][44].FieldValue; //AmtInsurance
                                    nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item4].FieldValue = oCFR.ScheduledScreens[2].Items[i][24].FieldValue; //Premium
                                }
                                if (FFCovCounter == 3) { break; }
                                // Third Free Form
                                if (oCFR.ScheduledScreens[2].Items[i][45].FieldValue != "")
                                {
                                    FFCovCounter++;
                                    (int, int, int, int) FFFFTuple = FreeFormField(FFCovCounter);
                                    nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item1].FieldValue = oCFR.ScheduledScreens[2].Items[i][65].FieldValue; //name
                                    nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item2].FieldValue = oCFR.ScheduledScreens[2].Items[i][54].FieldValue; //ded
                                    nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item3].FieldValue = oCFR.ScheduledScreens[2].Items[i][45].FieldValue; //AmtInsurance
                                    nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item4].FieldValue = oCFR.ScheduledScreens[2].Items[i][25].FieldValue; //Premium
                                }
                                if (FFCovCounter == 3) { break; }
                            }
                        }
                        else
                        {
                            int FFCovCounter = 0;
                            do
                            {
                                for (int i = 0; i < PropLocationCountMC; i++)
                                {
                                    // First Free Form
                                    if (oCFR.ScheduledScreens[2].Items[i][46].FieldValue != "")
                                    {
                                        FFCovCounter++;
                                        (int, int, int, int) FFTuple = FreeFormField(FFCovCounter);
                                        nCFR.NonScheduledScreens[1].Fields[FFTuple.Item1].FieldValue = oCFR.ScheduledScreens[2].Items[i][66].FieldValue; //name
                                        nCFR.NonScheduledScreens[1].Fields[FFTuple.Item2].FieldValue = oCFR.ScheduledScreens[2].Items[i][55].FieldValue; //ded
                                        nCFR.NonScheduledScreens[1].Fields[FFTuple.Item3].FieldValue = oCFR.ScheduledScreens[2].Items[i][46].FieldValue; //AmtInsurance
                                        nCFR.NonScheduledScreens[1].Fields[FFTuple.Item4].FieldValue = oCFR.ScheduledScreens[2].Items[i][26].FieldValue; //Premium
                                    }
                                    if (FFCovCounter == 3) { break; }
                                    // Second Free Form
                                    if (oCFR.ScheduledScreens[2].Items[i][44].FieldValue != "")
                                    {
                                        FFCovCounter++;
                                        (int, int, int, int) FFFTuple = FreeFormField(FFCovCounter);
                                        nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item1].FieldValue = oCFR.ScheduledScreens[2].Items[i][64].FieldValue; //name
                                        nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item2].FieldValue = oCFR.ScheduledScreens[2].Items[i][53].FieldValue; //ded
                                        nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item3].FieldValue = oCFR.ScheduledScreens[2].Items[i][44].FieldValue; //AmtInsurance
                                        nCFR.NonScheduledScreens[1].Fields[FFFTuple.Item4].FieldValue = oCFR.ScheduledScreens[2].Items[i][24].FieldValue; //Premium
                                    }
                                    if (FFCovCounter == 3) { break; }
                                    // Third Free Form
                                    if (oCFR.ScheduledScreens[2].Items[i][45].FieldValue != "")
                                    {
                                        FFCovCounter++;
                                        (int, int, int, int) FFFFTuple = FreeFormField(FFCovCounter);
                                        nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item1].FieldValue = oCFR.ScheduledScreens[2].Items[i][65].FieldValue; //name
                                        nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item2].FieldValue = oCFR.ScheduledScreens[2].Items[i][54].FieldValue; //ded
                                        nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item3].FieldValue = oCFR.ScheduledScreens[2].Items[i][45].FieldValue; //AmtInsurance
                                        nCFR.NonScheduledScreens[1].Fields[FFFFTuple.Item4].FieldValue = oCFR.ScheduledScreens[2].Items[i][25].FieldValue; //Premium
                                    }
                                    if (FFCovCounter == 3) { break; }
                                }
                            } while (FFCovCounter < 3);
                        }
                    }

                

                    // Reading CEF
                    int CECounter = 0;
                    string CECoverage = "";
                    string CEDeductable = "";
                    string CECoins = "";
                    string CELimit = "";
                    string CEpremium = "";


                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][183].FieldValue != "")
                        {
                            CECoverage = oCFR.ScheduledScreens[2].Items[i][295].FieldValue;
                            CEDeductable = oCFR.ScheduledScreens[2].Items[i][184].FieldValue;
                            CECoins = oCFR.ScheduledScreens[2].Items[i][185].FieldValue;
                            CELimit = oCFR.ScheduledScreens[2].Items[i][183].FieldValue;
                            CEpremium = oCFR.ScheduledScreens[2].Items[i][192].FieldValue;
                            CECounter++;
                            break;
                        }
                    }
                    if (CELimit != "")
                    {
                        (int, int, int, int, int, int) CETuple = ContEqupField(CECounter);
                        nCFR.NonScheduledScreens[1].Fields[CETuple.Item1].FieldValue = "CEF";
                        if (CECoverage == "BR")
                        {
                            nCFR.NonScheduledScreens[1].Fields[CETuple.Item2].FieldValue = "Broad Form";
                        }
                        else
                        {
                            nCFR.NonScheduledScreens[1].Fields[CETuple.Item2].FieldValue = "Named Perils";
                        }
                        nCFR.NonScheduledScreens[1].Fields[CETuple.Item3].FieldValue = CEDeductable;
                        if (CECoins == "80")
                        {
                            nCFR.NonScheduledScreens[1].Fields[CETuple.Item4].FieldValue = "80%";
                        }
                        if (CECoins == "90")
                        {
                            nCFR.NonScheduledScreens[1].Fields[CETuple.Item4].FieldValue = "90%";
                        }
                        if (CECoins == "100")
                        {
                            nCFR.NonScheduledScreens[1].Fields[CETuple.Item4].FieldValue = "100%";
                        }
                        nCFR.NonScheduledScreens[1].Fields[CETuple.Item5].FieldValue = CELimit;
                        nCFR.NonScheduledScreens[1].Fields[CETuple.Item6].FieldValue = CEpremium;
                    }

                    // Reading EF
                    string EFCoverage = "";
                    string EFDeductable = "";
                    string EFCoins = "";
                    string EFLimit = "";
                    string EFpremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][257].FieldValue != "")
                        {
                            EFCoverage = oCFR.ScheduledScreens[2].Items[i][305].FieldValue;
                            EFDeductable = oCFR.ScheduledScreens[2].Items[i][283].FieldValue;
                            EFCoins = oCFR.ScheduledScreens[2].Items[i][248].FieldValue;
                            EFLimit = oCFR.ScheduledScreens[2].Items[i][257].FieldValue;
                            EFpremium = oCFR.ScheduledScreens[2].Items[i][221].FieldValue;
                            CECounter++;
                            break;
                        }
                    }
                    if (EFLimit != "")
                    {
                        (int, int, int, int, int, int) EFTuple = ContEqupField(CECounter);
                        nCFR.NonScheduledScreens[1].Fields[EFTuple.Item1].FieldValue = "EF";
                        if (EFCoverage == "BR")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EFTuple.Item2].FieldValue = "Broad Form";
                        }
                        else
                        {
                            nCFR.NonScheduledScreens[1].Fields[EFTuple.Item2].FieldValue = "Named Perils";
                        }
                        nCFR.NonScheduledScreens[1].Fields[EFTuple.Item3].FieldValue = EFDeductable;
                        if (EFCoins == "80")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EFTuple.Item4].FieldValue = "80%";
                        }
                        if (EFCoins == "90")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EFTuple.Item4].FieldValue = "90%";
                        }
                        if (EFCoins == "100")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EFTuple.Item4].FieldValue = "100%";
                        }
                        nCFR.NonScheduledScreens[1].Fields[EFTuple.Item5].FieldValue = EFLimit;
                        nCFR.NonScheduledScreens[1].Fields[EFTuple.Item6].FieldValue = EFpremium;
                    }


                    // Reading IF
                    string IFCoverage = "";
                    string IFDeductable = "";
                    string IFCoins = "";
                    string IFLimit = "";
                    string IFpremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][266].FieldValue != "")
                        {
                            IFCoverage = oCFR.ScheduledScreens[2].Items[i][296].FieldValue;
                            IFDeductable = oCFR.ScheduledScreens[2].Items[i][179].FieldValue;
                            IFCoins = oCFR.ScheduledScreens[2].Items[i][253].FieldValue;
                            IFLimit = oCFR.ScheduledScreens[2].Items[i][266].FieldValue;
                            IFpremium = oCFR.ScheduledScreens[2].Items[i][175].FieldValue;
                            CECounter++;
                            break;
                        }
                    }
                    if (IFLimit != "")
                    {
                        (int, int, int, int, int, int) IFTuple = ContEqupField(CECounter);
                        nCFR.NonScheduledScreens[1].Fields[IFTuple.Item1].FieldValue = "IF";
                        if (IFCoverage == "BR")
                        {
                            nCFR.NonScheduledScreens[1].Fields[IFTuple.Item2].FieldValue = "Broad Form";
                        }
                        else
                        {
                            nCFR.NonScheduledScreens[1].Fields[IFTuple.Item2].FieldValue = "Named Perils";
                        }
                        nCFR.NonScheduledScreens[1].Fields[IFTuple.Item3].FieldValue = IFDeductable;
                        if (IFCoins == "80")
                        {
                            nCFR.NonScheduledScreens[1].Fields[IFTuple.Item4].FieldValue = "80%";
                        }
                        if (IFCoins == "90")
                        {
                            nCFR.NonScheduledScreens[1].Fields[IFTuple.Item4].FieldValue = "90%";
                        }
                        if (IFCoins == "100")
                        {
                            nCFR.NonScheduledScreens[1].Fields[IFTuple.Item4].FieldValue = "100%";
                        }
                        nCFR.NonScheduledScreens[1].Fields[IFTuple.Item5].FieldValue = IFLimit;
                        nCFR.NonScheduledScreens[1].Fields[IFTuple.Item6].FieldValue = IFpremium;
                    }


                    // Reading EXF
                    string EXFCoverage = "";
                    string EXFDeductable = "";
                    string EXFCoins = "";
                    string EXFLimit = "";
                    string EXFpremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][258].FieldValue != "")
                        {
                            EXFCoverage = oCFR.ScheduledScreens[2].Items[i][311].FieldValue;
                            EXFDeductable = oCFR.ScheduledScreens[2].Items[i][198].FieldValue;
                            EXFCoins = oCFR.ScheduledScreens[2].Items[i][227].FieldValue;
                            EXFLimit = oCFR.ScheduledScreens[2].Items[i][258].FieldValue;
                            EXFpremium = oCFR.ScheduledScreens[2].Items[i][229].FieldValue;
                            CECounter++;
                            break;
                        }
                    }
                    if (EXFLimit != "")
                    {
                        (int, int, int, int, int, int) EXFTuple = ContEqupField(CECounter);
                        nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item1].FieldValue = "EXF";
                        if (EXFCoverage == "BR")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item2].FieldValue = "Broad Form";
                        }
                        else
                        {
                            nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item2].FieldValue = "Named Perils";
                        }
                        nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item3].FieldValue = EXFDeductable;
                        if (EXFCoins == "80")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item4].FieldValue = "80%";
                        }
                        if (EXFCoins == "90")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item4].FieldValue = "90%";
                        }
                        if (EXFCoins == "100")
                        {
                            nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item4].FieldValue = "100%";
                        }
                        nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item5].FieldValue = EXFLimit;
                        nCFR.NonScheduledScreens[1].Fields[EXFTuple.Item6].FieldValue = EXFpremium;
                    }



                    // Reading TFM/TF
                    string TFMvalue = "";
                    string TFCoverage = "";
                    string TFDeductable = "";
                    string TFCoins = "";
                    string TFLimit = "";
                    string TFpremium = "";
                    for (int i = 0; i < PropLocationCountMC; i++)
                    {
                        if (oCFR.ScheduledScreens[2].Items[i][252].FieldValue != "")
                        {
                            TFMvalue = oCFR.ScheduledScreens[2].Items[i][143].FieldValue;
                            TFCoverage = oCFR.ScheduledScreens[2].Items[i][299].FieldValue;
                            TFDeductable = oCFR.ScheduledScreens[2].Items[i][237].FieldValue;
                            TFCoins = oCFR.ScheduledScreens[2].Items[i][180].FieldValue;
                            TFLimit = oCFR.ScheduledScreens[2].Items[i][252].FieldValue;
                            TFpremium = oCFR.ScheduledScreens[2].Items[i][259].FieldValue;
                            CECounter++;
                            break;
                        }
                    }
                    if (TFLimit != "")
                    {
                        (int, int, int, int, int, int) TFTuple = ContEqupField(CECounter);
                        if (TFMvalue != "")
                        {
                            nCFR.NonScheduledScreens[1].Fields[TFTuple.Item1].FieldValue = "TFM";
                        }
                        else
                        {
                            nCFR.NonScheduledScreens[1].Fields[TFTuple.Item1].FieldValue = "TF";
                        }
                        if (TFCoverage == "BR")
                        {
                            nCFR.NonScheduledScreens[1].Fields[TFTuple.Item2].FieldValue = "Broad Form";
                        }
                        else
                        {
                            nCFR.NonScheduledScreens[1].Fields[TFTuple.Item2].FieldValue = "Named Perils";
                        }
                        nCFR.NonScheduledScreens[1].Fields[TFTuple.Item3].FieldValue = TFDeductable;
                        if (TFCoins == "80")
                        {
                            nCFR.NonScheduledScreens[1].Fields[TFTuple.Item4].FieldValue = "80%";
                        }
                        if (TFCoins == "90")
                        {
                            nCFR.NonScheduledScreens[1].Fields[TFTuple.Item4].FieldValue = "90%";
                        }
                        if (TFCoins == "100")
                        {
                            nCFR.NonScheduledScreens[1].Fields[TFTuple.Item4].FieldValue = "100%";
                        }
                        nCFR.NonScheduledScreens[1].Fields[TFTuple.Item5].FieldValue = TFLimit;
                        nCFR.NonScheduledScreens[1].Fields[TFTuple.Item6].FieldValue = TFpremium;
                    }


                    //Reading Cargo
                    if (oCFR.ScheduledScreens[2].Items[0][146].FieldValue == "O")
                    {
                        nCFR.NonScheduledScreens[1].Fields[14].FieldValue = "MTC Owners Form";
                    }
                    if (oCFR.ScheduledScreens[2].Items[0][146].FieldValue == "C")
                    {
                        nCFR.NonScheduledScreens[1].Fields[14].FieldValue = "MTC  Carriers Legal";
                    }
                    // Testing feedback done
                    //Ded
                    nCFR.NonScheduledScreens[1].Fields[37].FieldValue = oCFR.ScheduledScreens[2].Items[0][174].FieldValue;
                    //Limit
                    nCFR.NonScheduledScreens[1].Fields[30].FieldValue = oCFR.ScheduledScreens[2].Items[0][231].FieldValue;
                    //Prem
                    nCFR.NonScheduledScreens[1].Fields[7].FieldValue = oCFR.ScheduledScreens[2].Items[0][265].FieldValue;

                    //Number of cargo schedules in old long form
                    int CargoSchNum = Math.Min(oCFR.ScheduledScreens[9].Items.Count, 6);
                    for (int i = 0; i < CargoSchNum; i++)
                    {
                        (int, int, int, int, int, int, int) CargoTuple = CargoFields(i);
                        nCFR.NonScheduledScreens[1].Fields[CargoTuple.Item1].FieldValue = oCFR.ScheduledScreens[9].Items[i][3].FieldValue;
                        nCFR.NonScheduledScreens[1].Fields[CargoTuple.Item2].FieldValue = oCFR.ScheduledScreens[9].Items[i][4].FieldValue;
                        nCFR.NonScheduledScreens[1].Fields[CargoTuple.Item3].FieldValue = oCFR.ScheduledScreens[9].Items[i][5].FieldValue;
                        nCFR.NonScheduledScreens[1].Fields[CargoTuple.Item4].FieldValue = oCFR.ScheduledScreens[9].Items[i][6].FieldValue;
                        nCFR.NonScheduledScreens[1].Fields[CargoTuple.Item5].FieldValue = oCFR.ScheduledScreens[9].Items[i][7].FieldValue;
                        nCFR.NonScheduledScreens[1].Fields[CargoTuple.Item6].FieldValue = oCFR.ScheduledScreens[9].Items[i][8].FieldValue;
                        nCFR.NonScheduledScreens[1].Fields[CargoTuple.Item7].FieldValue = oCFR.ScheduledScreens[9].Items[i][10].FieldValue;

                    }

                    int RemainingCargoSchedules = oCFR.ScheduledScreens[9].Items.Count - CargoSchNum;
                    if (RemainingCargoSchedules > 0)
                    {
                        string RemainingCargos = "";
                        for (int i = 6; i < RemainingCargoSchedules + 6; i++)
                        {
                            string RemCargo = "--" + oCFR.ScheduledScreens[9].Items[i][3].FieldValue + " " + oCFR.ScheduledScreens[9].Items[i][4].FieldValue + " "
                                + oCFR.ScheduledScreens[9].Items[i][5].FieldValue + " VIN#:" + oCFR.ScheduledScreens[9].Items[i][6].FieldValue
                                + " Deductible:" + oCFR.ScheduledScreens[9].Items[i][7].FieldValue + " Limit:" + oCFR.ScheduledScreens[9].Items[i][8].FieldValue
                                + " Premium:" + oCFR.ScheduledScreens[9].Items[i][10].FieldValue + System.Environment.NewLine;
                            RemainingCargos = RemainingCargos + RemCargo;
                        }
                        nCFR.NonScheduledScreens[1].Fields[9].FieldValue = RemainingCargos;
                    }
                    //Console.WriteLine("Misc Coverage Done");
                    if (LongFormUpdateSwitch == 1)
                    {
                        EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check                        
                    }
                }
                catch (Exception e)
                {
                    string e5 = oPolId + " | Long Form - Misc Coverage failed | " + e;
                    ErrorString = ErrorString + e5 + System.Environment.NewLine;
                    Console.WriteLine(e5);
                }





                //Liability
                try
                {

                
                    // Since Source form is non-scheduled, only a new scheduled item will be inserted in the destination
                    // inserting a schedule screen item in the source policy
                    string LSID = nCFR.ScheduledScreens[2].ScheduleID;
                    CBLServiceReference.FieldItems[] LFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, LSID);
                    nCFR.ScheduledScreens[2].Items.Insert(0, LFF[0]); //inserting at 0 since there is only 1
                    //1
                    nCFR.ScheduledScreens[2].Items[0][116].FieldValue = oCFR.NonScheduledScreens[2].Fields[46].FieldValue;
                    //2
                    nCFR.ScheduledScreens[2].Items[0][2].FieldValue = oCFR.NonScheduledScreens[2].Fields[44].FieldValue;
                    //2a
                    if (oCFR.NonScheduledScreens[2].Fields[32].FieldValue == "PA")
                    {
                        nCFR.ScheduledScreens[2].Items[0][30].FieldValue = "PIA";
                    }
                    if (oCFR.NonScheduledScreens[2].Fields[32].FieldValue == "PI")
                    {
                        nCFR.ScheduledScreens[2].Items[0][30].FieldValue = "PI";
                    }
                    //3
                    if (oCFR.NonScheduledScreens[2].Fields[23].FieldValue == "Y")
                    {
                        nCFR.ScheduledScreens[2].Items[0][9].FieldValue = "EL";
                    }
                    //4
                    if (oCFR.NonScheduledScreens[2].Fields[21].FieldValue == "Y")
                    {
                        nCFR.ScheduledScreens[2].Items[0][10].FieldValue = "96";
                    }
                    //5 - N/A
                    //6
                    if (oCFR.NonScheduledScreens[2].Fields[28].FieldValue == "Y")
                    {
                        nCFR.ScheduledScreens[2].Items[0][12].FieldValue = "CROSS LIABILITY";
                    }

                    // Testing feedback done - Employees' Benefit Liability
                    if (oCFR.NonScheduledScreens[2].Fields[30].FieldValue == "Y")
                    {
                    
                        //7
                        nCFR.ScheduledScreens[2].Items[0][13].FieldValue = "EMPLOYEE BENEFITS LIABILITY";
                        //31

                        //56

                        //81
                    }




                    //7 - 16 - N/A
                    //17
                    nCFR.ScheduledScreens[2].Items[0][3].FieldValue = oCFR.NonScheduledScreens[2].Fields[47].FieldValue;
                    //18
                    nCFR.ScheduledScreens[2].Items[0][4].FieldValue = oCFR.NonScheduledScreens[2].Fields[42].FieldValue;
                    //19
                    nCFR.ScheduledScreens[2].Items[0][92].FieldValue = oCFR.NonScheduledScreens[2].Fields[11].FieldValue;
                    //20 - N/A
                    //21
                    nCFR.ScheduledScreens[2].Items[0][88].FieldValue = oCFR.NonScheduledScreens[2].Fields[9].FieldValue;
                    //22
                    nCFR.ScheduledScreens[2].Items[0][86].FieldValue = oCFR.NonScheduledScreens[2].Fields[10].FieldValue;
                    //23
                    nCFR.ScheduledScreens[2].Items[0][82].FieldValue = oCFR.NonScheduledScreens[2].Fields[7].FieldValue;
                    //24 - 25 - N/A
                    //26
                    nCFR.ScheduledScreens[2].Items[0][72].FieldValue = oCFR.NonScheduledScreens[2].Fields[8].FieldValue;
                    //27-40 - N/A
                    //41
                    nCFR.ScheduledScreens[2].Items[0][5].FieldValue = oCFR.NonScheduledScreens[2].Fields[111].FieldValue;
                    //42
                    nCFR.ScheduledScreens[2].Items[0][6].FieldValue = oCFR.NonScheduledScreens[2].Fields[104].FieldValue;
                    //43
                    nCFR.ScheduledScreens[2].Items[0][41].FieldValue = oCFR.NonScheduledScreens[2].Fields[102].FieldValue;
                    //44
                    nCFR.ScheduledScreens[2].Items[0][43].FieldValue = oCFR.NonScheduledScreens[2].Fields[109].FieldValue;
                    //45
                    nCFR.ScheduledScreens[2].Items[0][45].FieldValue = oCFR.NonScheduledScreens[2].Fields[109].FieldValue;
                    //46
                    nCFR.ScheduledScreens[2].Items[0][47].FieldValue = oCFR.NonScheduledScreens[2].Fields[106].FieldValue;
                    //47
                    nCFR.ScheduledScreens[2].Items[0][49].FieldValue = oCFR.NonScheduledScreens[2].Fields[100].FieldValue;
                    //48
                    nCFR.ScheduledScreens[2].Items[0][51].FieldValue = oCFR.NonScheduledScreens[2].Fields[98].FieldValue;
                    //49
                    nCFR.ScheduledScreens[2].Items[0][53].FieldValue = oCFR.NonScheduledScreens[2].Fields[97].FieldValue;
                    //50 - N/A
                    //51
                    nCFR.ScheduledScreens[2].Items[0][57].FieldValue = oCFR.NonScheduledScreens[2].Fields[96].FieldValue;
                    //52-65 - N/A
                    //66
                    nCFR.ScheduledScreens[2].Items[0][7].FieldValue = oCFR.NonScheduledScreens[2].Fields[69].FieldValue;
                    //67
                    nCFR.ScheduledScreens[2].Items[0][8].FieldValue = oCFR.NonScheduledScreens[2].Fields[65].FieldValue;
                    //68
                    nCFR.ScheduledScreens[2].Items[0][24].FieldValue = oCFR.NonScheduledScreens[2].Fields[64].FieldValue;
                    //69
                    nCFR.ScheduledScreens[2].Items[0][25].FieldValue = oCFR.NonScheduledScreens[2].Fields[68].FieldValue;
                    //70
                    nCFR.ScheduledScreens[2].Items[0][26].FieldValue = oCFR.NonScheduledScreens[2].Fields[68].FieldValue;
                    //71
                    nCFR.ScheduledScreens[2].Items[0][27].FieldValue = oCFR.NonScheduledScreens[2].Fields[66].FieldValue;
                    //72
                    nCFR.ScheduledScreens[2].Items[0][28].FieldValue = oCFR.NonScheduledScreens[2].Fields[63].FieldValue;
                    //73
                    nCFR.ScheduledScreens[2].Items[0][29].FieldValue = oCFR.NonScheduledScreens[2].Fields[61].FieldValue;
                    //74
                    nCFR.ScheduledScreens[2].Items[0][31].FieldValue = oCFR.NonScheduledScreens[2].Fields[59].FieldValue;
                    //75
                    nCFR.ScheduledScreens[2].Items[0][33].FieldValue = oCFR.NonScheduledScreens[2].Fields[48].FieldValue;
                    //76
                    nCFR.ScheduledScreens[2].Items[0][35].FieldValue = oCFR.NonScheduledScreens[2].Fields[58].FieldValue;
                    //77 - N/A
                    //78
                    nCFR.ScheduledScreens[2].Items[0][39].FieldValue = oCFR.NonScheduledScreens[2].Fields[49].FieldValue;
                    //79-90 - N/A


                    //Console.WriteLine("Liability Done");
                    if (LongFormUpdateSwitch == 1)
                    {
                        EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check                        
                    }

                }
                catch (Exception e)
                {
                    string e6 = oPolId + " | Long Form - Liability failed | " + e;
                    ErrorString = ErrorString + e6 + System.Environment.NewLine;
                    Console.WriteLine(e6);
                }



                //Professional / Other Liability
                try
                {

                
                    //1-3 - N/A
                    //4
                    nCFR.NonScheduledScreens[2].Fields[78].FieldValue = oCFR.NonScheduledScreens[2].Fields[41].FieldValue;
                    //5
                    nCFR.NonScheduledScreens[2].Fields[76].FieldValue = oCFR.NonScheduledScreens[2].Fields[40].FieldValue;
                    //6-15 - N/A
                    //16
                    nCFR.NonScheduledScreens[2].Fields[99].FieldValue = oCFR.NonScheduledScreens[2].Fields[95].FieldValue;
                    //17
                    nCFR.NonScheduledScreens[2].Fields[119].FieldValue = oCFR.NonScheduledScreens[2].Fields[93].FieldValue;
                    //18-23 - N/A
                    //24
                    nCFR.NonScheduledScreens[2].Fields[107].FieldValue = oCFR.NonScheduledScreens[2].Fields[57].FieldValue;
                    //25
                    // Testing feedback done
                    nCFR.NonScheduledScreens[2].Fields[100].FieldValue = oCFR.NonScheduledScreens[2].Fields[55].FieldValue;
                    //26-34 - N/A
                    //35
                    nCFR.NonScheduledScreens[2].Fields[82].FieldValue = oCFR.NonScheduledScreens[2].Fields[39].FieldValue;
                    //36
                    nCFR.NonScheduledScreens[2].Fields[81].FieldValue = oCFR.NonScheduledScreens[2].Fields[38].FieldValue;
                    //37
                    nCFR.NonScheduledScreens[2].Fields[44].FieldValue = oCFR.NonScheduledScreens[2].Fields[36].FieldValue;
                    //38-52 - N/A
                    //53
                    nCFR.NonScheduledScreens[2].Fields[71].FieldValue = oCFR.NonScheduledScreens[2].Fields[6].FieldValue;
                    //54
                    nCFR.NonScheduledScreens[2].Fields[68].FieldValue = oCFR.NonScheduledScreens[2].Fields[5].FieldValue;
                    //55
                    nCFR.NonScheduledScreens[2].Fields[47].FieldValue = oCFR.NonScheduledScreens[2].Fields[4].FieldValue;
                    //56-61 - N/A
                    //62
                    nCFR.NonScheduledScreens[2].Fields[50].FieldValue = oCFR.NonScheduledScreens[2].Fields[94].FieldValue;
                    //63
                    nCFR.NonScheduledScreens[2].Fields[49].FieldValue = oCFR.NonScheduledScreens[2].Fields[92].FieldValue;
                    //64
                    nCFR.NonScheduledScreens[2].Fields[45].FieldValue = oCFR.NonScheduledScreens[2].Fields[89].FieldValue;
                    //65-70 - N/A
                    //71
                    nCFR.NonScheduledScreens[2].Fields[62].FieldValue = oCFR.NonScheduledScreens[2].Fields[56].FieldValue;
                    //72
                    nCFR.NonScheduledScreens[2].Fields[61].FieldValue = oCFR.NonScheduledScreens[2].Fields[54].FieldValue;
                    //73
                    nCFR.NonScheduledScreens[2].Fields[46].FieldValue = oCFR.NonScheduledScreens[2].Fields[51].FieldValue;
                    //74-75 - N/A
                    //76
                    if (oCFR.NonScheduledScreens[2].Fields[91].FieldValue != "" || oCFR.NonScheduledScreens[2].Fields[33].FieldValue != "")
                    {
                        nCFR.NonScheduledScreens[2].Fields[31].FieldValue = "U";
                    }
                    //77 - N/A
                    //78
                    nCFR.NonScheduledScreens[2].Fields[29].FieldValue = oCFR.NonScheduledScreens[2].Fields[35].FieldValue;
                    //79 - N/A
                    //80
                    nCFR.NonScheduledScreens[2].Fields[3].FieldValue = oCFR.NonScheduledScreens[2].Fields[91].FieldValue;
                    //81-N/A
                    //82
                    nCFR.NonScheduledScreens[2].Fields[27].FieldValue = oCFR.NonScheduledScreens[2].Fields[33].FieldValue;
                    //83 - N/A
                    //84
                    nCFR.NonScheduledScreens[2].Fields[6].FieldValue = oCFR.NonScheduledScreens[2].Fields[52].FieldValue;
                    //85-105 - N/A 

                    //Console.WriteLine("Professional / Other Liability Done");
                    if (LongFormUpdateSwitch == 1)
                    {
                        EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check                        
                    }

                }
                catch (Exception e)
                {
                    string e7 = oPolId + " | Long Form - Prof/Other Liability failed | " + e;
                    ErrorString = ErrorString + e7 + System.Environment.NewLine;
                    Console.WriteLine(e7);
                }


                // Scheduled Equipment
                try
                {

                

                    // Get number of locations
                    int oSchEquipCount = oCFR.ScheduledScreens[5].Items.Count;
                    // Read for each schedule
                    for (int i=0; i<oSchEquipCount; i++)
                    {
                        //Insert a schedule in new scheen
                        string SEID = nCFR.ScheduledScreens[3].ScheduleID;
                        CBLServiceReference.FieldItems[] SEF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, SEID);
                        nCFR.ScheduledScreens[3].Items.Insert(i, SEF[0]);

                        // Add fields for a given location
                        //1 - N/A
                        //2
                        nCFR.ScheduledScreens[3].Items[i][15].FieldValue = oCFR.ScheduledScreens[5].Items[i][4].FieldValue;
                        //3
                        string addint = oCFR.ScheduledScreens[5].Items[i][1].FieldValue;
                        if (addint.All(char.IsDigit) == true && addint != "")
                        {
                            nCFR.ScheduledScreens[3].Items[i][14].FieldValue = oCFR.ScheduledScreens[5].Items[i][1].FieldValue;
                        }

                    
                        //4
                        nCFR.ScheduledScreens[3].Items[i][13].FieldValue = oCFR.ScheduledScreens[5].Items[i][5].FieldValue;
                        //5
                        nCFR.ScheduledScreens[3].Items[i][12].FieldValue = oCFR.ScheduledScreens[5].Items[i][6].FieldValue;
                        //6
                        nCFR.ScheduledScreens[3].Items[i][11].FieldValue = oCFR.ScheduledScreens[5].Items[i][7].FieldValue;
                        //7
                        nCFR.ScheduledScreens[3].Items[i][10].FieldValue = oCFR.ScheduledScreens[5].Items[i][8].FieldValue;
                        //8 -9 - N/A
                        //10
                        nCFR.ScheduledScreens[3].Items[i][7].FieldValue = oCFR.ScheduledScreens[5].Items[i][0].FieldValue;
                        //11
                        nCFR.ScheduledScreens[3].Items[i][6].FieldValue = oCFR.ScheduledScreens[5].Items[i][10].FieldValue;
                        //12
                        nCFR.ScheduledScreens[3].Items[i][5].FieldValue = oCFR.ScheduledScreens[5].Items[i][2].FieldValue;
                        //13-16 - N/A
                        //17
                        nCFR.ScheduledScreens[3].Items[i][0].FieldValue = "Date of Purchase: " + oCFR.ScheduledScreens[5].Items[i][9].FieldValue + System.Environment.NewLine + "Original Cost: " + oCFR.ScheduledScreens[5].Items[i][11].FieldValue;
                    }
                    //Console.WriteLine("Sch Equipment Done");
                    if (LongFormUpdateSwitch == 1)
                    {
                        EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check                        
                    }
                }
                catch (Exception e)
                {
                    string e8 = oPolId + " | Long Form - Sch Equip. Failed failed | " + e;
                    ErrorString = ErrorString + e8 + System.Environment.NewLine;
                    Console.WriteLine(e8);
                }

                if (LongFormUpdateSwitch == 1)
                {
                    //EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR); //final check
                    CformUpdated = 1;
                    InitialLSFormStatus = true;
                }

                Console.WriteLine("Long Form Updated");
                //SQL-Commented out
                //if (SQLLongFormUpdate == 1)
                //{
                //    conn.Open();
                //    using (SqlCommand commandLongFormUpdate = conn.CreateCommand())
                //    {
                //        string sqlsix = string.Format("update {0} set CFormUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
                //        commandLongFormUpdate.CommandText = sqlsix;
                        
                //        commandLongFormUpdate.Parameters.AddWithValue("@OldPolID", oPolId);
                //        commandLongFormUpdate.ExecuteNonQuery();
                //    }
                //    conn.Close();
                //}


            }



            else if (SchduleScreenCount > 0) // Check if the form has any schedule screen forms
            {
                // Get the first schedule screen
                oSupScr = EpicSDKClient.Get_CustomForm_SupplementalScreen(oMessageHeader, oLineID, 0, CBLServiceReference.SupplementalScreenGetType.LineID)[0];
                //Check if short form or BSCA1.0
                if (oSupScr.Name == "Commercial Short Form Application")
                {
                    string SFDesc = oSupScr.FormDataValue[0].NonScheduledItemsValue[41].Value;
                    string SFLiabOccr = oSupScr.FormDataValue[3].NonScheduledItemsValue[50].Value;
                    if (SFDesc.Trim() != "" || SFLiabOccr.Trim() != "")
                    {
                        Console.WriteLine("*-*-*This is a Short Form*-*-*");

                        // Client Information
                        try
                        {

                        
                            //1
                            nCFR.NonScheduledScreens[0].Fields[50].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[40].Value;
                            //2 - 3  N/A
                            //4
                            nCFR.NonScheduledScreens[0].Fields[52].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[41].Value;
                            //5-6 N/A
                            //7
                            nCFR.NonScheduledScreens[0].Fields[56].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[39].Value;
                            //8 - N/A
                            //9
                            // testinf feedback done
                            if (oSupScr.FormDataValue[0].NonScheduledItemsValue[18].Value != "")
                            {
                                int SFBizSince = (DateTime.ParseExact(oSupScr.FormDataValue[0].NonScheduledItemsValue[18].Value, "MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture)).Year;
                                nCFR.NonScheduledScreens[0].Fields[37].FieldValue = SFBizSince.ToString();
                            }
                        
                            //10 N/A
                            //11
                            nCFR.NonScheduledScreens[0].Fields[35].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[26].Value;
                            //12
                            nCFR.NonScheduledScreens[0].Fields[34].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[23].Value;
                            //13
                            nCFR.NonScheduledScreens[0].Fields[33].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[17].Value;
                            //14 N/A
                            //15
                            nCFR.NonScheduledScreens[0].Fields[38].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[22].Value;
                            //16 N/A
                            //17
                            nCFR.NonScheduledScreens[0].Fields[47].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[25].Value;
                            //18
                            nCFR.NonScheduledScreens[0].Fields[45].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[21].Value;
                            //19
                            nCFR.NonScheduledScreens[0].Fields[43].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[27].Value;
                            //20
                            nCFR.NonScheduledScreens[0].Fields[41].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[20].Value;
                            //21
                            nCFR.NonScheduledScreens[0].Fields[46].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[24].Value;
                            //22 N/A
                            //23
                            nCFR.NonScheduledScreens[0].Fields[44].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[31].Value;
                            //24 N/A
                            //25
                            nCFR.NonScheduledScreens[0].Fields[32].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[29].Value;
                            //26
                            nCFR.NonScheduledScreens[0].Fields[31].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[28].Value;
                            //27
                            nCFR.NonScheduledScreens[0].Fields[30].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[30].Value;
                            //28 N/A
                            //29
                            if (oSupScr.FormDataValue[0].NonScheduledItemsValue[1].Value != "" || oSupScr.FormDataValue[0].NonScheduledItemsValue[2].Value != "" || oSupScr.FormDataValue[0].NonScheduledItemsValue[3].Value != "")
                            {
                                nCFR.NonScheduledScreens[0].Fields[28].FieldValue = "Yes";
                            }
                            else
                            {
                                nCFR.NonScheduledScreens[0].Fields[28].FieldValue = "No";
                            }
                            //30
                            nCFR.NonScheduledScreens[0].Fields[27].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[1].Value;
                            //31 N/A
                            //32
                            nCFR.NonScheduledScreens[0].Fields[23].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[7].Value;
                            //33
                            nCFR.NonScheduledScreens[0].Fields[20].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[10].Value;
                            //34
                            nCFR.NonScheduledScreens[0].Fields[18].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[15].Value;
                            //35
                            nCFR.NonScheduledScreens[0].Fields[26].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[2].Value;
                            //36 N/A
                            //37
                            nCFR.NonScheduledScreens[0].Fields[22].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[8].Value;
                            //38
                            nCFR.NonScheduledScreens[0].Fields[19].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[11].Value;
                            //39
                            nCFR.NonScheduledScreens[0].Fields[17].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[14].Value;
                            //40
                            nCFR.NonScheduledScreens[0].Fields[21].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[3].Value + " "
                                + oSupScr.FormDataValue[0].NonScheduledItemsValue[9].Value + " Paid/Reserved:$" + oSupScr.FormDataValue[0].NonScheduledItemsValue[12].Value
                                + " Closed:"+ oSupScr.FormDataValue[0].NonScheduledItemsValue[13].Value;
                            //41
                            nCFR.NonScheduledScreens[0].Fields[16].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[32].Value;
                            //42
                            nCFR.NonScheduledScreens[0].Fields[13].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[37].Value;
                            //43
                            nCFR.NonScheduledScreens[0].Fields[15].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[33].Value;
                            //44
                            nCFR.NonScheduledScreens[0].Fields[14].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[34].Value;
                            //45
                            nCFR.NonScheduledScreens[0].Fields[12].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[35].Value;
                            //46
                            nCFR.NonScheduledScreens[0].Fields[11].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[36].Value;
                            //47
                            nCFR.NonScheduledScreens[0].Fields[10].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[38].Value;
                            //48
                            nCFR.NonScheduledScreens[0].Fields[9].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[49].Value;
                            //49
                            nCFR.NonScheduledScreens[0].Fields[7].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[46].Value;
                            //50
                            nCFR.NonScheduledScreens[0].Fields[5].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[51].Value;
                            //51
                            nCFR.NonScheduledScreens[0].Fields[3].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[48].Value;
                            //52
                            nCFR.NonScheduledScreens[0].Fields[1].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[45].Value;
                            //53
                            nCFR.NonScheduledScreens[0].Fields[8].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[44].Value;
                            //54
                            nCFR.NonScheduledScreens[0].Fields[6].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[47].Value;
                            //55
                            nCFR.NonScheduledScreens[0].Fields[4].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[43].Value;
                            //56
                            nCFR.NonScheduledScreens[0].Fields[2].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[50].Value;
                            //57
                            nCFR.NonScheduledScreens[0].Fields[0].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[42].Value;

                            if (ShortFormUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }
                                                       
                        }
                        catch (Exception e)
                        {
                            string e9 = oPolId + " | Short Form - Client Info failed | " + e;
                            ErrorString = ErrorString + e9 + System.Environment.NewLine;
                            Console.WriteLine(e9);
                        }


                        //COPE
                        try
                        {

                        
                            // Get number of locations from Short Form COPE
                            int SFLocationCount = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count;
                            int CovScreenCount = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count;
                            // Read for each location
                            for (int i = 0; i < SFLocationCount; i++)
                            {
                                // First insert a Location Item
                                string SFCOPEID = nCFR.ScheduledScreens[0].ScheduleID;
                                CBLServiceReference.FieldItems[] SFCOPEFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, SFCOPEID);
                                nCFR.ScheduledScreens[0].Items.Insert(i, SFCOPEFF[0]);
                                // Add fields for a given location
                                //1
                                nCFR.ScheduledScreens[0].Items[i][36].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[2].Value;
                                //2 - 5 N/A
                                //6
                                nCFR.ScheduledScreens[0].Items[i][32].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[8].Value;
                                //7
                                nCFR.ScheduledScreens[0].Items[i][31].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[9].Value;
                                //8
                                nCFR.ScheduledScreens[0].Items[i][30].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[10].Value;
                                //9
                                nCFR.ScheduledScreens[0].Items[i][29].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[11].Value;
                                //10
                                nCFR.ScheduledScreens[0].Items[i][28].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[0].Value;
                                //11 - N/A
                                //12
                                for (int j = 0; j < CovScreenCount; j++) // for all the coverage screens
                                {
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[j].ItemsValue[282].Value != "") // if the value is not blank and location number is same
                                    {
                                        if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[j].ItemsValue[282].Value == oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[2].Value)
                                        {
                                            if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[j].ItemsValue[255].Value == "410A")
                                            {
                                                nCFR.ScheduledScreens[0].Items[i][26].FieldValue = "Yes";
                                            }
                                        }
                                    }
                                }
                                //13 - N/A
                                //14
                                nCFR.ScheduledScreens[0].Items[i][23].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[38].Value;
                                //15
                                nCFR.ScheduledScreens[0].Items[i][22].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[37].Value;
                                //16
                                nCFR.ScheduledScreens[0].Items[i][21].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[36].Value;
                                //17-18 N/A
                                //19
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[30].Value == "R")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "FR";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[30].Value == "M")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MS";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[30].Value == "V")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "MV";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[30].Value == "F")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][18].FieldValue = "FM";
                                }
                                //20
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[35].Value == "PC")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][15].FieldValue = "RC";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[35].Value == "HCB")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][15].FieldValue = "HCBM";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[35].Value == "FRA")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][15].FieldValue = "F";
                                }
                                //21
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "1")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "CGF";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "2")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "COF";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "5")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "EH";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "6")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "CHW";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "19")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "NH";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "10" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "11" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "12")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "PHD";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "9" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "33" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value == "34")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][12].FieldValue = "SFS";
                                }
                                //22
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[60].Value == "10")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][17].FieldValue = "RC";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[60].Value == "6")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][17].FieldValue = "W";
                                }
                                //23
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[53].Value == "7")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][14].FieldValue = "SD";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[53].Value == "2")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][14].FieldValue = "WJ";
                                }
                                //24
                                nCFR.ScheduledScreens[0].Items[i][11].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[45].Value;
                                //25
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[58].Value == "NB")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][16].FieldValue = "No";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[58].Value == "FOA" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[58].Value == "HCB" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[58].Value == "PC")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][16].FieldValue = "Yes";
                                }
                                //26 
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value == "6")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][13].FieldValue = "B";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value == "8")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][13].FieldValue = "F";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value == "5")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][13].FieldValue = "O";
                                }
                                //27
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value == "1")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][10].FieldValue = "A";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value == "2")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][10].FieldValue = "C";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value == "3")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][10].FieldValue = "KT";
                                }
                                //28
                                nCFR.ScheduledScreens[0].Items[i][9].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[32].Value;
                                //29
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[27].Value == "COMPLETE")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][8].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[25].Value;
                                }
                                //30
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[27].Value == "PARTIAL")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][4].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[25].Value;
                                }
                                //31
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[26].Value == "COMPLETE")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][7].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[24].Value;
                                }
                                //32
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[26].Value == "PARTIAL")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][3].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[24].Value;
                                }
                                //33
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[23].Value == "COMPLETE")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][6].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[22].Value;
                                }
                                //34
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[23].Value == "PARTIAL")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][2].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[22].Value;
                                }
                                //35
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[21].Value == "COMPLETE")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][5].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[20].Value;
                                }
                                //36
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[21].Value == "PARTIAL")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][1].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[20].Value;
                                }
                                //37
                                nCFR.ScheduledScreens[0].Items[i][0].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[19].Value;
                                //38
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[18].Value == "1")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][39].FieldValue = "Yes";
                                }
                                //39 N/A
                                //40
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value == "6")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][38].FieldValue = "L";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value == "998")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][38].FieldValue = "N";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value == "4" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value == "5")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][38].FieldValue = "M";
                                }
                                //41, 42 N/A
                                //43
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[17].Value == "0")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "NH";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[17].Value == "1")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "100";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[17].Value == "2")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "300";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[17].Value == "3")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][44].FieldValue = "300+";
                                }
                                //44
                                string OldFireProtectionVal = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[15].Value;
                                if (OldFireProtectionVal == "A" || OldFireProtectionVal == "B")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "-2.5";
                                }
                                if (OldFireProtectionVal == "C" || OldFireProtectionVal == "D" || OldFireProtectionVal == "E" || OldFireProtectionVal == "1")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "-5";
                                }
                                if (OldFireProtectionVal == "F" || OldFireProtectionVal == "G" || OldFireProtectionVal == "H" || OldFireProtectionVal == "2")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "-8";
                                }
                                if (Regex.IsMatch(OldFireProtectionVal, @"[I-Z]") || OldFireProtectionVal == "4" || OldFireProtectionVal == "5")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][40].FieldValue = "+8";
                                }
                                //45
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value == "6")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][43].FieldValue = "L";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value == "998")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][43].FieldValue = "N";
                                }
                                if (oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value == "4" || oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value == "5")
                                {
                                    nCFR.ScheduledScreens[0].Items[i][43].FieldValue = "M";
                                }
                                //46
                                nCFR.ScheduledScreens[0].Items[i][42].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[12].Value;

                            }
                            if (ShortFormUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }

                        }
                        catch (Exception e)
                        {
                            string e10 = oPolId + " | Short Form - COPE failed | " + e;
                            ErrorString = ErrorString + e10 + System.Environment.NewLine;
                            Console.WriteLine(e10);
                        }

                        // Short Form - Property
                        try
                        {

                        
                            // Get number of Property schedules from ShortForm - Coverage Screen
                            int PropSFLocationCount = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count;
                            // Read for each schedule
                            for (int i = 0; i < PropSFLocationCount; i++)
                            {
                                // First insert a Property Item
                                string SFPID = nCFR.ScheduledScreens[1].ScheduleID;
                                CBLServiceReference.FieldItems[] SFPFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, SFPID);
                                nCFR.ScheduledScreens[1].Items.Insert(i, SFPFF[0]);
                                // Add fields for a given Property Schedule
                                List<string> SFRowValues = new List<string>() {"1","2A","3","6","7","23","21","22","5" };
                                List<string> SFFreeFormVals = new List<string>() {"20", "10", "9", "13", "11", "14", "4", "8", "25", "19" };
                                int SFPropertyFFCounter = 4;
                                //1-100
                                // Read row 1 of old policy SF
                                string Row1Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[108].Value;
                                string Row1Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[192].Value;
                                if (SFRowValues.Contains(Row1Val))
                                {
                                    (int, int, int, int, int, int) SFRow1Tuple = SFPropertyMapping(Row1Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[175].Value;
                                    if (Row1Valuation == "RC" || Row1Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple.Item2].FieldValue = Row1Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[192].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[203].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[177].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[118].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[124].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row1Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow1Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item1].FieldValue = SFPropertyDesc(Row1Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[175].Value;
                                    if (Row1Valuation == "RC" || Row1Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item3].FieldValue = Row1Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[192].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[203].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[177].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[118].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow1Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[124].Value;
                                }
                            
                                string Row2Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[107].Value;
                                string Row2Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[174].Value; 
                                if (SFRowValues.Contains(Row2Val))
                                {
                                    (int, int, int, int, int, int) SFRow2Tuple = SFPropertyMapping(Row2Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[106].Value;
                                    if (Row2Valuation == "RC" || Row2Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple.Item2].FieldValue = Row2Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[174].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[73].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[157].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[113].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[51].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row2Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow2Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item1].FieldValue = SFPropertyDesc(Row2Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[106].Value;
                                    if (Row2Valuation == "RC" || Row2Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item3].FieldValue = Row2Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[174].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[73].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[157].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[113].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow2Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[51].Value;
                                }
                                                        
                                string Row3Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[105].Value;
                                string Row3Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[96].Value;
                                if (SFRowValues.Contains(Row3Val))
                                {
                                    (int, int, int, int, int, int) SFRow3Tuple = SFPropertyMapping(Row3Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[87].Value;
                                    if (Row3Valuation == "RC" || Row3Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple.Item2].FieldValue = Row3Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[96].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[182].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[117].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[187].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[160].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row3Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow3Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item1].FieldValue = SFPropertyDesc(Row3Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[87].Value;
                                    if (Row3Valuation == "RC" || Row3Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item3].FieldValue = Row3Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[96].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[182].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[117].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[187].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow3Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[160].Value;
                                }

                                string Row4Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[186].Value;
                                string Row4Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[146].Value;
                                if (SFRowValues.Contains(Row4Val))
                                {
                                    (int, int, int, int, int, int) SFRow4Tuple = SFPropertyMapping(Row4Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[169].Value;
                                    if (Row4Valuation == "RC" || Row4Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple.Item2].FieldValue = Row4Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[146].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[72].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[196].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[112].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row4Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow4Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item1].FieldValue = SFPropertyDesc(Row4Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[169].Value;
                                    if (Row4Valuation == "RC" || Row4Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item3].FieldValue = Row4Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[146].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[72].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[196].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[112].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow4Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value;
                                }


                                string Row5Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[104].Value;
                                string Row5Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[95].Value;
                                if (SFRowValues.Contains(Row5Val))
                                {
                                    (int, int, int, int, int, int) SFRow5Tuple = SFPropertyMapping(Row5Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[86].Value;
                                    if (Row5Valuation == "RC" || Row5Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple.Item2].FieldValue = Row5Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[95].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[134].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[116].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[154].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[123].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row5Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow5Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item1].FieldValue = SFPropertyDesc(Row5Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[86].Value;
                                    if (Row5Valuation == "RC" || Row5Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item3].FieldValue = Row5Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[95].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[134].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[116].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[154].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow5Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[123].Value;
                                }


                                string Row6Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[150].Value;
                                string Row6Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[171].Value;
                                if (SFRowValues.Contains(Row6Val))
                                {
                                    (int, int, int, int, int, int) SFRow6Tuple = SFPropertyMapping(Row6Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[141].Value;
                                    if (Row6Valuation == "RC" || Row6Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple.Item2].FieldValue = Row6Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[171].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[71].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[156].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[111].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[49].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row6Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow6Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item1].FieldValue = SFPropertyDesc(Row6Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[141].Value;
                                    if (Row6Valuation == "RC" || Row6Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item3].FieldValue = Row6Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[171].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[71].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[156].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[111].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow6Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[49].Value;
                                }


                                string Row7Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[103].Value;
                                string Row7Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[94].Value;
                                if (SFRowValues.Contains(Row7Val))
                                {
                                    (int, int, int, int, int, int) SFRow7Tuple = SFPropertyMapping(Row7Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[85].Value;
                                    if (Row7Valuation == "RC" || Row7Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple.Item2].FieldValue = Row7Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[94].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[165].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[115].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[110].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[179].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row7Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow7Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item1].FieldValue = SFPropertyDesc(Row7Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[85].Value;
                                    if (Row7Valuation == "RC" || Row7Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item3].FieldValue = Row7Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[94].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[165].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[115].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[110].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow7Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[179].Value;
                                }



                                string Row8Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[173].Value;
                                string Row8Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[145].Value;
                                if (SFRowValues.Contains(Row8Val))
                                {
                                    (int, int, int, int, int, int) SFRow8Tuple = SFPropertyMapping(Row8Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[197].Value;
                                    if (Row8Valuation == "RC" || Row8Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple.Item2].FieldValue = Row8Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[145].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[70].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[176].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[153].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[48].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row8Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow8Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item1].FieldValue = SFPropertyDesc(Row8Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[197].Value;
                                    if (Row8Valuation == "RC" || Row8Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item3].FieldValue = Row8Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[145].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[70].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[176].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[153].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow8Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[48].Value;
                                }



                                string Row9Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[102].Value;
                                string Row9Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[93].Value;
                                if (SFRowValues.Contains(Row9Val))
                                {
                                    (int, int, int, int, int, int) SFRow9Tuple = SFPropertyMapping(Row9Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[84].Value;
                                    if (Row9Valuation == "RC" || Row9Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple.Item2].FieldValue = Row9Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[93].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[133].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[114].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[109].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[122].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row9Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow9Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item1].FieldValue = SFPropertyDesc(Row9Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[84].Value;
                                    if (Row9Valuation == "RC" || Row9Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item3].FieldValue = Row9Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[93].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[133].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[114].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[109].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow9Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[122].Value;
                                }


                                string Row10Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[149].Value;
                                string Row10Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[191].Value;
                                if (SFRowValues.Contains(Row10Val))
                                {
                                    (int, int, int, int, int, int) SFRow10Tuple = SFPropertyMapping(Row10Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[140].Value;
                                    if (Row10Valuation == "RC" || Row10Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple.Item2].FieldValue = Row10Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[191].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[69].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[155].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[152].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[47].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row10Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow10Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item1].FieldValue = SFPropertyDesc(Row10Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[140].Value;
                                    if (Row10Valuation == "RC" || Row10Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item3].FieldValue = Row10Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[191].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[69].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[155].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[152].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow10Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[47].Value;
                                }

                                string Row11Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[101].Value;
                                string Row11Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[92].Value;
                                if (SFRowValues.Contains(Row11Val))
                                {
                                    (int, int, int, int, int, int) SFRow11Tuple = SFPropertyMapping(Row11Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[83].Value;
                                    if (Row11Valuation == "RC" || Row11Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple.Item2].FieldValue = Row11Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[92].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[193].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[78].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[130].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[159].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row11Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow11Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item1].FieldValue = SFPropertyDesc(Row11Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[83].Value;
                                    if (Row11Valuation == "RC" || Row11Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item3].FieldValue = Row11Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[92].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[193].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[78].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[130].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow11Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[159].Value;
                                }



                                string Row12Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[194].Value;
                                string Row12Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[144].Value;
                                if (SFRowValues.Contains(Row12Val))
                                {
                                    (int, int, int, int, int, int) SFRow12Tuple = SFPropertyMapping(Row12Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[168].Value;
                                    if (Row12Valuation == "RC" || Row12Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple.Item2].FieldValue = Row12Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[144].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[68].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[137].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[63].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[46].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row12Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow12Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item1].FieldValue = SFPropertyDesc(Row12Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[168].Value;
                                    if (Row12Valuation == "RC" || Row12Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item3].FieldValue = Row12Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[144].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[68].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[137].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[63].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow12Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[46].Value;
                                }


                                string Row13Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[100].Value;
                                string Row13Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[91].Value;
                                if (SFRowValues.Contains(Row13Val))
                                {
                                    (int, int, int, int, int, int) SFRow13Tuple = SFPropertyMapping(Row13Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[82].Value;
                                    if (Row13Valuation == "RC" || Row13Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple.Item2].FieldValue = Row13Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[91].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[132].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[77].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[163].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[121].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row13Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow13Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item1].FieldValue = SFPropertyDesc(Row13Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[82].Value;
                                    if (Row13Valuation == "RC" || Row13Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item3].FieldValue = Row13Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[91].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[132].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[77].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[163].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow13Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[121].Value;
                                }


                                string Row14Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[148].Value;
                                string Row14Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[170].Value;
                                if (SFRowValues.Contains(Row14Val))
                                {
                                    (int, int, int, int, int, int) SFRow14Tuple = SFPropertyMapping(Row14Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[139].Value;
                                    if (Row14Valuation == "RC" || Row14Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple.Item2].FieldValue = Row14Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[170].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[67].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[190].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[62].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[45].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row14Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow14Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item1].FieldValue = SFPropertyDesc(Row14Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[139].Value;
                                    if (Row14Valuation == "RC" || Row14Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item3].FieldValue = Row14Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[170].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[67].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[190].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[62].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow14Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[45].Value;
                                }


                                string Row15Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[99].Value;
                                string Row15Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[90].Value;
                                if (SFRowValues.Contains(Row15Val))
                                {
                                    (int, int, int, int, int, int) SFRow15Tuple = SFPropertyMapping(Row15Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[81].Value;
                                    if (Row15Valuation == "RC" || Row15Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple.Item2].FieldValue = Row15Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[90].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[164].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[76].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[129].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[188].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row15Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow15Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item1].FieldValue = SFPropertyDesc(Row15Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[81].Value;
                                    if (Row15Valuation == "RC" || Row15Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item3].FieldValue = Row15Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[90].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[164].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[76].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[129].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow15Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[188].Value;
                                }



                                string Row16Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[172].Value;
                                string Row16Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[143].Value;
                                if (SFRowValues.Contains(Row16Val))
                                {
                                    (int, int, int, int, int, int) SFRow16Tuple = SFPropertyMapping(Row16Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[183].Value;
                                    if (Row16Valuation == "RC" || Row16Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple.Item2].FieldValue = Row16Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[143].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[66].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[136].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[61].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[44].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row16Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow16Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item1].FieldValue = SFPropertyDesc(Row16Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[183].Value;
                                    if (Row16Valuation == "RC" || Row16Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item3].FieldValue = Row16Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[143].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[66].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[136].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[61].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow16Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[44].Value;
                                }



                                string Row17Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[98].Value;
                                string Row17Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[89].Value;
                                if (SFRowValues.Contains(Row17Val))
                                {
                                    (int, int, int, int, int, int) SFRow17Tuple = SFPropertyMapping(Row17Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[80].Value;
                                    if (Row17Valuation == "RC" || Row17Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple.Item2].FieldValue = Row17Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[89].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[131].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[75].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[189].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[120].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row17Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow17Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item1].FieldValue = SFPropertyDesc(Row17Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[80].Value;
                                    if (Row17Valuation == "RC" || Row17Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item3].FieldValue = Row17Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[89].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[131].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[75].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[189].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow17Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[120].Value;
                                }



                                string Row18Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[147].Value;
                                string Row18Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[184].Value;
                                if (SFRowValues.Contains(Row18Val))
                                {
                                    (int, int, int, int, int, int) SFRow18Tuple = SFPropertyMapping(Row18Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[138].Value;
                                    if (Row18Valuation == "RC" || Row18Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple.Item2].FieldValue = Row18Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[184].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[65].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[166].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[60].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[227].Value;
                                }
                                else if (SFFreeFormVals.Contains(Row18Val) && SFPropertyFFCounter < 9)
                                {
                                    SFPropertyFFCounter++;
                                    (int, int, int, int, int, int, int) SFRow18Tuple1 = SFFreeFormMapping(SFPropertyFFCounter);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item1].FieldValue = SFPropertyDesc(Row18Val);
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[138].Value;
                                    if (Row18Valuation == "RC" || Row18Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item3].FieldValue = Row18Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[184].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[65].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[166].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[60].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRow18Tuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[227].Value;
                                }



                                // Adding tenant TLI
                                if (nCFR.ScheduledScreens[1].Items[i][191].FieldValue != "" && nCFR.ScheduledScreens[1].Items[i][107].FieldValue != "" && nCFR.ScheduledScreens[1].Items[i][56].FieldValue != "")
                                {
                                    nCFR.ScheduledScreens[1].Items[i][95].FieldValue = "TLI";
                                }

                                // 4 freeforms from old
                                int SFPropertyFFCounter2 = 0;

                                string SFFreeForm1Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[88].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[39].Value != "" && SFPropertyFFCounter2 < 4)
                                {
                                    SFPropertyFFCounter2++;
                                    (int, int, int, int, int, int, int) SFRowFFTuple1 = SFFreeFormMapping(SFPropertyFFCounter2);
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[39].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[79].Value;
                                    if (SFFreeForm1Valuation == "RC" || SFFreeForm1Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item3].FieldValue = SFFreeForm1Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[88].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[181].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[74].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[128].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[158].Value;
                                }

                                string SFFreeForm2Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[142].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[38].Value != "" && SFPropertyFFCounter2 < 4)
                                {
                                    SFPropertyFFCounter2++;
                                    (int, int, int, int, int, int, int) SFRowFFTuple2 = SFFreeFormMapping(SFPropertyFFCounter2);
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[38].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[167].Value;
                                    if (SFFreeForm2Valuation == "RC" || SFFreeForm2Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item3].FieldValue = SFFreeForm2Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[142].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[64].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[135].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[59].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple2.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[42].Value;
                                }

                                string SFFreeForm3Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[180].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[127].Value != "" && SFPropertyFFCounter2 < 4)
                                {
                                    SFPropertyFFCounter2++;
                                    (int, int, int, int, int, int, int) SFRowFFTuple3 = SFFreeFormMapping(SFPropertyFFCounter2);
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[127].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[56].Value;
                                    if (SFFreeForm3Valuation == "RC" || SFFreeForm3Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item3].FieldValue = SFFreeForm3Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[180].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[55].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[126].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[161].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple3.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[119].Value;
                                }

                                string SFFreeForm4Valuation = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[54].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value != "" && SFPropertyFFCounter2 < 4)
                                {
                                    SFPropertyFFCounter2++;
                                    (int, int, int, int, int, int, int) SFRowFFTuple4 = SFFreeFormMapping(SFPropertyFFCounter2);
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item1].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[125].Value;
                                    if (SFFreeForm4Valuation == "RC" || SFFreeForm4Valuation == "ACV")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item3].FieldValue = SFFreeForm4Valuation;
                                    }
                                    //nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[54].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[195].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[53].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[52].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFRowFFTuple4.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[41].Value;
                                }

                            

                                //101
                                nCFR.ScheduledScreens[1].Items[i][42].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[40].Value;
                                //102
                                nCFR.ScheduledScreens[1].Items[i][201].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[178].Value;

                                //103-144
                                int SFBICount = 0;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[208].Value != "")
                                {
                                    SFBICount++;
                                    (int, int, int, int, int, int) SFBITuple1 = SFBIMapping(SFBICount);
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[208].Value == "100")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item1].FieldValue = "ALS";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[208].Value == "101")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item1].FieldValue = "GR";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[208].Value == "102")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item1].FieldValue = "P";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[208].Value == "103")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item1].FieldValue = "EE";
                                    }
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[200].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[217].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[203].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[211].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[210].Value;
                                }

                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[216].Value != "")
                                {
                                    SFBICount++;
                                    (int, int, int, int, int, int) SFBITuple2 = SFBIMapping(SFBICount);
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[216].Value == "100")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item1].FieldValue = "ALS";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[216].Value == "101")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item1].FieldValue = "GR";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[216].Value == "102")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item1].FieldValue = "P";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[216].Value == "103")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item1].FieldValue = "EE";
                                    }
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[199].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[207].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[202].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[206].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple2.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[205].Value;
                                }

                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[213].Value != "")
                                {
                                    SFBICount++;
                                    (int, int, int, int, int, int) SFBITuple3 = SFBIMapping(SFBICount);
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[213].Value == "100")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item1].FieldValue = "ALS";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[213].Value == "101")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item1].FieldValue = "GR";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[213].Value == "102")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item1].FieldValue = "P";
                                    }
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[213].Value == "103")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item1].FieldValue = "EE";
                                    }
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[198].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[212].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[201].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[214].Value;
                                    nCFR.ScheduledScreens[1].Items[i][SFBITuple3.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[215].Value;
                                }

                                //145
                                nCFR.ScheduledScreens[1].Items[i][223].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[209].Value;
                                //146
                                nCFR.ScheduledScreens[1].Items[i][219].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[204].Value;
                                //147, 149, 150, 151, 152
                                List<int> SFEBS = new List<int>() {255, 242, 265, 241, 254, 240, 262, 239, 253, 238};
                                List<string> EBCov1Options = new List<string> { "408", "409", "410", "410A" };
                                foreach (int element in SFEBS)
                                {
                                    if (EBCov1Options.Contains(oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[element].Value))
                                    {
                                        string EBCov1Val = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[element].Value;
                                        if(EBCov1Val == "408")
                                        {
                                            nCFR.ScheduledScreens[1].Items[i][38].FieldValue = "BM1";
                                            nCFR.ScheduledScreens[1].Items[i][26].FieldValue = "No";
                                        }
                                        else if (EBCov1Val == "409")
                                        {
                                            nCFR.ScheduledScreens[1].Items[i][38].FieldValue = "BM2";
                                            nCFR.ScheduledScreens[1].Items[i][26].FieldValue = "No";
                                        }
                                        else if (EBCov1Val == "410")
                                        {
                                            nCFR.ScheduledScreens[1].Items[i][38].FieldValue = "BM3";
                                            nCFR.ScheduledScreens[1].Items[i][26].FieldValue = "No";
                                        }
                                        else if (EBCov1Val == "410A")
                                        {
                                            nCFR.ScheduledScreens[1].Items[i][38].FieldValue = "BM3";
                                            nCFR.ScheduledScreens[1].Items[i][26].FieldValue = "Yes";
                                        }
                                        (int, int, int) EB1Tuple1 = EBdescMapping(element);
                                        nCFR.ScheduledScreens[1].Items[i][33].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[EB1Tuple1.Item1].Value;
                                        nCFR.ScheduledScreens[1].Items[i][25].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[EB1Tuple1.Item2].Value;
                                        nCFR.ScheduledScreens[1].Items[i][37].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[EB1Tuple1.Item3].Value;
                                        break;
                                    }
                                }
                                //148 N/A
                                //153-158
                                foreach (int element2 in SFEBS)
                                {
                                    if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[element2].Value == "406")
                                    {
                                        nCFR.ScheduledScreens[1].Items[i][29].FieldValue = "SP";
                                        nCFR.ScheduledScreens[1].Items[i][16].FieldValue = "No";
                                        (int, int, int) EB1Tuple2 = EBdescMapping(element2);
                                        nCFR.ScheduledScreens[1].Items[i][35].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[EB1Tuple2.Item1].Value;
                                        nCFR.ScheduledScreens[1].Items[i][15].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[EB1Tuple2.Item2].Value;
                                        nCFR.ScheduledScreens[1].Items[i][27].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[EB1Tuple2.Item3].Value;
                                        break;
                                    }
                                }
                                //159-170 N/A
                                //171
                                nCFR.ScheduledScreens[1].Items[i][13].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[221].Value;
                                //172, 173 N/A
                                //174
                                nCFR.ScheduledScreens[1].Items[i][24].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[220].Value;
                                //175
                                nCFR.ScheduledScreens[1].Items[i][32].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[219].Value;
                                //176
                                nCFR.ScheduledScreens[1].Items[i][7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[218].Value;
                                //177
                                nCFR.ScheduledScreens[1].Items[i][19].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[257].Value;
                                //178-179 N/A
                                //180
                                nCFR.ScheduledScreens[1].Items[i][9].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[244].Value;
                                //181
                                nCFR.ScheduledScreens[1].Items[i][8].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[243].Value;
                                //182
                                nCFR.ScheduledScreens[1].Items[i][23].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[256].Value;
                                //183
                                nCFR.ScheduledScreens[1].Items[i][31].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[267].Value;
                                //184 N/A
                                //185
                                List<string> EBRemarkOptions = new List<string> { "407", "401", "404", "405", "403", "400" };
                                string EBRemarkSet = "";
                                foreach (int element3 in SFEBS)
                                {
                                    if (EBRemarkOptions.Contains(oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[element3].Value))
                                    {
                                        string EBRem1 = "";
                                        string EBRemElem = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[element3].Value;
                                        if (EBRemElem == "407") { EBRem1 = "Water Damage"; }
                                        if (EBRemElem == "401") { EBRem1 = "Air Conditioning Systems"; }
                                        if (EBRemElem == "404") { EBRem1 = "Business Interruption (follow form property)"; }
                                        if (EBRemElem == "405") { EBRem1 = "Consequential Loss (including off premises power)"; }
                                        if (EBRemElem == "403") { EBRem1 = "Electronic Equipment"; }
                                        if (EBRemElem == "400") { EBRem1 = "Equipment Breakdown - Insured Objects"; }
                                        (int, int, int) EB3Tuple3 = EBdescMapping(element3);
                                        string EBRem2 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[EB3Tuple3.Item2].Value;
                                        EBRemarkSet = EBRemarkSet+"COVERAGE: " + EBRem1 + " LIMIT: " + EBRem2 + Environment.NewLine;
                                    }
                                }
                                nCFR.ScheduledScreens[1].Items[i][22].FieldValue = EBRemarkSet;
                                //186
                                nCFR.ScheduledScreens[1].Items[i][254].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[282].Value;

                            }
                            if (ShortFormUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }


                        }
                        catch (Exception e)
                        {
                            string e11 = oPolId + " | Short Form - Property failed | " + e;
                            ErrorString = ErrorString + e11 + System.Environment.NewLine;
                            Console.WriteLine(e11);
                        }



                        // Short Form - Misc. Coverage
                        try
                        {

                        
                            // Short Form is scheduled screen, while BSCA2 is non-schedule. 
                            //1-40
                            string SFCrimeRemarks = "";
                            int SFCrimeValCounter = 0;

                            string SFCrimeVal1 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[35].Value;
                            if (SFCrimeVal1 == "202" || SFCrimeVal1 == "207" || SFCrimeVal1 == "204" || SFCrimeVal1 == "205" || SFCrimeVal1 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCrTuple1 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple1.Item1].FieldValue = SFCrimeCoding(SFCrimeVal1);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[18].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[17].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[3].Value;
                            }
                            if (SFCrimeVal1 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCr2Tuple1 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple1.Item1].FieldValue = "LO";
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[18].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple1.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[17].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[3].Value;
                            }
                            else if (SFCrimeVal1 == "206" || SFCrimeVal1 == "209" || SFCrimeVal1 == "208" || SFCrimeVal1 == "201")
                            {
                                SFCrimeRemarks = SFCrimeRemarks +"COVERAGE: " + SFCrimeCoding(SFCrimeVal1) + " LIMIT: "+ oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[17].Value + Environment.NewLine;
                            }


                            string SFCrimeVal2 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[20].Value;
                            if (SFCrimeVal2 == "202" || SFCrimeVal2 == "207" || SFCrimeVal2 == "204" || SFCrimeVal2 == "205" || SFCrimeVal2 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCrTuple2 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple2.Item1].FieldValue = SFCrimeCoding(SFCrimeVal2);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple2.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[34].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple2.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[28].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple2.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[37].Value;
                            }
                            if (SFCrimeVal2 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCr2Tuple2 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple2.Item1].FieldValue = "LO";
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple2.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[34].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple2.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[28].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple2.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[37].Value;
                            }
                            else if (SFCrimeVal2 == "206" || SFCrimeVal2 == "209" || SFCrimeVal2 == "208" || SFCrimeVal2 == "201")
                            {
                                SFCrimeRemarks = SFCrimeRemarks + "COVERAGE: " + SFCrimeCoding(SFCrimeVal2) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[28].Value + Environment.NewLine;
                            }


                            string SFCrimeVal3 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[15].Value;
                            if (SFCrimeVal3 == "202" || SFCrimeVal3 == "207" || SFCrimeVal3 == "204" || SFCrimeVal3 == "205" || SFCrimeVal3 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCrTuple3 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple3.Item1].FieldValue = SFCrimeCoding(SFCrimeVal3);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple3.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[36].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple3.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[33].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple3.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[2].Value;
                            }
                            if (SFCrimeVal3 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCr2Tuple3 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple3.Item1].FieldValue = "LO";
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple3.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[36].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple3.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[33].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple3.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[2].Value;
                            }
                            else if (SFCrimeVal3 == "206" || SFCrimeVal3 == "209" || SFCrimeVal3 == "208" || SFCrimeVal3 == "201")
                            {
                                SFCrimeRemarks = SFCrimeRemarks + "COVERAGE: " + SFCrimeCoding(SFCrimeVal3) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[33].Value + Environment.NewLine;
                            }


                            string SFCrimeVal4 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[12].Value;
                            if (SFCrimeVal4 == "202" || SFCrimeVal4 == "207" || SFCrimeVal4 == "204" || SFCrimeVal4 == "205" || SFCrimeVal4 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCrTuple4 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple4.Item1].FieldValue = SFCrimeCoding(SFCrimeVal4);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple4.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[10].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple4.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[26].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple4.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[22].Value;
                            }
                            if (SFCrimeVal4 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCr2Tuple4 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple4.Item1].FieldValue = "LO";
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple4.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[10].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple4.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[26].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple4.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[22].Value;
                            }
                            else if (SFCrimeVal4 == "206" || SFCrimeVal4 == "209" || SFCrimeVal4 == "208" || SFCrimeVal4 == "201")
                            {
                                SFCrimeRemarks = SFCrimeRemarks + "COVERAGE: " + SFCrimeCoding(SFCrimeVal4) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[26].Value + Environment.NewLine;
                            }


                            string SFCrimeVal5 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[9].Value;
                            if (SFCrimeVal5 == "202" || SFCrimeVal5 == "207" || SFCrimeVal5 == "204" || SFCrimeVal5 == "205" || SFCrimeVal5 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCrTuple5 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple5.Item1].FieldValue = SFCrimeCoding(SFCrimeVal5);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple5.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[25].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple5.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[7].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple5.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[1].Value;
                            }
                            if (SFCrimeVal5 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCr2Tuple5 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple5.Item1].FieldValue = "LO";
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple5.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[25].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple5.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[7].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple5.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[1].Value;
                            }
                            else if (SFCrimeVal5 == "206" || SFCrimeVal5 == "209" || SFCrimeVal5 == "208" || SFCrimeVal5 == "201")
                            {
                                SFCrimeRemarks = SFCrimeRemarks + "COVERAGE: " + SFCrimeCoding(SFCrimeVal5) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[7].Value + Environment.NewLine;
                            }


                            string SFCrimeVal6 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[6].Value;
                            if (SFCrimeVal6 == "202" || SFCrimeVal6 == "207" || SFCrimeVal6 == "204" || SFCrimeVal6 == "205" || SFCrimeVal6 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCrTuple6 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple6.Item1].FieldValue = SFCrimeCoding(SFCrimeVal6);
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple6.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[24].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple6.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[23].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCrTuple6.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[31].Value;
                            }
                            if (SFCrimeVal6 == "203")
                            {
                                SFCrimeValCounter++;
                                (int, int, int, int) SFCr2Tuple6 = SFmiscCrimeMapping(SFCrimeValCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple6.Item1].FieldValue = "LO";
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple6.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[24].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple6.Item3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[23].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFCr2Tuple6.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[31].Value;
                            }
                            else if (SFCrimeVal6 == "206" || SFCrimeVal6 == "209" || SFCrimeVal6 == "208" || SFCrimeVal6 == "201")
                            {
                                SFCrimeRemarks = SFCrimeRemarks + "COVERAGE: " + SFCrimeCoding(SFCrimeVal6) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[23].Value + Environment.NewLine;
                            }
                            //41
                            nCFR.NonScheduledScreens[1].Fields[207].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[21].Value;
                            //42
                            nCFR.NonScheduledScreens[1].Fields[206].FieldValue = SFCrimeRemarks;
                            //43 N/A
                            //44 - 99
                            int SFContEqpCounter = 0;
                            string SFContEqpRemark = "";

                            string SFConEqp1 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[108].Value;
                            if (SFConEqp1 == "15" || SFConEqp1 == "26" || SFConEqp1 == "18" || SFConEqp1 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple1 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item1].FieldValue = SFCECoding(SFConEqp1);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[175].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[192].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[192].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[177].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[203].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[118].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple1.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[124].Value;
                            }
                            else if (SFConEqp1 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp1) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[118].Value + Environment.NewLine;
                            }


                            string SFConEqp2 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[107].Value;
                            if (SFConEqp2 == "15" || SFConEqp2 == "26" || SFConEqp2 == "18" || SFConEqp2 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple2 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item1].FieldValue = SFCECoding(SFConEqp2);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[106].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[174].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[174].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[157].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[73].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[113].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple2.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[51].Value;
                            }
                            else if (SFConEqp2 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp2) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[113].Value + Environment.NewLine;
                            }


                            string SFConEqp3 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[105].Value;
                            if (SFConEqp3 == "15" || SFConEqp3 == "26" || SFConEqp3 == "18" || SFConEqp3 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple3 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item1].FieldValue = SFCECoding(SFConEqp3);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[87].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[96].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[96].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[117].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[182].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[187].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple3.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[160].Value;
                            }
                            else if (SFConEqp3 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp3) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[187].Value + Environment.NewLine;
                            }


                            string SFConEqp4 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[186].Value;
                            if (SFConEqp4 == "15" || SFConEqp4 == "26" || SFConEqp4 == "18" || SFConEqp4 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple4 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item1].FieldValue = SFCECoding(SFConEqp4);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[169].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[146].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[146].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[196].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[72].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[112].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple4.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[50].Value;
                            }
                            else if (SFConEqp4 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp4) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[112].Value + Environment.NewLine;
                            }


                            string SFConEqp5 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[104].Value;
                            if (SFConEqp5 == "15" || SFConEqp5 == "26" || SFConEqp5 == "18" || SFConEqp5 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple5 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item1].FieldValue = SFCECoding(SFConEqp5);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[86].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[95].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[95].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[116].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[134].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[154].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple5.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[123].Value;
                            }
                            else if (SFConEqp5 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp5) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[154].Value + Environment.NewLine;
                            }


                            string SFConEqp6 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[150].Value;
                            if (SFConEqp6 == "15" || SFConEqp6 == "26" || SFConEqp6 == "18" || SFConEqp6 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple6 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item1].FieldValue = SFCECoding(SFConEqp6);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[141].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[171].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[171].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[156].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[71].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[111].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple6.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[49].Value;
                            }
                            else if (SFConEqp6 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp6) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[111].Value + Environment.NewLine;
                            }


                            string SFConEqp7 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[103].Value;
                            if (SFConEqp7 == "15" || SFConEqp7 == "26" || SFConEqp7 == "18" || SFConEqp7 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple7 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item1].FieldValue = SFCECoding(SFConEqp7);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[85].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[94].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[94].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[115].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[165].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[110].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple7.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[179].Value;
                            }
                            else if (SFConEqp7 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp7) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[110].Value + Environment.NewLine;
                            }


                            string SFConEqp8 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[173].Value;
                            if (SFConEqp8 == "15" || SFConEqp8 == "26" || SFConEqp8 == "18" || SFConEqp8 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple8 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item1].FieldValue = SFCECoding(SFConEqp8);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[197].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[145].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[145].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[176].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[70].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[153].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple8.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[48].Value;
                            }
                            else if (SFConEqp8 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp8) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[153].Value + Environment.NewLine;
                            }


                            string SFConEqp9 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[102].Value;
                            if (SFConEqp9 == "15" || SFConEqp9 == "26" || SFConEqp9 == "18" || SFConEqp9 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple9 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item1].FieldValue = SFCECoding(SFConEqp9);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[84].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[93].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[93].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[114].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[133].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[109].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple9.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[122].Value;
                            }
                            else if (SFConEqp9 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp9) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[109].Value + Environment.NewLine;
                            }


                            string SFConEqp10 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[149].Value;
                            if (SFConEqp10 == "15" || SFConEqp10 == "26" || SFConEqp10 == "18" || SFConEqp10 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple10 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item1].FieldValue = SFCECoding(SFConEqp10);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[140].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[191].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[191].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[155].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[69].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[152].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple10.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[47].Value;
                            }
                            else if (SFConEqp10 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp10) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[152].Value + Environment.NewLine;
                            }


                            string SFConEqp11 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[101].Value;
                            if (SFConEqp11 == "15" || SFConEqp11 == "26" || SFConEqp11 == "18" || SFConEqp11 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple11 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item1].FieldValue = SFCECoding(SFConEqp11);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[83].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[92].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[92].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[78].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[193].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[130].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple11.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[159].Value;
                            }
                            else if (SFConEqp11 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp11) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[130].Value + Environment.NewLine;
                            }


                            string SFConEqp12 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[194].Value;
                            if (SFConEqp12 == "15" || SFConEqp12 == "26" || SFConEqp12 == "18" || SFConEqp12 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple12 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item1].FieldValue = SFCECoding(SFConEqp12);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[168].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[144].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[144].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[137].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[68].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[63].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple12.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[46].Value;
                            }
                            else if (SFConEqp12 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp12) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[63].Value + Environment.NewLine;
                            }


                            string SFConEqp13 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[100].Value;
                            if (SFConEqp13 == "15" || SFConEqp13 == "26" || SFConEqp13 == "18" || SFConEqp13 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple13 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item1].FieldValue = SFCECoding(SFConEqp13);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[82].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[91].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[91].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[77].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[132].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[163].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple13.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[121].Value;
                            }
                            else if (SFConEqp13 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp13) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[163].Value + Environment.NewLine;
                            }


                            string SFConEqp14 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[148].Value;
                            if (SFConEqp14 == "15" || SFConEqp14 == "26" || SFConEqp14 == "18" || SFConEqp14 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple14 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item1].FieldValue = SFCECoding(SFConEqp14);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[139].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[170].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[170].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[190].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[67].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[62].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple14.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[45].Value;
                            }
                            else if (SFConEqp14 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp14) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[62].Value + Environment.NewLine;
                            }


                            string SFConEqp15 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[99].Value;
                            if (SFConEqp15 == "15" || SFConEqp15 == "26" || SFConEqp15 == "18" || SFConEqp15 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple15 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item1].FieldValue = SFCECoding(SFConEqp15);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[81].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[90].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[90].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[76].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[164].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[129].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple15.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[188].Value;
                            }
                            else if (SFConEqp15 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp15) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[129].Value + Environment.NewLine;
                            }


                            string SFConEqp16 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[172].Value;
                            if (SFConEqp16 == "15" || SFConEqp16 == "26" || SFConEqp16 == "18" || SFConEqp16 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple16 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item1].FieldValue = SFCECoding(SFConEqp16);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[183].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[143].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[143].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[136].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[66].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[61].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple16.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[44].Value;
                            }
                            else if (SFConEqp16 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp16) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[61].Value + Environment.NewLine;
                            }


                            string SFConEqp17 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[98].Value;
                            if (SFConEqp17 == "15" || SFConEqp17 == "26" || SFConEqp17 == "18" || SFConEqp17 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple17 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item1].FieldValue = SFCECoding(SFConEqp17);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[80].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[89].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[89].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[75].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[131].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[189].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple17.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[120].Value;
                            }
                            else if (SFConEqp17 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp17) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[189].Value + Environment.NewLine;
                            }


                            string SFConEqp18 = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[147].Value;
                            if (SFConEqp18 == "15" || SFConEqp18 == "26" || SFConEqp18 == "18" || SFConEqp18 == "16")
                            {
                                SFContEqpCounter++;
                                (int, int, int, int, int, int, int) SFConEqTuple18 = SFMCContEquipMap(SFContEqpCounter);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item1].FieldValue = SFCECoding(SFConEqp18);
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item2].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[138].Value;
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[184].Value == "RC")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item3].FieldValue = "RC3";
                                }
                                else if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[184].Value == "ACV")
                                {
                                    nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item3].FieldValue = "ACV";
                                }
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[166].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item5].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[65].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item6].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[60].Value;
                                nCFR.NonScheduledScreens[1].Fields[SFConEqTuple18.Item7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[227].Value;
                            }
                            else if (SFConEqp18 == "17")
                            {
                                SFContEqpRemark = SFContEqpRemark + "COVERAGE: " + SFCECoding(SFConEqp18) + " LIMIT: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[60].Value + Environment.NewLine;
                            }

                            //100
                            nCFR.NonScheduledScreens[1].Fields[99].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[40].Value;
                            //101
                            nCFR.NonScheduledScreens[1].Fields[97].FieldValue = SFContEqpRemark;

                            if (ShortFormUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }

                        }
                        catch (Exception e)
                        {
                            string e12 = oPolId + " | Short Form - Misc Cove failed | " + e;
                            ErrorString = ErrorString + e12 + System.Environment.NewLine;
                            Console.WriteLine(e12);
                        }


                        // Short Form - Liability
                        try
                        {

                        
                            // In Short Form, liability is non-scheduled, while in BSCA2, it is schedule item
                            string LBSFID = nCFR.ScheduledScreens[2].ScheduleID;
                            CBLServiceReference.FieldItems[] LBSFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, LBSFID);
                            nCFR.ScheduledScreens[2].Items.Insert(0, LBSFF[0]); //inserting at 0 since there is only 1
                            //1
                            if (oSupScr.FormDataValue[3].NonScheduledItemsValue[52].Value == "Occurrence")
                            {
                                nCFR.ScheduledScreens[2].Items[0][116].FieldValue = "O";
                            }
                            else if (oSupScr.FormDataValue[3].NonScheduledItemsValue[52].Value == "Claims Made")
                            {
                                nCFR.ScheduledScreens[2].Items[0][116].FieldValue = "C";
                            }
                            //2 N/A
                            List<int> SFCGL = new List<int>() {91, 88, 87, 90, 89, 86, 85, 45, 44 };
                            foreach (int SFIndex in SFCGL)
                            {
                                //2a, 19, 43, 68
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "312")
                                {
                                    nCFR.ScheduledScreens[2].Items[0][30].FieldValue = "PIA";
                                    (int, int, int) LiabCGLTuple1 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][92].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple1.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][41].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple1.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][24].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple1.Item3].Value;
                                }
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "313")
                                {
                                    nCFR.ScheduledScreens[2].Items[0][30].FieldValue = "PI";
                                    (int, int, int) LiabCGLTuple2 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][92].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple2.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][41].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple2.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][24].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple2.Item3].Value;
                                }

                                //3
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "316")
                                {
                                    nCFR.ScheduledScreens[2].Items[0][9].FieldValue = "EL";
                                    (int, int, int) LiabCGLTuple3 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][111].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple3.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][59].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple3.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][37].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple3.Item3].Value;
                                }
                                //4
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "304")
                                {
                                    nCFR.ScheduledScreens[2].Items[0][10].FieldValue = "96";
                                    (int, int, int) LiabCGLTuple4 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][113].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple4.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][61].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple4.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][39].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple4.Item3].Value;
                                }
                                //5
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "314")
                                {
                                    nCFR.ScheduledScreens[2].Items[0][11].FieldValue = "AIL";
                                    (int, int, int) LiabCGLTuple5 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][115].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple5.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][65].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple5.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][63].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple5.Item3].Value;
                                }
                                //20,44,45,69,70
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "310")
                                {
                                    (int, int, int) LiabCGLTuple6 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][90].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple6.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][43].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple6.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][25].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple6.Item3].Value;
                                }
                                //21,46,71
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "318")
                                {
                                    (int, int, int) LiabCGLTuple7 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][88].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple7.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][47].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple7.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][27].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple7.Item3].Value;
                                }
                                //22,47,72
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "308")
                                {
                                    (int, int, int) LiabCGLTuple8 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][86].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple8.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][49].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple8.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][28].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple8.Item3].Value;
                                }
                                //23,48,73
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "302")
                                {
                                    (int, int, int) LiabCGLTuple9 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][82].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple9.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][51].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple9.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][29].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple9.Item3].Value;
                                }
                                //24,49,74
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "303")
                                {
                                    (int, int, int) LiabCGLTuple10 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][78].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple10.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][53].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple10.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][31].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple10.Item3].Value;
                                }
                                //25,50,75
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "305")
                                {
                                    (int, int, int) LiabCGLTuple11 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][74].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple11.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][55].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple11.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][33].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple11.Item3].Value;
                                }
                                //26,51,76
                                if (oSupScr.FormDataValue[3].NonScheduledItemsValue[SFIndex].Value == "325")
                                {
                                    (int, int, int) LiabCGLTuple12 = SFliabMapping(SFIndex);
                                    nCFR.ScheduledScreens[2].Items[0][72].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple12.Item1].Value;
                                    nCFR.ScheduledScreens[2].Items[0][57].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple12.Item2].Value;
                                    nCFR.ScheduledScreens[2].Items[0][35].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[LiabCGLTuple12.Item3].Value;
                                }
                            }
                            //6
                            nCFR.ScheduledScreens[2].Items[0][12].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[29].Value;
                            //7
                            nCFR.ScheduledScreens[2].Items[0][13].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[28].Value;
                            //8
                            nCFR.ScheduledScreens[2].Items[0][14].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[47].Value;
                            //9
                            nCFR.ScheduledScreens[2].Items[0][15].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[46].Value;
                            //10-16 N/A
                            //17
                            nCFR.ScheduledScreens[2].Items[0][3].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[51].Value;
                            //18
                            nCFR.ScheduledScreens[2].Items[0][4].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[55].Value;
                            //30
                            nCFR.ScheduledScreens[2].Items[0][114].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[35].Value;
                            //31
                            nCFR.ScheduledScreens[2].Items[0][112].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[32].Value;
                            //32
                            nCFR.ScheduledScreens[2].Items[0][110].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[71].Value;
                            //33
                            nCFR.ScheduledScreens[2].Items[0][108].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[74].Value;
                            //34-40 N/A
                            //41
                            nCFR.ScheduledScreens[2].Items[0][5].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[50].Value;
                            //42
                            nCFR.ScheduledScreens[2].Items[0][6].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[54].Value;
                            //55
                            nCFR.ScheduledScreens[2].Items[0][67].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[34].Value;
                            //56
                            nCFR.ScheduledScreens[2].Items[0][71].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[31].Value;
                            //57
                            nCFR.ScheduledScreens[2].Items[0][73].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[70].Value;
                            //58
                            nCFR.ScheduledScreens[2].Items[0][75].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[73].Value;
                            //59-65 N/A
                            //66
                            nCFR.ScheduledScreens[2].Items[0][7].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[49].Value;
                            //67
                            nCFR.ScheduledScreens[2].Items[0][8].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[53].Value;
                            //80
                            nCFR.ScheduledScreens[2].Items[0][69].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[33].Value;
                            //81
                            nCFR.ScheduledScreens[2].Items[0][91].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[30].Value;
                            //82
                            nCFR.ScheduledScreens[2].Items[0][93].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[75].Value;
                            //83
                            nCFR.ScheduledScreens[2].Items[0][95].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[72].Value;
                            //84-90 N/A
                            //91
                            nCFR.ScheduledScreens[2].Items[0][32].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[76].Value;
                            //92
                            nCFR.ScheduledScreens[2].Items[0][23].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[48].Value;

                            if (ShortFormUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }

                        }
                        catch (Exception e)
                        {
                            string e13 = oPolId + " | Short Form - Liability failed | " + e;
                            ErrorString = ErrorString + e13 + System.Environment.NewLine;
                            Console.WriteLine(e13);
                        }


                        // Professional / Other Liability
                        try
                        {

                        
                            //1-75 N/A
                            //76
                            if (oSupScr.FormDataValue[3].NonScheduledItemsValue[25].Value != "")
                            {
                                nCFR.NonScheduledScreens[2].Fields[31].FieldValue = "U";
                            }
                            //77 N/A
                            //78
                            nCFR.NonScheduledScreens[2].Fields[29].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[26].Value;
                            //79 N/A
                            //80
                            if (oSupScr.FormDataValue[3].NonScheduledItemsValue[25].Value == "PO")
                            {
                                nCFR.NonScheduledScreens[2].Fields[3].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[18].Value;
                            }
                            //81 N/A
                            //82
                            if (oSupScr.FormDataValue[3].NonScheduledItemsValue[25].Value == "AL")
                            {
                                nCFR.NonScheduledScreens[2].Fields[27].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[18].Value;
                            }
                            //83 N/A
                            //84
                            nCFR.NonScheduledScreens[2].Fields[6].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[21].Value;
                            //85, 86 - N/A
                            //87
                            nCFR.NonScheduledScreens[2].Fields[25].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[17].Value;
                            //88
                            nCFR.NonScheduledScreens[2].Fields[24].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[11].Value;
                            //89
                            nCFR.NonScheduledScreens[2].Fields[23].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[5].Value;
                            //90
                            nCFR.NonScheduledScreens[2].Fields[22].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[16].Value;
                            //91
                            nCFR.NonScheduledScreens[2].Fields[21].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[10].Value;
                            //92
                            nCFR.NonScheduledScreens[2].Fields[20].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[4].Value;
                            //93
                            nCFR.NonScheduledScreens[2].Fields[19].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[15].Value;
                            //94
                            nCFR.NonScheduledScreens[2].Fields[18].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[9].Value;
                            //95
                            nCFR.NonScheduledScreens[2].Fields[17].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[3].Value;
                            //96
                            nCFR.NonScheduledScreens[2].Fields[16].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[14].Value;
                            //97
                            nCFR.NonScheduledScreens[2].Fields[15].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[8].Value;
                            //98
                            nCFR.NonScheduledScreens[2].Fields[14].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[2].Value;
                            //99
                            nCFR.NonScheduledScreens[2].Fields[13].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[13].Value;
                            //100
                            nCFR.NonScheduledScreens[2].Fields[12].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[7].Value;
                            //101
                            nCFR.NonScheduledScreens[2].Fields[11].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[1].Value;
                            //102
                            nCFR.NonScheduledScreens[2].Fields[10].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[12].Value;
                            //103
                            nCFR.NonScheduledScreens[2].Fields[9].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[6].Value;
                            //104
                            nCFR.NonScheduledScreens[2].Fields[8].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[0].Value;
                            //105
                            nCFR.NonScheduledScreens[2].Fields[7].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[23].Value;

                            if (ShortFormUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }

                        }
                        catch (Exception e)
                        {
                            string e14 = oPolId + " | Short Form - Prof/Other Liab failed | " + e;
                            ErrorString = ErrorString + e14 + System.Environment.NewLine;
                            Console.WriteLine(e14);
                        }


                        //Scheduled Equipment
                        try
                        {

                        
                            // Get number of Schedule Equipment schedules from Short Form
                            int SchEqpSFLocationCount = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count;
                            // Read for each schedule
                            for (int i = 0; i < SchEqpSFLocationCount; i++)
                            {
                                // Insert a new schedule item in BSCA2 
                                string SEQID = nCFR.ScheduledScreens[3].ScheduleID;
                                CBLServiceReference.FieldItems[] SQSFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, SEQID);
                                nCFR.ScheduledScreens[3].Items.Insert(i, SQSFF[0]);
                                // Add fields for a given Property Schedule
                                //1 N/A
                                //2
                                nCFR.ScheduledScreens[3].Items[i][15].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[5].Value;
                                //3 N/A
                                //4
                                nCFR.ScheduledScreens[3].Items[i][13].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[6].Value;
                                //5
                                nCFR.ScheduledScreens[3].Items[i][12].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[7].Value;
                                //6
                                nCFR.ScheduledScreens[3].Items[i][11].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[8].Value;
                                //7
                                nCFR.ScheduledScreens[3].Items[i][10].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[9].Value;
                                //8
                                nCFR.ScheduledScreens[3].Items[i][9].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[12].Value;
                                //9
                                if (oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value == "RC")
                                {
                                    nCFR.ScheduledScreens[3].Items[i][8].FieldValue = "RC3";
                                }
                                if (oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value == "ACV")
                                {
                                    nCFR.ScheduledScreens[3].Items[i][8].FieldValue = "ACV";
                                }

                                //10
                                nCFR.ScheduledScreens[3].Items[i][7].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[0].Value;
                                //11
                                nCFR.ScheduledScreens[3].Items[i][6].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[10].Value;
                                //12
                                nCFR.ScheduledScreens[3].Items[i][5].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value;
                                //13
                                nCFR.ScheduledScreens[3].Items[i][4].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[4].Value;
                                //14
                                nCFR.ScheduledScreens[3].Items[i][3].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[3].Value;
                                //15
                                nCFR.ScheduledScreens[3].Items[i][2].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[2].Value;
                                //16
                                nCFR.ScheduledScreens[3].Items[i][1].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[1].Value;
                                //17
                                nCFR.ScheduledScreens[3].Items[i][0].FieldValue = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[15].Value;
                            }
                            if (ShortFormUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }
                        }
                        catch (Exception e)
                        {
                            string e15 = oPolId + " | Short Form - Schd Equip failed | " + e;
                            ErrorString = ErrorString + e15 + System.Environment.NewLine;
                            Console.WriteLine(e15);
                        }


                        if (ShortFormUpdateSwitch == 1)
                        {
                            //EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            CformUpdated = 1;
                            InitialLSFormStatus = true;
                        }

                        //SQL-Commented out
                        //if (SQLShortFormUpdate == 1)
                        //{
                        //    conn.Open();
                        //    using (SqlCommand commandLongFormUpdate = conn.CreateCommand())
                        //    {
                        //        string sqlseven = string.Format("update {0} set CFormUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
                        //        commandLongFormUpdate.CommandText = sqlseven;

                        //        commandLongFormUpdate.Parameters.AddWithValue("@OldPolID", oPolId);
                        //        commandLongFormUpdate.ExecuteNonQuery();
                        //    }
                        //    conn.Close();
                        //}

                    }
                }

                // Cheching if it's BSCA1.0
                else if (oSupScr.Name == "BrokerLink Standard Commercial Application")
                {
                    string BSCALiabOccr = oSupScr.FormDataValue[4].NonScheduledItemsValue[60].Value;
                    string BSCADesc = oSupScr.FormDataValue[0].NonScheduledItemsValue[7].Value;
                    if (BSCADesc.Trim() != "" || BSCALiabOccr.Trim() != "")
                    {
                        Console.WriteLine("*-*-*This is BSCA 1.0*-*-*");
                        // Client Information
                        try
                        {
                                                   
                        
                            //1
                            nCFR.NonScheduledScreens[0].Fields[50].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[9].Value;
                            //2
                            nCFR.NonScheduledScreens[0].Fields[51].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[10].Value;
                            //3
                            nCFR.NonScheduledScreens[0].Fields[49].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[8].Value;
                            //4
                            nCFR.NonScheduledScreens[0].Fields[52].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[7].Value;
                            //5
                            nCFR.NonScheduledScreens[0].Fields[54].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[52].Value;
                            //6 - N/A
                            //7
                            nCFR.NonScheduledScreens[0].Fields[56].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[51].Value;
                            //8 - N/A
                            //9
                            nCFR.NonScheduledScreens[0].Fields[37].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[32].Value;
                            //10
                            nCFR.NonScheduledScreens[0].Fields[36].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[27].Value;
                            //11
                            nCFR.NonScheduledScreens[0].Fields[35].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[37].Value;
                            //12
                            nCFR.NonScheduledScreens[0].Fields[34].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[35].Value;
                            //13
                            nCFR.NonScheduledScreens[0].Fields[33].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[26].Value;
                            //14 - N/A
                            //15
                            nCFR.NonScheduledScreens[0].Fields[38].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[34].Value;
                            //16 - N/A
                            //17
                            nCFR.NonScheduledScreens[0].Fields[47].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[28].Value;
                            //18
                            nCFR.NonScheduledScreens[0].Fields[45].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[24].Value;
                            //19
                            nCFR.NonScheduledScreens[0].Fields[43].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[33].Value;
                            //20
                            nCFR.NonScheduledScreens[0].Fields[41].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[31].Value;
                            //21
                            nCFR.NonScheduledScreens[0].Fields[46].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[36].Value;
                            //22
                            nCFR.NonScheduledScreens[0].Fields[48].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[30].Value;
                            //23
                            nCFR.NonScheduledScreens[0].Fields[44].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[29].Value;
                            //24
                            nCFR.NonScheduledScreens[0].Fields[42].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[22].Value;
                            //25
                            nCFR.NonScheduledScreens[0].Fields[32].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[25].Value;
                            //26
                            nCFR.NonScheduledScreens[0].Fields[31].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[23].Value;
                            //27
                            nCFR.NonScheduledScreens[0].Fields[30].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[21].Value;
                            //28
                            nCFR.NonScheduledScreens[0].Fields[29].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[50].Value;
                            //29
                            nCFR.NonScheduledScreens[0].Fields[28].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[46].Value;
                            //30
                            nCFR.NonScheduledScreens[0].Fields[27].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[41].Value;
                            //31
                            nCFR.NonScheduledScreens[0].Fields[25].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[39].Value;
                            //32
                            nCFR.NonScheduledScreens[0].Fields[23].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[47].Value;
                            //33
                            nCFR.NonScheduledScreens[0].Fields[20].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[49].Value;
                            //34
                            nCFR.NonScheduledScreens[0].Fields[18].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[48].Value;
                            //35
                            nCFR.NonScheduledScreens[0].Fields[26].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[45].Value;
                            //36
                            nCFR.NonScheduledScreens[0].Fields[24].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[38].Value;
                            //37
                            nCFR.NonScheduledScreens[0].Fields[22].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[42].Value;
                            //38
                            nCFR.NonScheduledScreens[0].Fields[19].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[44].Value;
                            //39
                            nCFR.NonScheduledScreens[0].Fields[17].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[43].Value;
                            //40
                            nCFR.NonScheduledScreens[0].Fields[21].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[40].Value;
                            //41
                            nCFR.NonScheduledScreens[0].Fields[16].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[6].Value;
                            //42
                            nCFR.NonScheduledScreens[0].Fields[13].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[2].Value;
                            //43
                            nCFR.NonScheduledScreens[0].Fields[15].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[0].Value;
                            //44
                            nCFR.NonScheduledScreens[0].Fields[14].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[3].Value;
                            //45
                            nCFR.NonScheduledScreens[0].Fields[12].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[1].Value;
                            //46
                            nCFR.NonScheduledScreens[0].Fields[11].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[5].Value;
                            //47
                            nCFR.NonScheduledScreens[0].Fields[10].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[4].Value;
                            //48
                            nCFR.NonScheduledScreens[0].Fields[9].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[17].Value;
                            //49
                            nCFR.NonScheduledScreens[0].Fields[7].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[13].Value;
                            //50
                            nCFR.NonScheduledScreens[0].Fields[5].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[20].Value;
                            //51
                            nCFR.NonScheduledScreens[0].Fields[3].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[14].Value;
                            //52
                            nCFR.NonScheduledScreens[0].Fields[1].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[16].Value;
                            //53
                            nCFR.NonScheduledScreens[0].Fields[8].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[12].Value;
                            //54
                            nCFR.NonScheduledScreens[0].Fields[6].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[18].Value;
                            //55
                            nCFR.NonScheduledScreens[0].Fields[4].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[19].Value;
                            //56
                            nCFR.NonScheduledScreens[0].Fields[2].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[15].Value;
                            //57
                            nCFR.NonScheduledScreens[0].Fields[0].FieldValue = oSupScr.FormDataValue[0].NonScheduledItemsValue[11].Value;

                            if (BSCAUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }

                        }
                        catch (Exception e)
                        {
                            string e16 = oPolId + " | BSCA1 - Client Info failed | " + e;
                            ErrorString = ErrorString + e16 + System.Environment.NewLine;
                            Console.WriteLine(e16);
                        }

                        //COPE
                        try
                        {

                        
                            // Get number of locations from BSCA 1 COPE
                            int B1LocationCount = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count;
                            // Read for each location
                            for (int i = 0; i < B1LocationCount; i++)
                            {
                                // First insert a Location Item
                                string CSID = nCFR.ScheduledScreens[0].ScheduleID;
                                CBLServiceReference.FieldItems[] CFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, CSID);
                                nCFR.ScheduledScreens[0].Items.Insert(i, CFF[0]);
                                // Add fields for a given location
                                //1
                                nCFR.ScheduledScreens[0].Items[i][36].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[7].Value;
                                //2 - N/A
                                //3
                                nCFR.ScheduledScreens[0].Items[i][35].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[5].Value;
                                //4
                                nCFR.ScheduledScreens[0].Items[i][34].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[10].Value;
                                //5
                                nCFR.ScheduledScreens[0].Items[i][33].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[4].Value;
                                //6
                                nCFR.ScheduledScreens[0].Items[i][32].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[11].Value;
                                //7
                                nCFR.ScheduledScreens[0].Items[i][31].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[12].Value;
                                //8
                                nCFR.ScheduledScreens[0].Items[i][30].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value;
                                //9
                                nCFR.ScheduledScreens[0].Items[i][29].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value;
                                //10
                                nCFR.ScheduledScreens[0].Items[i][28].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[6].Value;
                                //11
                                nCFR.ScheduledScreens[0].Items[i][27].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[3].Value;
                                //12
                                nCFR.ScheduledScreens[0].Items[i][26].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[2].Value;
                                //13
                                nCFR.ScheduledScreens[0].Items[i][24].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[34].Value;
                                //14
                                nCFR.ScheduledScreens[0].Items[i][23].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[42].Value;
                                //15
                                nCFR.ScheduledScreens[0].Items[i][22].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[41].Value;
                                //16
                                nCFR.ScheduledScreens[0].Items[i][21].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[39].Value;
                                //17
                                nCFR.ScheduledScreens[0].Items[i][20].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[37].Value;
                                //18
                                nCFR.ScheduledScreens[0].Items[i][19].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[36].Value;
                                //19
                                nCFR.ScheduledScreens[0].Items[i][18].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[35].Value;
                                //20
                                nCFR.ScheduledScreens[0].Items[i][15].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[40].Value;
                                //21
                                nCFR.ScheduledScreens[0].Items[i][12].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[44].Value;
                                //22
                                nCFR.ScheduledScreens[0].Items[i][17].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[48].Value;
                                //23
                                nCFR.ScheduledScreens[0].Items[i][14].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[45].Value;
                                //24
                                nCFR.ScheduledScreens[0].Items[i][11].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[43].Value;
                                //25
                                nCFR.ScheduledScreens[0].Items[i][16].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[47].Value;
                                //26
                                nCFR.ScheduledScreens[0].Items[i][13].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[46].Value;
                                //27
                                nCFR.ScheduledScreens[0].Items[i][10].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[33].Value;
                                //28
                                nCFR.ScheduledScreens[0].Items[i][9].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[38].Value;
                                //29
                                nCFR.ScheduledScreens[0].Items[i][8].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[32].Value;
                                //30
                                nCFR.ScheduledScreens[0].Items[i][4].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[27].Value;
                                //31
                                nCFR.ScheduledScreens[0].Items[i][7].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[31].Value;
                                //32
                                nCFR.ScheduledScreens[0].Items[i][3].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[26].Value;
                                //33
                                nCFR.ScheduledScreens[0].Items[i][6].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[30].Value;
                                //34
                                nCFR.ScheduledScreens[0].Items[i][2].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[25].Value;
                                //35
                                nCFR.ScheduledScreens[0].Items[i][5].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[29].Value;
                                //36
                                nCFR.ScheduledScreens[0].Items[i][1].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[24].Value;
                                //37
                                nCFR.ScheduledScreens[0].Items[i][0].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[28].Value;
                                //38
                                nCFR.ScheduledScreens[0].Items[i][39].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[23].Value;
                                //39
                                nCFR.ScheduledScreens[0].Items[i][45].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[15].Value;
                                //40
                                nCFR.ScheduledScreens[0].Items[i][38].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[21].Value;
                                //41
                                nCFR.ScheduledScreens[0].Items[i][41].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[17].Value;
                                //42
                                nCFR.ScheduledScreens[0].Items[i][37].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[16].Value;
                                //43
                                nCFR.ScheduledScreens[0].Items[i][44].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[22].Value;
                                //44
                                nCFR.ScheduledScreens[0].Items[i][40].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[18].Value;
                                //45
                                nCFR.ScheduledScreens[0].Items[i][43].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[20].Value;
                                //46
                                nCFR.ScheduledScreens[0].Items[i][42].FieldValue = oSupScr.FormDataValue[1].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[19].Value;
                            } // COPE Location schedule ends

                            if (BSCAUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }
                        }
                        catch (Exception e)
                        {
                            string e17 = oPolId + " | BSCA1 - COPE failed | " + e;
                            ErrorString = ErrorString + e17 + System.Environment.NewLine;
                            Console.WriteLine(e17);
                        }

                        //Property
                        try
                        {

                        
                            // Get number of Property schedules from BSCA 1 COPE
                            int PropBLocationCount = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count;
                            // Read for each schedule
                            for (int i = 0; i < PropBLocationCount; i++)
                            {
                                // First insert a Property Item
                                string BPSID = nCFR.ScheduledScreens[1].ScheduleID;
                                CBLServiceReference.FieldItems[] BPFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, BPSID);
                                nCFR.ScheduledScreens[1].Items.Insert(i, BPFF[0]);
                                // Add fields for a given Property Schedule
                                //1
                                nCFR.ScheduledScreens[1].Items[i][200].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[168].Value;
                                //2
                                nCFR.ScheduledScreens[1].Items[i][199].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[157].Value;
                                //3
                                nCFR.ScheduledScreens[1].Items[i][198].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[186].Value;
                                //4
                                nCFR.ScheduledScreens[1].Items[i][197].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[122].Value;
                                //5
                                nCFR.ScheduledScreens[1].Items[i][196].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[124].Value;
                                //6
                                nCFR.ScheduledScreens[1].Items[i][195].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[104].Value;
                                //7
                                nCFR.ScheduledScreens[1].Items[i][194].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[175].Value;
                                //8
                                nCFR.ScheduledScreens[1].Items[i][193].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[123].Value;
                                //9
                                nCFR.ScheduledScreens[1].Items[i][192].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[179].Value;
                                //10
                                nCFR.ScheduledScreens[1].Items[i][191].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[135].Value;
                                //11
                                nCFR.ScheduledScreens[1].Items[i][190].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[156].Value;
                                //12
                                nCFR.ScheduledScreens[1].Items[i][189].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[141].Value;
                                //13
                                nCFR.ScheduledScreens[1].Items[i][188].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[163].Value;
                                //14
                                nCFR.ScheduledScreens[1].Items[i][187].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[188].Value;
                                //15
                                nCFR.ScheduledScreens[1].Items[i][186].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[170].Value;
                                //16
                                nCFR.ScheduledScreens[1].Items[i][185].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[120].Value;
                                //17
                                nCFR.ScheduledScreens[1].Items[i][184].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[134].Value;
                                //18
                                nCFR.ScheduledScreens[1].Items[i][183].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[155].Value;
                                //19
                                nCFR.ScheduledScreens[1].Items[i][182].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[167].Value;
                                //20
                                nCFR.ScheduledScreens[1].Items[i][181].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[113].Value;
                                //21 - N/A
                                //22
                                nCFR.ScheduledScreens[1].Items[i][180].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[77].Value;
                                //23
                                nCFR.ScheduledScreens[1].Items[i][179].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[93].Value;
                                //24
                                nCFR.ScheduledScreens[1].Items[i][178].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[87].Value;
                                //25
                                nCFR.ScheduledScreens[1].Items[i][177].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[59].Value;
                                //26
                                nCFR.ScheduledScreens[1].Items[i][176].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[85].Value;
                                //27 
                                nCFR.ScheduledScreens[1].Items[i][158].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[164].Value;
                                //28
                                nCFR.ScheduledScreens[1].Items[i][157].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[143].Value;
                                //29
                                nCFR.ScheduledScreens[1].Items[i][156].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[119].Value;
                                //30
                                nCFR.ScheduledScreens[1].Items[i][155].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[178].Value;
                                //31
                                nCFR.ScheduledScreens[1].Items[i][154].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[184].Value;
                                //32
                                nCFR.ScheduledScreens[1].Items[i][153].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[103].Value;
                                //33
                                nCFR.ScheduledScreens[1].Items[i][152].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[181].Value;
                                //34
                                nCFR.ScheduledScreens[1].Items[i][151].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[133].Value;
                                //35
                                nCFR.ScheduledScreens[1].Items[i][150].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[154].Value;
                                //36
                                nCFR.ScheduledScreens[1].Items[i][149].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[174].Value;
                                //37
                                nCFR.ScheduledScreens[1].Items[i][148].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[192].Value;
                                //38
                                nCFR.ScheduledScreens[1].Items[i][147].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[118].Value;
                                //39
                                nCFR.ScheduledScreens[1].Items[i][146].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[153].Value;
                                //40
                                nCFR.ScheduledScreens[1].Items[i][145].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[140].Value;
                                //41
                                nCFR.ScheduledScreens[1].Items[i][144].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[117].Value;
                                //42
                                nCFR.ScheduledScreens[1].Items[i][143].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[162].Value;
                                //43
                                nCFR.ScheduledScreens[1].Items[i][142].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[191].Value;
                                //44
                                nCFR.ScheduledScreens[1].Items[i][141].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[182].Value;
                                //44A
                                nCFR.ScheduledScreens[1].Items[i][140].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[116].Value;
                                //44B
                                nCFR.ScheduledScreens[1].Items[i][139].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[150].Value;
                                //44c - N/A
                                //45
                                nCFR.ScheduledScreens[1].Items[i][137].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[54].Value;
                                //46
                                nCFR.ScheduledScreens[1].Items[i][136].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[58].Value;
                                //47
                                nCFR.ScheduledScreens[1].Items[i][135].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[67].Value;
                                //48
                                nCFR.ScheduledScreens[1].Items[i][134].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[78].Value;
                                //49
                                nCFR.ScheduledScreens[1].Items[i][133].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[57].Value;
                                //50
                                nCFR.ScheduledScreens[1].Items[i][132].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[84].Value;
                                //51
                                nCFR.ScheduledScreens[1].Items[i][131].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[64].Value;
                                //52
                                nCFR.ScheduledScreens[1].Items[i][130].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[76].Value;
                                //53 - N/A
                                //54
                                nCFR.ScheduledScreens[1].Items[i][129].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[72].Value;
                                //55 - N/A
                                //56
                                nCFR.ScheduledScreens[1].Items[i][128].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[61].Value;
                                //57
                                nCFR.ScheduledScreens[1].Items[i][127].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[79].Value;
                                //58
                                nCFR.ScheduledScreens[1].Items[i][126].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[55].Value;
                                //59
                                nCFR.ScheduledScreens[1].Items[i][125].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[69].Value;
                                //60
                                nCFR.ScheduledScreens[1].Items[i][124].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[81].Value;
                                //61
                                nCFR.ScheduledScreens[1].Items[i][123].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[73].Value;
                                //62
                                nCFR.ScheduledScreens[1].Items[i][122].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[66].Value;
                                //63
                                nCFR.ScheduledScreens[1].Items[i][121].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[70].Value;
                                //64
                                nCFR.ScheduledScreens[1].Items[i][120].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[82].Value;
                                //64A
                                nCFR.ScheduledScreens[1].Items[i][119].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[63].Value;
                                //64B
                                nCFR.ScheduledScreens[1].Items[i][118].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[75].Value;
                                //64C - N/A
                                //65
                                nCFR.ScheduledScreens[1].Items[i][116].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[106].Value;
                                //66
                                nCFR.ScheduledScreens[1].Items[i][115].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[171].Value;
                                //67
                                nCFR.ScheduledScreens[1].Items[i][114].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[126].Value;
                                //68
                                nCFR.ScheduledScreens[1].Items[i][113].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[177].Value;
                                //69
                                nCFR.ScheduledScreens[1].Items[i][112].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[189].Value;
                                //70
                                nCFR.ScheduledScreens[1].Items[i][111].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[101].Value;
                                //71
                                nCFR.ScheduledScreens[1].Items[i][110].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[125].Value;
                                //72
                                nCFR.ScheduledScreens[1].Items[i][109].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[137].Value;
                                //73
                                nCFR.ScheduledScreens[1].Items[i][108].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[158].Value;
                                //74
                                nCFR.ScheduledScreens[1].Items[i][107].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[136].Value;
                                //75
                                nCFR.ScheduledScreens[1].Items[i][106].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[131].Value;
                                //76
                                nCFR.ScheduledScreens[1].Items[i][105].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[152].Value;
                                //77
                                nCFR.ScheduledScreens[1].Items[i][104].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[115].Value;
                                //78
                                nCFR.ScheduledScreens[1].Items[i][103].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[193].Value;
                                //79
                                nCFR.ScheduledScreens[1].Items[i][102].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[173].Value;
                                //80
                                nCFR.ScheduledScreens[1].Items[i][101].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[183].Value;
                                //81
                                nCFR.ScheduledScreens[1].Items[i][100].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[114].Value;
                                //82
                                nCFR.ScheduledScreens[1].Items[i][99].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[130].Value;
                                //82A
                                nCFR.ScheduledScreens[1].Items[i][98].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[151].Value;
                                //82B
                                nCFR.ScheduledScreens[1].Items[i][97].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[190].Value;
                                //82C - N/A
                                //83
                                nCFR.ScheduledScreens[1].Items[i][72].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[129].Value;
                                //84
                                nCFR.ScheduledScreens[1].Items[i][70].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[139].Value;
                                //85
                                nCFR.ScheduledScreens[1].Items[i][69].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[112].Value;
                                //86
                                nCFR.ScheduledScreens[1].Items[i][66].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[128].Value;
                                //87
                                nCFR.ScheduledScreens[1].Items[i][43].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[149].Value;
                                //88
                                nCFR.ScheduledScreens[1].Items[i][64].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[99].Value;
                                //89
                                nCFR.ScheduledScreens[1].Items[i][62].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[169].Value;
                                //90
                                nCFR.ScheduledScreens[1].Items[i][60].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[160].Value;
                                //91
                                nCFR.ScheduledScreens[1].Items[i][58].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[187].Value;
                                //92
                                nCFR.ScheduledScreens[1].Items[i][56].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[148].Value;
                                //93
                                nCFR.ScheduledScreens[1].Items[i][54].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[165].Value;
                                //94
                                nCFR.ScheduledScreens[1].Items[i][53].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[111].Value;
                                //95
                                nCFR.ScheduledScreens[1].Items[i][52].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[172].Value;
                                //96
                                nCFR.ScheduledScreens[1].Items[i][51].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[144].Value;
                                //97
                                nCFR.ScheduledScreens[1].Items[i][50].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[110].Value;
                                //98
                                nCFR.ScheduledScreens[1].Items[i][49].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[127].Value;
                                //99
                                nCFR.ScheduledScreens[1].Items[i][48].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[147].Value;
                                //100
                                nCFR.ScheduledScreens[1].Items[i][47].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[138].Value;
                                //100A
                                nCFR.ScheduledScreens[1].Items[i][46].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[109].Value;
                                //100B
                                nCFR.ScheduledScreens[1].Items[i][45].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[159].Value;
                                //100C - N/A
                                //101
                                nCFR.ScheduledScreens[1].Items[i][42].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[108].Value;
                                //102
                                nCFR.ScheduledScreens[1].Items[i][201].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[142].Value;
                                //103
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[17].Value == "PALS")
                                {
                                    nCFR.ScheduledScreens[1].Items[i][252].FieldValue = "ALS";
                                }
                                else
                                {
                                    nCFR.ScheduledScreens[1].Items[i][252].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[17].Value;
                                }
                                //104
                                nCFR.ScheduledScreens[1].Items[i][251].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value;
                                //105
                                nCFR.ScheduledScreens[1].Items[i][238].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[31].Value;
                                //106
                                nCFR.ScheduledScreens[1].Items[i][241].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[23].Value;
                                //107
                                nCFR.ScheduledScreens[1].Items[i][249].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[25].Value;
                                //108
                                nCFR.ScheduledScreens[1].Items[i][239].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[18].Value;
                                //109
                                nCFR.ScheduledScreens[1].Items[i][228].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[22].Value;
                                //110
                                nCFR.ScheduledScreens[1].Items[i][220].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[24].Value;
                                //111
                                nCFR.ScheduledScreens[1].Items[i][234].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value;
                                //112
                                nCFR.ScheduledScreens[1].Items[i][226].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[16].Value;
                                //113
                                nCFR.ScheduledScreens[1].Items[i][224].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[27].Value;
                                //114
                                nCFR.ScheduledScreens[1].Items[i][221].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[12].Value;
                                //115
                                nCFR.ScheduledScreens[1].Items[i][240].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[19].Value;
                                //116
                                nCFR.ScheduledScreens[1].Items[i][246].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[30].Value;
                                //117
                                nCFR.ScheduledScreens[1].Items[i][217].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[20].Value;
                                //118
                                nCFR.ScheduledScreens[1].Items[i][250].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[26].Value;
                                //119
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[21].Value == "PALS")
                                {
                                    nCFR.ScheduledScreens[1].Items[i][232].FieldValue = "ALS";
                                }
                                else
                                {
                                    nCFR.ScheduledScreens[1].Items[i][232].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[21].Value;
                                }
                                //120
                                if (oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[28].Value == "PALS")
                                {
                                    nCFR.ScheduledScreens[1].Items[i][236].FieldValue = "ALS";
                                }
                                else
                                {
                                    nCFR.ScheduledScreens[1].Items[i][236].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[28].Value;
                                }
                                //121
                                nCFR.ScheduledScreens[1].Items[i][222].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[11].Value;
                                //122
                                nCFR.ScheduledScreens[1].Items[i][242].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[9].Value;
                                //123
                                nCFR.ScheduledScreens[1].Items[i][244].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[7].Value;
                                //124
                                nCFR.ScheduledScreens[1].Items[i][218].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[5].Value;
                                //125
                                nCFR.ScheduledScreens[1].Items[i][248].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[3].Value;
                                //126
                                nCFR.ScheduledScreens[1].Items[i][245].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[1].Value;
                                //127
                                nCFR.ScheduledScreens[1].Items[i][231].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[10].Value;
                                //128
                                nCFR.ScheduledScreens[1].Items[i][235].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[8].Value;
                                //129
                                nCFR.ScheduledScreens[1].Items[i][227].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[6].Value;
                                //130
                                nCFR.ScheduledScreens[1].Items[i][230].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[4].Value;
                                //131
                                nCFR.ScheduledScreens[1].Items[i][233].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[2].Value;
                                //132
                                nCFR.ScheduledScreens[1].Items[i][229].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[0].Value;
                                //133-144 - N/A
                                //145
                                nCFR.ScheduledScreens[1].Items[i][223].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[29].Value;
                                //146
                                nCFR.ScheduledScreens[1].Items[i][219].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[15].Value;
                                //147
                                nCFR.ScheduledScreens[1].Items[i][38].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[53].Value;
                                //148-N/A
                                //149
                                nCFR.ScheduledScreens[1].Items[i][26].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[35].Value;
                                //150
                                nCFR.ScheduledScreens[1].Items[i][33].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[52].Value;
                                //151
                                nCFR.ScheduledScreens[1].Items[i][25].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[51].Value;
                                //152
                                nCFR.ScheduledScreens[1].Items[i][37].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[50].Value;
                                //153
                                nCFR.ScheduledScreens[1].Items[i][29].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[49].Value;
                                //154-N/A
                                //155
                                nCFR.ScheduledScreens[1].Items[i][16].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[34].Value;
                                //156
                                nCFR.ScheduledScreens[1].Items[i][35].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[48].Value;
                                //157
                                nCFR.ScheduledScreens[1].Items[i][15].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[47].Value;
                                //158
                                nCFR.ScheduledScreens[1].Items[i][27].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[46].Value;
                                //159-170 N/A
                                //171
                                nCFR.ScheduledScreens[1].Items[i][13].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[45].Value;
                                //172-N/A
                                //173
                                nCFR.ScheduledScreens[1].Items[i][4].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[33].Value;
                                //174
                                nCFR.ScheduledScreens[1].Items[i][24].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[43].Value;
                                //175
                                nCFR.ScheduledScreens[1].Items[i][32].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[41].Value;
                                //176
                                nCFR.ScheduledScreens[1].Items[i][7].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[39].Value;
                                //177
                                nCFR.ScheduledScreens[1].Items[i][19].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[44].Value;
                                //178-N/A
                                //179
                                nCFR.ScheduledScreens[1].Items[i][3].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[32].Value;
                                //180
                                nCFR.ScheduledScreens[1].Items[i][9].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[42].Value;
                                //181
                                nCFR.ScheduledScreens[1].Items[i][8].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[40].Value;
                                //182
                                nCFR.ScheduledScreens[1].Items[i][23].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[38].Value;
                                //183
                                nCFR.ScheduledScreens[1].Items[i][31].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[37].Value;
                                //184 - N/A
                                //185
                                nCFR.ScheduledScreens[1].Items[i][22].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[36].Value;
                                //186
                                nCFR.ScheduledScreens[1].Items[i][254].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[194].Value;
                                //187 - N / A
                                //188
                                nCFR.ScheduledScreens[1].Items[i][95].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[105].Value;
                                //189
                                nCFR.ScheduledScreens[1].Items[i][94].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[180].Value;
                                //190
                                nCFR.ScheduledScreens[1].Items[i][93].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[176].Value;
                                //191
                                nCFR.ScheduledScreens[1].Items[i][41].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[88].Value;
                                //192
                                nCFR.ScheduledScreens[1].Items[i][90].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[90].Value;
                                //193
                                nCFR.ScheduledScreens[1].Items[i][91].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[92].Value;
                                //194
                                nCFR.ScheduledScreens[1].Items[i][90].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[94].Value;
                                //195
                                nCFR.ScheduledScreens[1].Items[i][89].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[96].Value;
                                //196
                                nCFR.ScheduledScreens[1].Items[i][88].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[146].Value;
                                //197
                                nCFR.ScheduledScreens[1].Items[i][175].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[102].Value;
                                //197
                                nCFR.ScheduledScreens[1].Items[i][87].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[107].Value;
                                //198
                                nCFR.ScheduledScreens[1].Items[i][174].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[95].Value;
                                //198
                                nCFR.ScheduledScreens[1].Items[i][86].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[161].Value;
                                //199
                                nCFR.ScheduledScreens[1].Items[i][173].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[71].Value;
                                //200
                                nCFR.ScheduledScreens[1].Items[i][172].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[65].Value;
                                //201
                                nCFR.ScheduledScreens[1].Items[i][171].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[62].Value;
                                //202
                                nCFR.ScheduledScreens[1].Items[i][170].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[97].Value;
                                //203
                                nCFR.ScheduledScreens[1].Items[i][169].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[89].Value;
                                //204
                                nCFR.ScheduledScreens[1].Items[i][168].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[86].Value;
                                //205
                                nCFR.ScheduledScreens[1].Items[i][167].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[98].Value;
                                //206
                                nCFR.ScheduledScreens[1].Items[i][166].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[68].Value;
                                //207
                                nCFR.ScheduledScreens[1].Items[i][165].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[56].Value;
                                //208
                                nCFR.ScheduledScreens[1].Items[i][164].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[80].Value;
                                //209
                                nCFR.ScheduledScreens[1].Items[i][163].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[100].Value;
                                //210
                                nCFR.ScheduledScreens[1].Items[i][162].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[91].Value;
                                //211
                                nCFR.ScheduledScreens[1].Items[i][161].FieldValue = oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[83].Value;
                                //212 - N/A

                            } // Property schedule ends here

                            if (BSCAUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }
                        }
                        catch (Exception e)
                        {
                            string e18 = oPolId + " | BSCA1 - Property failed | " + e;
                            ErrorString = ErrorString + e18 + System.Environment.NewLine;
                            Console.WriteLine(e18);
                        }



                        // Misc. Coverage
                        try
                        {

                        
                            //1
                            nCFR.NonScheduledScreens[1].Fields[205].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[139].Value;
                            //2
                            nCFR.NonScheduledScreens[1].Fields[204].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[153].Value;
                            //3
                            nCFR.NonScheduledScreens[1].Fields[203].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[137].Value;
                            //4
                            nCFR.NonScheduledScreens[1].Fields[202].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[138].Value;
                            //5
                            nCFR.NonScheduledScreens[1].Fields[201].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[136].Value;
                            //6
                            nCFR.NonScheduledScreens[1].Fields[200].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[165].Value;
                            //7 - N/A
                            //8
                            nCFR.NonScheduledScreens[1].Fields[199].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[162].Value;
                            //9
                            nCFR.NonScheduledScreens[1].Fields[198].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[154].Value;
                            //10 - N/A
                            //11
                            nCFR.NonScheduledScreens[1].Fields[197].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[158].Value;
                            //12
                            nCFR.NonScheduledScreens[1].Fields[196].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[148].Value;
                            //13
                            nCFR.NonScheduledScreens[1].Fields[195].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[156].Value;
                            //14
                            nCFR.NonScheduledScreens[1].Fields[194].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[145].Value;
                            //15
                            nCFR.NonScheduledScreens[1].Fields[193].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[143].Value;
                            //16
                            nCFR.NonScheduledScreens[1].Fields[192].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[164].Value;
                            //17 N/A
                            //18
                            nCFR.NonScheduledScreens[1].Fields[191].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[157].Value;
                            //19
                            nCFR.NonScheduledScreens[1].Fields[190].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[150].Value;
                            //20 N/A
                            //21
                            nCFR.NonScheduledScreens[1].Fields[189].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[147].Value;
                            //22
                            nCFR.NonScheduledScreens[1].Fields[188].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[149].Value;
                            //23
                            nCFR.NonScheduledScreens[1].Fields[187].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[169].Value;
                            //24
                            nCFR.NonScheduledScreens[1].Fields[186].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[159].Value;
                            //25
                            nCFR.NonScheduledScreens[1].Fields[185].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[166].Value;
                            //26
                            nCFR.NonScheduledScreens[1].Fields[184].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[146].Value;
                            //27 N/A
                            //28
                            nCFR.NonScheduledScreens[1].Fields[183].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[141].Value;
                            //29
                            nCFR.NonScheduledScreens[1].Fields[182].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[144].Value;
                            //30 N/A
                            //31
                            nCFR.NonScheduledScreens[1].Fields[215].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[163].Value;
                            //32
                            nCFR.NonScheduledScreens[1].Fields[214].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[140].Value;
                            //33
                            nCFR.NonScheduledScreens[1].Fields[213].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[155].Value;
                            //34
                            nCFR.NonScheduledScreens[1].Fields[212].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[152].Value;
                            //35
                            nCFR.NonScheduledScreens[1].Fields[211].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[161].Value;
                            //36
                            nCFR.NonScheduledScreens[1].Fields[210].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[151].Value;
                            //37 - N/A
                            //38
                            nCFR.NonScheduledScreens[1].Fields[209].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[168].Value;
                            //39
                            nCFR.NonScheduledScreens[1].Fields[208].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[160].Value;
                            //40 N/A
                            //41
                            nCFR.NonScheduledScreens[1].Fields[207].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[142].Value;
                            //42
                            nCFR.NonScheduledScreens[1].Fields[206].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[167].Value;
                            //43
                            nCFR.NonScheduledScreens[1].Fields[163].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[78].Value;
                            //44
                            nCFR.NonScheduledScreens[1].Fields[162].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[98].Value;
                            //45
                            nCFR.NonScheduledScreens[1].Fields[161].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[97].Value;
                            //46
                            nCFR.NonScheduledScreens[1].Fields[160].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[96].Value;
                            //47
                            nCFR.NonScheduledScreens[1].Fields[159].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[95].Value;
                            //48
                            nCFR.NonScheduledScreens[1].Fields[158].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[94].Value;
                            //49
                            nCFR.NonScheduledScreens[1].Fields[157].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[93].Value;
                            //50
                            nCFR.NonScheduledScreens[1].Fields[156].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[92].Value;
                            //51
                            nCFR.NonScheduledScreens[1].Fields[155].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[91].Value;
                            //52
                            nCFR.NonScheduledScreens[1].Fields[154].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[134].Value;
                            //53
                            nCFR.NonScheduledScreens[1].Fields[153].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[133].Value;
                            //54
                            nCFR.NonScheduledScreens[1].Fields[152].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[132].Value;
                            //55
                            nCFR.NonScheduledScreens[1].Fields[151].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[131].Value;
                            //56
                            nCFR.NonScheduledScreens[1].Fields[150].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[130].Value;
                            //57
                            nCFR.NonScheduledScreens[1].Fields[149].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[129].Value;
                            //58
                            nCFR.NonScheduledScreens[1].Fields[148].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[80].Value;
                            //59
                            nCFR.NonScheduledScreens[1].Fields[147].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[79].Value;
                            //60
                            nCFR.NonScheduledScreens[1].Fields[146].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[135].Value;
                            //61
                            nCFR.NonScheduledScreens[1].Fields[145].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[128].Value;
                            //62
                            nCFR.NonScheduledScreens[1].Fields[144].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[127].Value;
                            //63
                            nCFR.NonScheduledScreens[1].Fields[143].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[126].Value;
                            //64
                            nCFR.NonScheduledScreens[1].Fields[142].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[125].Value;
                            //65
                            nCFR.NonScheduledScreens[1].Fields[141].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[124].Value;
                            //66
                            nCFR.NonScheduledScreens[1].Fields[140].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[82].Value;
                            //67
                            nCFR.NonScheduledScreens[1].Fields[139].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[81].Value;
                            //68
                            nCFR.NonScheduledScreens[1].Fields[138].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[123].Value;
                            //69
                            nCFR.NonScheduledScreens[1].Fields[137].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[122].Value;
                            //70
                            nCFR.NonScheduledScreens[1].Fields[136].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[121].Value;
                            //71
                            nCFR.NonScheduledScreens[1].Fields[135].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[120].Value;
                            //72
                            nCFR.NonScheduledScreens[1].Fields[134].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[119].Value;
                            //73
                            nCFR.NonScheduledScreens[1].Fields[133].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[118].Value;
                            //74
                            nCFR.NonScheduledScreens[1].Fields[132].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[90].Value;
                            //75
                            nCFR.NonScheduledScreens[1].Fields[131].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[89].Value;
                            //76
                            nCFR.NonScheduledScreens[1].Fields[130].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[117].Value;
                            //77
                            nCFR.NonScheduledScreens[1].Fields[129].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[116].Value;
                            //78
                            nCFR.NonScheduledScreens[1].Fields[128].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[115].Value;
                            //79
                            nCFR.NonScheduledScreens[1].Fields[127].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[114].Value;
                            //80
                            nCFR.NonScheduledScreens[1].Fields[126].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[113].Value;
                            //81
                            nCFR.NonScheduledScreens[1].Fields[125].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[112].Value;
                            //82
                            nCFR.NonScheduledScreens[1].Fields[124].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[84].Value;
                            //83
                            nCFR.NonScheduledScreens[1].Fields[112].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[83].Value;
                            //84
                            nCFR.NonScheduledScreens[1].Fields[123].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[111].Value;
                            //85
                            nCFR.NonScheduledScreens[1].Fields[122].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[110].Value;
                            //86
                            nCFR.NonScheduledScreens[1].Fields[121].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[109].Value;
                            //87
                            nCFR.NonScheduledScreens[1].Fields[120].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[108].Value;
                            //88
                            nCFR.NonScheduledScreens[1].Fields[119].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[107].Value;
                            //89
                            nCFR.NonScheduledScreens[1].Fields[118].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[106].Value;
                            //90
                            nCFR.NonScheduledScreens[1].Fields[117].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[88].Value;
                            //91
                            nCFR.NonScheduledScreens[1].Fields[116].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[87].Value;
                            //92
                            nCFR.NonScheduledScreens[1].Fields[115].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[105].Value;
                            //93
                            nCFR.NonScheduledScreens[1].Fields[113].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[104].Value;
                            //94
                            nCFR.NonScheduledScreens[1].Fields[111].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[103].Value;
                            //95
                            nCFR.NonScheduledScreens[1].Fields[109].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[102].Value;
                            //96
                            nCFR.NonScheduledScreens[1].Fields[107].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[101].Value;
                            //97
                            nCFR.NonScheduledScreens[1].Fields[105].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[100].Value;
                            //98
                            nCFR.NonScheduledScreens[1].Fields[103].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[86].Value;
                            //99
                            nCFR.NonScheduledScreens[1].Fields[101].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[85].Value;
                            //100
                            nCFR.NonScheduledScreens[1].Fields[99].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[99].Value;
                            //101
                            nCFR.NonScheduledScreens[1].Fields[97].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[77].Value;
                            //102
                            if (oSupScr.FormDataValue[3].NonScheduledItemsValue[69].Value == "FP" || oSupScr.FormDataValue[3].NonScheduledItemsValue[67].Value == "FP")
                            {
                                nCFR.NonScheduledScreens[1].Fields[92].FieldValue = "F";
                            }
                            if (oSupScr.FormDataValue[3].NonScheduledItemsValue[69].Value == "TP" || oSupScr.FormDataValue[3].NonScheduledItemsValue[67].Value == "TP")
                            {
                                nCFR.NonScheduledScreens[1].Fields[92].FieldValue = "FT";
                            }
                            //103
                            nCFR.NonScheduledScreens[1].Fields[96].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[76].Value;
                            //104-107 - N/A
                            //108
                            nCFR.NonScheduledScreens[1].Fields[95].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[74].Value;
                            //109-112 - N/A
                            //113
                            nCFR.NonScheduledScreens[1].Fields[94].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[72].Value;
                            //114 - 117 - N/A
                            //118
                            nCFR.NonScheduledScreens[1].Fields[91].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[70].Value;
                            //119
                            nCFR.NonScheduledScreens[1].Fields[76].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[68].Value;
                            //120
                            nCFR.NonScheduledScreens[1].Fields[73].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[0].Value;
                            //121
                            nCFR.NonScheduledScreens[1].Fields[72].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[39].Value;
                            //122
                            nCFR.NonScheduledScreens[1].Fields[14].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[3].Value;
                            //123
                            nCFR.NonScheduledScreens[1].Fields[15].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[2].Value;
                            //124
                            nCFR.NonScheduledScreens[1].Fields[13].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[13].Value;
                            //125
                            nCFR.NonScheduledScreens[1].Fields[12].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[11].Value;
                            //126
                            nCFR.NonScheduledScreens[1].Fields[11].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[9].Value;
                            //127
                            nCFR.NonScheduledScreens[1].Fields[10].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[7].Value;
                            //128
                            nCFR.NonScheduledScreens[1].Fields[8].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[5].Value;
                            //129
                            nCFR.NonScheduledScreens[1].Fields[71].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[6].Value;
                            //130
                            nCFR.NonScheduledScreens[1].Fields[70].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[8].Value;
                            //131
                            nCFR.NonScheduledScreens[1].Fields[69].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[24].Value;
                            //132
                            nCFR.NonScheduledScreens[1].Fields[18].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[29].Value;
                            //133
                            nCFR.NonScheduledScreens[1].Fields[68].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[35].Value;
                            //134
                            nCFR.NonScheduledScreens[1].Fields[67].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[42].Value;
                            //135
                            nCFR.NonScheduledScreens[1].Fields[66].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[48].Value;
                            //136
                            nCFR.NonScheduledScreens[1].Fields[65].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[18].Value;
                            //137
                            nCFR.NonScheduledScreens[1].Fields[64].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[23].Value;
                            //138
                            nCFR.NonScheduledScreens[1].Fields[63].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[54].Value;
                            //139
                            nCFR.NonScheduledScreens[1].Fields[62].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[61].Value;
                            //140
                            nCFR.NonScheduledScreens[1].Fields[61].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[64].Value;
                            //141
                            nCFR.NonScheduledScreens[1].Fields[60].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[56].Value;
                            //142
                            nCFR.NonScheduledScreens[1].Fields[59].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[50].Value;
                            //143
                            nCFR.NonScheduledScreens[1].Fields[58].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[15].Value;
                            //144
                            nCFR.NonScheduledScreens[1].Fields[57].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[14].Value;
                            //145
                            nCFR.NonScheduledScreens[1].Fields[56].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[44].Value;
                            //146
                            nCFR.NonScheduledScreens[1].Fields[55].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[41].Value;
                            //147
                            nCFR.NonScheduledScreens[1].Fields[54].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[47].Value;
                            //148
                            nCFR.NonScheduledScreens[1].Fields[53].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[53].Value;
                            //149
                            nCFR.NonScheduledScreens[1].Fields[52].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[60].Value;
                            //150
                            nCFR.NonScheduledScreens[1].Fields[51].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[26].Value;
                            //151
                            nCFR.NonScheduledScreens[1].Fields[50].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[19].Value;
                            //152
                            nCFR.NonScheduledScreens[1].Fields[49].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[65].Value;
                            //153
                            nCFR.NonScheduledScreens[1].Fields[48].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[57].Value;
                            //154
                            nCFR.NonScheduledScreens[1].Fields[47].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[51].Value;
                            //155
                            nCFR.NonScheduledScreens[1].Fields[46].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[45].Value;
                            //156
                            nCFR.NonScheduledScreens[1].Fields[45].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[37].Value;
                            //157
                            nCFR.NonScheduledScreens[1].Fields[44].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[10].Value;
                            //158
                            nCFR.NonScheduledScreens[1].Fields[43].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[4].Value;
                            //159
                            nCFR.NonScheduledScreens[1].Fields[42].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[31].Value;
                            //160
                            nCFR.NonScheduledScreens[1].Fields[41].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[21].Value;
                            //161
                            nCFR.NonScheduledScreens[1].Fields[40].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[30].Value;
                            //162
                            nCFR.NonScheduledScreens[1].Fields[39].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[36].Value;
                            //163
                            nCFR.NonScheduledScreens[1].Fields[38].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[43].Value;
                            //164
                            nCFR.NonScheduledScreens[1].Fields[37].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[16].Value;
                            //165
                            nCFR.NonScheduledScreens[1].Fields[36].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[22].Value;
                            //166
                            nCFR.NonScheduledScreens[1].Fields[35].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[46].Value;
                            //167
                            nCFR.NonScheduledScreens[1].Fields[34].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[59].Value;
                            //168
                            nCFR.NonScheduledScreens[1].Fields[33].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[55].Value;
                            //169
                            nCFR.NonScheduledScreens[1].Fields[32].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[62].Value;
                            //170
                            nCFR.NonScheduledScreens[1].Fields[31].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[66].Value;
                            //171
                            nCFR.NonScheduledScreens[1].Fields[30].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[20].Value;
                            //172
                            nCFR.NonScheduledScreens[1].Fields[29].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[25].Value;
                            //173
                            nCFR.NonScheduledScreens[1].Fields[28].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[58].Value;
                            //174
                            nCFR.NonScheduledScreens[1].Fields[27].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[63].Value;
                            //175
                            nCFR.NonScheduledScreens[1].Fields[26].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[52].Value;
                            //176
                            nCFR.NonScheduledScreens[1].Fields[25].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[40].Value;
                            //177
                            nCFR.NonScheduledScreens[1].Fields[24].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[34].Value;
                            //178
                            nCFR.NonScheduledScreens[1].Fields[7].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[12].Value;
                            //179
                            nCFR.NonScheduledScreens[1].Fields[6].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[17].Value;
                            //180
                            nCFR.NonScheduledScreens[1].Fields[5].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[49].Value;
                            //181
                            nCFR.NonScheduledScreens[1].Fields[4].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[38].Value;
                            //182
                            nCFR.NonScheduledScreens[1].Fields[3].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[28].Value;
                            //183
                            nCFR.NonScheduledScreens[1].Fields[2].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[27].Value;
                            //184
                            nCFR.NonScheduledScreens[1].Fields[1].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[33].Value;
                            //185
                            nCFR.NonScheduledScreens[1].Fields[0].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[32].Value;
                            //186
                            nCFR.NonScheduledScreens[1].Fields[9].FieldValue = oSupScr.FormDataValue[3].NonScheduledItemsValue[1].Value;
                            if (BSCAUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }

                        }
                        catch (Exception e)
                        {
                            string e19 = oPolId + " | BSCA1 - Misc Cov failed | " + e;
                            ErrorString = ErrorString + e19 + System.Environment.NewLine;
                            Console.WriteLine(e19);
                        }





                        //Liability - BSCA 1
                        try
                        {

                        
                            // In BSCA1, liability is non-schedules, while in BSCA2, it is schedule item
                            string LBSID = nCFR.ScheduledScreens[2].ScheduleID;
                            CBLServiceReference.FieldItems[] LBFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, LBSID);
                            nCFR.ScheduledScreens[2].Items.Insert(0, LBFF[0]); //inserting at 0 since there is only 1
                            //1 - 2a - N/A
                            //3
                            nCFR.ScheduledScreens[2].Items[0][9].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[78].Value;
                            //4
                            nCFR.ScheduledScreens[2].Items[0][10].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[81].Value;
                            //5
                            nCFR.ScheduledScreens[2].Items[0][11].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[76].Value;
                            //6
                            nCFR.ScheduledScreens[2].Items[0][12].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[43].Value;
                            //7
                            nCFR.ScheduledScreens[2].Items[0][13].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[42].Value;
                            //8
                            nCFR.ScheduledScreens[2].Items[0][14].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[41].Value;
                            //9
                            nCFR.ScheduledScreens[2].Items[0][15].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[40].Value;
                            //10
                            nCFR.ScheduledScreens[2].Items[0][16].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[39].Value;
                            //11
                            nCFR.ScheduledScreens[2].Items[0][17].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[38].Value;
                            //12
                            nCFR.ScheduledScreens[2].Items[0][18].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[37].Value;
                            //13
                            nCFR.ScheduledScreens[2].Items[0][19].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[36].Value;
                            //14
                            nCFR.ScheduledScreens[2].Items[0][20].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[35].Value;
                            //15
                            nCFR.ScheduledScreens[2].Items[0][21].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[7].Value;
                            //16
                            nCFR.ScheduledScreens[2].Items[0][22].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[6].Value;
                            //17
                            nCFR.ScheduledScreens[2].Items[0][3].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[72].Value;
                            //18
                            nCFR.ScheduledScreens[2].Items[0][4].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[90].Value;
                            //19
                            nCFR.ScheduledScreens[2].Items[0][92].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[56].Value;
                            //20
                            nCFR.ScheduledScreens[2].Items[0][90].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[86].Value;
                            //21
                            nCFR.ScheduledScreens[2].Items[0][88].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[52].Value;
                            //22
                            nCFR.ScheduledScreens[2].Items[0][86].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[66].Value;
                            //23
                            nCFR.ScheduledScreens[2].Items[0][82].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[74].Value;
                            //24
                            nCFR.ScheduledScreens[2].Items[0][78].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[88].Value;
                            //25
                            nCFR.ScheduledScreens[2].Items[0][74].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[48].Value;
                            //26
                            nCFR.ScheduledScreens[2].Items[0][72].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[85].Value;
                            //27
                            nCFR.ScheduledScreens[2].Items[0][111].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[87].Value;
                            //28
                            nCFR.ScheduledScreens[2].Items[0][113].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[58].Value;
                            //29
                            nCFR.ScheduledScreens[2].Items[0][115].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[57].Value;
                            //30
                            nCFR.ScheduledScreens[2].Items[0][114].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[34].Value;
                            //31
                            nCFR.ScheduledScreens[2].Items[0][112].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[33].Value;
                            //32
                            nCFR.ScheduledScreens[2].Items[0][110].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[32].Value;
                            //33
                            nCFR.ScheduledScreens[2].Items[0][108].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[31].Value;
                            //34
                            nCFR.ScheduledScreens[2].Items[0][106].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[30].Value;
                            //35
                            nCFR.ScheduledScreens[2].Items[0][104].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[29].Value;
                            //36
                            nCFR.ScheduledScreens[2].Items[0][102].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[28].Value;
                            //37
                            nCFR.ScheduledScreens[2].Items[0][100].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[27].Value;
                            //38
                            nCFR.ScheduledScreens[2].Items[0][98].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[26].Value;
                            //39
                            nCFR.ScheduledScreens[2].Items[0][96].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[5].Value;
                            //40
                            nCFR.ScheduledScreens[2].Items[0][94].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[4].Value;
                            //41
                            nCFR.ScheduledScreens[2].Items[0][5].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[60].Value;
                            //42
                            nCFR.ScheduledScreens[2].Items[0][6].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[84].Value;
                            //43
                            nCFR.ScheduledScreens[2].Items[0][41].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[55].Value;
                            //44
                            nCFR.ScheduledScreens[2].Items[0][43].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[91].Value;
                            //45
                            nCFR.ScheduledScreens[2].Items[0][45].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[45].Value;
                            //46
                            nCFR.ScheduledScreens[2].Items[0][47].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[69].Value;
                            //47
                            nCFR.ScheduledScreens[2].Items[0][49].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[82].Value;
                            //48
                            nCFR.ScheduledScreens[2].Items[0][51].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[62].Value;
                            //49
                            nCFR.ScheduledScreens[2].Items[0][53].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[54].Value;
                            //50
                            nCFR.ScheduledScreens[2].Items[0][55].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[59].Value;
                            //51
                            nCFR.ScheduledScreens[2].Items[0][57].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[47].Value;
                            //52
                            nCFR.ScheduledScreens[2].Items[0][59].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[80].Value;
                            //53
                            nCFR.ScheduledScreens[2].Items[0][61].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[70].Value;
                            //54
                            nCFR.ScheduledScreens[2].Items[0][65].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[67].Value;
                            //55
                            nCFR.ScheduledScreens[2].Items[0][67].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[25].Value;
                            //56
                            nCFR.ScheduledScreens[2].Items[0][71].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[24].Value;
                            //57
                            nCFR.ScheduledScreens[2].Items[0][73].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[23].Value;
                            //58
                            nCFR.ScheduledScreens[2].Items[0][75].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[22].Value;
                            //59
                            nCFR.ScheduledScreens[2].Items[0][77].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[21].Value;
                            //60
                            nCFR.ScheduledScreens[2].Items[0][79].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[20].Value;
                            //61
                            nCFR.ScheduledScreens[2].Items[0][89].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[19].Value;
                            //62
                            nCFR.ScheduledScreens[2].Items[0][87].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[18].Value;
                            //63
                            nCFR.ScheduledScreens[2].Items[0][85].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[17].Value;
                            //64
                            nCFR.ScheduledScreens[2].Items[0][83].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[3].Value;
                            //65
                            nCFR.ScheduledScreens[2].Items[0][81].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[2].Value;
                            //66
                            nCFR.ScheduledScreens[2].Items[0][7].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[49].Value;
                            //67
                            nCFR.ScheduledScreens[2].Items[0][8].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[50].Value;
                            //68
                            nCFR.ScheduledScreens[2].Items[0][24].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[75].Value;
                            //69
                            nCFR.ScheduledScreens[2].Items[0][25].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[63].Value;
                            //70
                            nCFR.ScheduledScreens[2].Items[0][26].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[44].Value;
                            //71
                            nCFR.ScheduledScreens[2].Items[0][27].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[73].Value;
                            //72
                            nCFR.ScheduledScreens[2].Items[0][28].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[77].Value;
                            //73
                            nCFR.ScheduledScreens[2].Items[0][29].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[53].Value;
                            //74
                            nCFR.ScheduledScreens[2].Items[0][31].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[68].Value;
                            //75
                            nCFR.ScheduledScreens[2].Items[0][33].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[71].Value;
                            //76
                            nCFR.ScheduledScreens[2].Items[0][35].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[92].Value;
                            //77
                            nCFR.ScheduledScreens[2].Items[0][37].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[46].Value;
                            //78
                            nCFR.ScheduledScreens[2].Items[0][39].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[64].Value;
                            //79
                            nCFR.ScheduledScreens[2].Items[0][63].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[83].Value;
                            //80
                            nCFR.ScheduledScreens[2].Items[0][69].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[16].Value;
                            //81
                            nCFR.ScheduledScreens[2].Items[0][91].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[15].Value;
                            //82
                            nCFR.ScheduledScreens[2].Items[0][93].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[14].Value;
                            //83
                            nCFR.ScheduledScreens[2].Items[0][95].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[13].Value;
                            //84
                            nCFR.ScheduledScreens[2].Items[0][97].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[12].Value;
                            //85
                            nCFR.ScheduledScreens[2].Items[0][99].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[11].Value;
                            //86
                            nCFR.ScheduledScreens[2].Items[0][101].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[10].Value;
                            //87
                            nCFR.ScheduledScreens[2].Items[0][103].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[9].Value;
                            //88
                            nCFR.ScheduledScreens[2].Items[0][105].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[8].Value;
                            //89
                            nCFR.ScheduledScreens[2].Items[0][107].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[1].Value;
                            //90
                            nCFR.ScheduledScreens[2].Items[0][109].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[0].Value;
                            //91
                            nCFR.ScheduledScreens[2].Items[0][32].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[89].Value;
                            //92
                            nCFR.ScheduledScreens[2].Items[0][23].FieldValue = oSupScr.FormDataValue[4].NonScheduledItemsValue[65].Value;

                            if (BSCAUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }


                        }
                        catch (Exception e)
                        {
                            string e20 = oPolId + " | BSCA1 - Liability failed | " + e;
                            ErrorString = ErrorString + e20 + System.Environment.NewLine;
                            Console.WriteLine(e20);
                        }


                        // Professional / Other Liability - BSCA1
                        try
                        {

                        
                            //1 N/A

                            string col1val = oSupScr.FormDataValue[5].NonScheduledItemsValue[62].Value;
                            string col2val = oSupScr.FormDataValue[5].NonScheduledItemsValue[40].Value;
                            string col3val = oSupScr.FormDataValue[5].NonScheduledItemsValue[39].Value;
                            string col4val = oSupScr.FormDataValue[5].NonScheduledItemsValue[38].Value;
                            string col5val = oSupScr.FormDataValue[5].NonScheduledItemsValue[37].Value;
                            string col6val = oSupScr.FormDataValue[5].NonScheduledItemsValue[36].Value;
                            string col7val = oSupScr.FormDataValue[5].NonScheduledItemsValue[35].Value;
                            string col8val = oSupScr.FormDataValue[5].NonScheduledItemsValue[34].Value;
                            //2, 3, 10, 11, 14, 15, 18, 19, 22, 23 
                            List<Tuple<int, string>> MlEolist = new List<Tuple<int, string>>();

                            if (col1val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(62, "ML"));
                            }
                            if (col2val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(40, "ML"));
                            }
                            if (col3val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(39, "ML"));
                            }
                            if (col4val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(38, "ML"));
                            }
                            if (col5val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(37, "ML"));
                            }
                            if (col6val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(36, "ML"));
                            }
                            if (col7val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(35, "ML"));
                            }
                            if (col8val == "ML")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(34, "ML"));
                            }
                            if (col1val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(62, "EO"));
                            }
                            if (col2val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(40, "EO"));
                            }
                            if (col3val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(39, "EO"));
                            }
                            if (col4val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(38, "EO"));
                            }
                            if (col5val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(37, "EO"));
                            }
                            if (col6val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(36, "EO"));
                            }
                            if (col7val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(35, "EO"));
                            }
                            if (col8val == "EO")
                            {
                                MlEolist.Add(Tuple.Create<int, string>(34, "EO"));
                            }

                            if (MlEolist.Count > 0)
                            {
                                int MlEoCounter = 0;
                                foreach (Tuple<int, string> i in MlEolist)
                                {
                                    //Liability Code
                                    MlEoCounter++;
                                    (int, int, int) MlEoOldTuple = ProLibNums(i.Item1);
                                    (int, int, int, int) MlEoNewTuple = BSCA2ProLib2(MlEoCounter);
                                    if (i.Item2 == "EO")
                                    {
                                        nCFR.NonScheduledScreens[2].Fields[MlEoNewTuple.Item1].FieldValue = "E&O";
                                    }
                                    else
                                    {
                                        nCFR.NonScheduledScreens[2].Fields[MlEoNewTuple.Item1].FieldValue = "MAL";
                                    }
                                    nCFR.NonScheduledScreens[2].Fields[MlEoNewTuple.Item2].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[MlEoOldTuple.Item1].Value;
                                    nCFR.NonScheduledScreens[2].Fields[MlEoNewTuple.Item3].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[MlEoOldTuple.Item2].Value;
                                    nCFR.NonScheduledScreens[2].Fields[MlEoNewTuple.Item4].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[MlEoOldTuple.Item3].Value;
                                }
                            }
                            // 29-34, 47-52, 56-61, 65-70

                            List<Tuple<int, string>> OthLiablist = new List<Tuple<int, string>>();
                            if (col1val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(62, "DO")); }
                            if (col2val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(40, "DO")); }
                            if (col3val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(39, "DO")); }
                            if (col4val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(38, "DO")); }
                            if (col5val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(37, "DO")); }
                            if (col6val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(36, "DO")); }
                            if (col7val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(35, "DO")); }
                            if (col8val == "DO") { OthLiablist.Add(Tuple.Create<int, string>(34, "DO")); }

                            if (col1val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(62, "EPL")); }
                            if (col2val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(40, "EPL")); }
                            if (col3val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(39, "EPL")); }
                            if (col4val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(38, "EPL")); }
                            if (col5val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(37, "EPL")); }
                            if (col6val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(36, "EPL")); }
                            if (col7val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(35, "EPL")); }
                            if (col8val == "EPL") { OthLiablist.Add(Tuple.Create<int, string>(34, "EPL")); }

                            if (col1val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(62, "LPL")); }
                            if (col2val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(40, "LPL")); }
                            if (col3val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(39, "LPL")); }
                            if (col4val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(38, "LPL")); }
                            if (col5val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(37, "LPL")); }
                            if (col6val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(36, "LPL")); }
                            if (col7val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(35, "LPL")); }
                            if (col8val == "LPL") { OthLiablist.Add(Tuple.Create<int, string>(34, "LPL")); }

                            if (col1val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(62, "WVI")); }
                            if (col2val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(40, "WVI")); }
                            if (col3val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(39, "WVI")); }
                            if (col4val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(38, "WVI")); }
                            if (col5val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(37, "WVI")); }
                            if (col6val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(36, "WVI")); }
                            if (col7val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(35, "WVI")); }
                            if (col8val == "WVI") { OthLiablist.Add(Tuple.Create<int, string>(34, "WVI")); }

                            if (col1val == "C") { OthLiablist.Add(Tuple.Create<int, string>(62, "C")); }
                            if (col2val == "C") { OthLiablist.Add(Tuple.Create<int, string>(40, "C")); }
                            if (col3val == "C") { OthLiablist.Add(Tuple.Create<int, string>(39, "C")); }
                            if (col4val == "C") { OthLiablist.Add(Tuple.Create<int, string>(38, "C")); }
                            if (col5val == "C") { OthLiablist.Add(Tuple.Create<int, string>(37, "C")); }
                            if (col6val == "C") { OthLiablist.Add(Tuple.Create<int, string>(36, "C")); }
                            if (col7val == "C") { OthLiablist.Add(Tuple.Create<int, string>(35, "C")); }
                            if (col8val == "C") { OthLiablist.Add(Tuple.Create<int, string>(34, "C")); }

                            if (OthLiablist.Count > 0)
                            {
                                int OthLiabCounter = 0;
                                foreach (Tuple<int, string> i in OthLiablist)
                                {
                                    //Liability Code
                                    OthLiabCounter++;
                                    (int, int, int) MlEoOldTupleTwo = ProLibNums(i.Item1);
                                    (int, int, int, int) MlEoNewTupleTwo = BSCA2ProLib4(OthLiabCounter);
                                    nCFR.NonScheduledScreens[2].Fields[MlEoNewTupleTwo.Item1].FieldValue = i.Item2;
                                    nCFR.NonScheduledScreens[2].Fields[MlEoNewTupleTwo.Item2].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[MlEoOldTupleTwo.Item1].Value;
                                    nCFR.NonScheduledScreens[2].Fields[MlEoNewTupleTwo.Item3].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[MlEoOldTupleTwo.Item2].Value;
                                    nCFR.NonScheduledScreens[2].Fields[MlEoNewTupleTwo.Item4].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[MlEoOldTupleTwo.Item3].Value;
                                }
                            }
                            //4
                            nCFR.NonScheduledScreens[2].Fields[78].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[33].Value;
                            //5
                            nCFR.NonScheduledScreens[2].Fields[76].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[32].Value;
                            //6-9 N/A
                            //12
                            nCFR.NonScheduledScreens[2].Fields[114].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[66].Value;
                            //13
                            nCFR.NonScheduledScreens[2].Fields[106].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[67].Value;
                            //16
                            nCFR.NonScheduledScreens[2].Fields[99].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[68].Value;
                            //17
                            nCFR.NonScheduledScreens[2].Fields[119].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[69].Value;
                            //20-21- N/A
                            //24
                            nCFR.NonScheduledScreens[2].Fields[107].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[70].Value;
                            //25
                            nCFR.NonScheduledScreens[2].Fields[100].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[71].Value;
                            //26
                            nCFR.NonScheduledScreens[2].Fields[113].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[30].Value;
                            //27
                            nCFR.NonScheduledScreens[2].Fields[63].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[31].Value;
                            //28, 35-46, 53-55, 62-64, 71-75 - N/A
                            //76
                            nCFR.NonScheduledScreens[2].Fields[31].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[1].Value;
                            //77
                            nCFR.NonScheduledScreens[2].Fields[30].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[0].Value;
                            //78
                            nCFR.NonScheduledScreens[2].Fields[29].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[6].Value;
                            //79
                            nCFR.NonScheduledScreens[2].Fields[28].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[10].Value;
                            //80-83
                            string POALone = oSupScr.FormDataValue[5].NonScheduledItemsValue[20].Value;
                            if (POALone == "PO")
                            {
                                nCFR.NonScheduledScreens[2].Fields[3].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[7].Value;
                            }
                            if (POALone == "AL")
                            {
                                nCFR.NonScheduledScreens[2].Fields[27].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[7].Value;
                            }
                            string POALtwo = oSupScr.FormDataValue[5].NonScheduledItemsValue[18].Value;
                            if (POALtwo == "PO")
                            {
                                nCFR.NonScheduledScreens[2].Fields[2].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[11].Value;
                            }
                            if (POALtwo == "AL")
                            {
                                nCFR.NonScheduledScreens[2].Fields[26].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[11].Value;
                            }
                            //84
                            nCFR.NonScheduledScreens[2].Fields[6].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[22].Value;
                            //85
                            nCFR.NonScheduledScreens[2].Fields[5].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[24].Value;
                            //86
                            nCFR.NonScheduledScreens[2].Fields[4].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[4].Value;
                            //87
                            nCFR.NonScheduledScreens[2].Fields[25].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[26].Value;
                            //88
                            nCFR.NonScheduledScreens[2].Fields[24].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[27].Value;
                            //89
                            nCFR.NonScheduledScreens[2].Fields[23].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[21].Value;
                            //90
                            nCFR.NonScheduledScreens[2].Fields[22].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[12].Value;
                            //91
                            nCFR.NonScheduledScreens[2].Fields[21].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[8].Value;
                            //92
                            nCFR.NonScheduledScreens[2].Fields[20].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[15].Value;
                            //93
                            nCFR.NonScheduledScreens[2].Fields[19].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[28].Value;
                            //94
                            nCFR.NonScheduledScreens[2].Fields[18].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[25].Value;
                            //95
                            nCFR.NonScheduledScreens[2].Fields[17].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[19].Value;
                            //96
                            nCFR.NonScheduledScreens[2].Fields[16].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[5].Value;
                            //97
                            nCFR.NonScheduledScreens[2].Fields[15].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[14].Value;
                            //98
                            nCFR.NonScheduledScreens[2].Fields[14].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[9].Value;
                            //99
                            nCFR.NonScheduledScreens[2].Fields[13].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[29].Value;
                            //100
                            nCFR.NonScheduledScreens[2].Fields[12].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[23].Value;
                            //101
                            nCFR.NonScheduledScreens[2].Fields[11].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[17].Value;
                            //102
                            nCFR.NonScheduledScreens[2].Fields[10].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[13].Value;
                            //103
                            nCFR.NonScheduledScreens[2].Fields[9].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[3].Value;
                            //104
                            nCFR.NonScheduledScreens[2].Fields[8].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[16].Value;
                            //105
                            nCFR.NonScheduledScreens[2].Fields[7].FieldValue = oSupScr.FormDataValue[5].NonScheduledItemsValue[2].Value;


                            if (BSCAUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }

                        }
                        catch (Exception e)
                        {
                            string e21 = oPolId + " | BSCA1 - Prof/Other Liability failed | " + e;
                            ErrorString = ErrorString + e21 + System.Environment.NewLine;
                            Console.WriteLine(e21);
                        }



                        // Schedule Equipment - BSCA1
                        try
                        {

                        
                            int B1SchEqupCount = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count;
                            for (int i = 0; i<B1SchEqupCount; i++)
                            {
                                // First insert a schedule item
                                string SSBSID = nCFR.ScheduledScreens[3].ScheduleID;
                                CBLServiceReference.FieldItems[] SSBCFF = EpicSDKClient.Get_CustomForm_BlankScheduledItem(oMessageHeader, nLineID, SSBSID);
                                nCFR.ScheduledScreens[3].Items.Insert(i, SSBCFF[0]);
                                // Add fields for a given schedule
                                //1
                                nCFR.ScheduledScreens[3].Items[i][16].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[15].Value;
                                //2
                                nCFR.ScheduledScreens[3].Items[i][15].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[16].Value;
                                //3 - N/A
                                //4
                                nCFR.ScheduledScreens[3].Items[i][13].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[5].Value;
                                //5
                                nCFR.ScheduledScreens[3].Items[i][12].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[6].Value;
                                //6
                                nCFR.ScheduledScreens[3].Items[i][11].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[7].Value;
                                //7
                                nCFR.ScheduledScreens[3].Items[i][10].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[8].Value;
                                //8
                                nCFR.ScheduledScreens[3].Items[i][9].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[11].Value;
                                //9
                                nCFR.ScheduledScreens[3].Items[i][8].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[12].Value;
                                //10
                                nCFR.ScheduledScreens[3].Items[i][7].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[0].Value;
                                //11
                                nCFR.ScheduledScreens[3].Items[i][6].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[9].Value;
                                //12
                                nCFR.ScheduledScreens[3].Items[i][5].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[13].Value;
                                //13
                                nCFR.ScheduledScreens[3].Items[i][4].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[4].Value;
                                //14
                                nCFR.ScheduledScreens[3].Items[i][3].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[3].Value;
                                //15
                                nCFR.ScheduledScreens[3].Items[i][2].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[2].Value;
                                //16
                                nCFR.ScheduledScreens[3].Items[i][1].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[1].Value;
                                //17
                                nCFR.ScheduledScreens[3].Items[i][0].FieldValue = oSupScr.FormDataValue[6].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[i].ItemsValue[14].Value;
                            }

                            if (BSCAUpdateSwitch == 1)
                            {
                                EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            }
                        }
                        catch (Exception e)
                        {
                            string e22 = oPolId + " | BSCA1 - Schd Equip failed | " + e;
                            ErrorString = ErrorString + e22 + System.Environment.NewLine;
                            Console.WriteLine(e22);
                        }


                        if (BSCAUpdateSwitch == 1)
                        {
                            //EpicSDKClient.Update_CustomForm(oMessageHeader, nCFR);
                            CformUpdated = 1;
                            InitialLSFormStatus = true;
                        }

                        //SQL-Commented out
                        //if (SQLBSCAUpdate == 1)
                        //{
                        //    conn.Open();
                        //    using (SqlCommand commandLongFormUpdate = conn.CreateCommand())
                        //    {
                        //        string sqleight = string.Format("update {0} set CFormUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtable);
                        //        commandLongFormUpdate.CommandText = sqleight;

                        //        commandLongFormUpdate.Parameters.AddWithValue("@OldPolID", oPolId);
                        //        commandLongFormUpdate.ExecuteNonQuery();
                        //    }
                        //    conn.Close();
                        //}
                    }
                }
            }


            // Rough Work for Checking and Mapping

            //Console.WriteLine("rr: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue.Count);
            //Console.WriteLine("rrt: " + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[36].Value);

            //FormDataValue = TabIndex. Property = 2
            // ScheduledDataItemsRowsValue = Schedules in a tab
            // ItemsValue = Each Item




            //int BSCA1COPECount = oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue.Count;
            //for (int i = 0; i < BSCA1COPECount; i++)
            //{
            //    Console.WriteLine("Old-# " + i + ":" + oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[i].FieldDescription + ":" + oSupScr.FormDataValue[4].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[i].Value);
            //}

            //int BSCA1COPECount = oSupScr.FormDataValue[3].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue.Count;
            //for (int i = 0; i < BSCA1COPECount; i++)
            //{
            //    Console.WriteLine("Old-# " + i + ":" + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[i].FieldDescription + ":" + oSupScr.FormDataValue[2].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue[i].Value);
            //}


            //int SscrCount = oSupScr.FormDataValue.Count;
            //for (int i = 0; i < SscrCount; i++)
            //{
            //    Console.WriteLine(oSupScr.Name + "-" + i + ":" + oSupScr.FormDataValue[i].ScheduledScreensValue.Count);
            //}
            //Non - Scheduled items
            //int shdscr0 = oSupScr.FormDataValue[3].NonScheduledItemsValue.Count;
            //for (int i = 0; i < shdscr0; i++)
            //{
            //    Console.WriteLine("Old#-" + i + ":" + oSupScr.FormDataValue[3].NonScheduledItemsValue[i].FieldDescription + ":" + oSupScr.FormDataValue[3].NonScheduledItemsValue[i].Value);
            //}

            // Scheduled Items
            // Run when Scheduled equipemt is also there
            //int schsitem = 6;
            //int schScrCount = oSupScr.FormDataValue[schsitem].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0].ItemsValue.Count;
            //var schSc = oSupScr.FormDataValue[schsitem].ScheduledScreensValue[0].ScheduledDataItemsRowsValue[0];
            //for (int i=0; i< schScrCount; i++)
            //{
            //    Console.WriteLine("SchScr" + i + ":" + schSc.ItemsValue[i].FieldDescription+":"+ schSc.ItemsValue[i].Value);
            //}



            //int ssc = nCFR.ScheduledScreens.Count;
            //int nscc = nCFR.NonScheduledScreens.Count;
            //for (int i = 0; i < ssc; i++)
            //{
            //    Console.WriteLine("Schedule Screen #" + i + ": " + nCFR.ScheduledScreens[i].ScreenName);
            //}
            //for (int i = 0; i < nscc; i++)
            //{
            //    Console.WriteLine("Non-Schedule Screen #" + i + ": " + nCFR.NonScheduledScreens[i].ScreenName);
            //}
            //int oScreenNum = 1;
            //int ssf = oCFR.NonScheduledScreens[oScreenNum].Fields.Count;
            //for (int i = 0; i < ssf; i++)
            //{
            //    Console.WriteLine("#" + i + ": " + oCFR.NonScheduledScreens[oScreenNum].Fields[i].FieldLabel + ": " + oCFR.NonScheduledScreens[oScreenNum].Fields[i].FieldName + " : " + oCFR.NonScheduledScreens[oScreenNum].Fields[i].FieldValue);
            //}
            //int oSScreenNum = 2;
            //Console.WriteLine(nCFR.ScheduledScreens[oSScreenNum].ScreenName);
            //Console.WriteLine(nCFR.ScheduledScreens[oSScreenNum].Items.Count);
            //int sssf = nCFR.ScheduledScreens[oSScreenNum].Items[0].Count;
            //Console.WriteLine(sssf);
            //for (int i = 0; i < sssf; i++)
            //{
            //    Console.WriteLine("Property Field#" + i + ": " + nCFR.ScheduledScreens[oSScreenNum].Items[0][i].FieldName + " : " + nCFR.ScheduledScreens[oSScreenNum].Items[0][i].FieldValue);
            //}
            //int nssf = nCFR.NonScheduledScreens[0].Fields.Count;
            //for (int i = 0; i < nssf; i++)
            //{
            //    Console.WriteLine("#" + i + ":" + nCFR.NonScheduledScreens[0].Fields[i].FieldLabel + ":" + nCFR.NonScheduledScreens[0].Fields[i].FieldName + ":" + nCFR.NonScheduledScreens[0].Fields[i].FieldValue);
            //}
            //int cp = oCFR.ScheduledScreens[5].Items[0].Count;
            //for (int i = 0; i < cp; i++)
            //{
            //    Console.WriteLine("SchEquip#" + i + " : " + oCFR.ScheduledScreens[5].Items[0][i].FieldLabel + " : " + oCFR.ScheduledScreens[5].Items[0][i].FieldName + " : " + oCFR.ScheduledScreens[5].Items[0][i].FieldValue);
            //}
            //Console.WriteLine(nPolId);
            //Console.WriteLine(nCFR.ScheduledScreens[0].ScreenName);
            //Console.WriteLine(nCFR.ScheduledScreens[0].Items.Count);
            //int y = nCFR.ScheduledScreens[0].Items[0].Count;
            //for (int r = 0; r < y; r++)
            //{

            //    Console.WriteLine("New# " + r + ":" + nCFR.ScheduledScreens[0].Items[0][r].FieldLabel + ":" + nCFR.ScheduledScreens[0].Items[0][r].FieldName + ":" + nCFR.ScheduledScreens[0].Items[0][r].FieldValue);

            //}
            //int y = nCFR.ScheduledScreens[1].Items[1].Count;
            //for (int t = 0; t<y; t++)
            //{
            //    Console.WriteLine("Index: " + t);
            //    Console.WriteLine("fieldID: " + nCFR.ScheduledScreens[1].Items[1][t].FieldID);
            //    Console.WriteLine("label: " + nCFR.ScheduledScreens[1].Items[1][t].FieldLabel);
            //    Console.WriteLine("Name: " + nCFR.ScheduledScreens[1].Items[1][t].FieldName);
            //    Console.WriteLine("value: " + nCFR.ScheduledScreens[1].Items[1][t].FieldValue);
            //    Console.WriteLine("*-*-*-*-*-*-*");
            //}
            //Console.WriteLine(nCFR.ScheduledScreens[1].ScreenName);
            //Console.WriteLine(nCFR.ScheduledScreens[1].Items.Count);
            //int aa = nCFR.ScheduledScreens[1].Items[0].Count;
            //Console.WriteLine(aa);

            //Console.WriteLine(nCFR.NonScheduledScreens.Count);


            //Console.WriteLine("*-*-Long Form Update DONE-*-*");
            //Console.ReadKey();

            return InitialLSFormStatus;
        }

        public Tuple<string, string> CatchErrors()
        {
            string BigErrorStr = ErrorString;
            var ErrorRet = Tuple.Create<string, string>(BigErrorStr, ErrorFilePath);

            return ErrorRet;
        }

        //public void CFYesFinalUpdate(int oPolId)
        //{
        //    if (PolicyCreated == 1 && LineUpdated == 1 && CformUpdated == 1)
        //    {
        //        if (FinalYesSQLSwitch == 1)
        //        {
        //            conn.Open();
        //            using (SqlCommand commandOne = conn.CreateCommand())
        //            {

        //                string sql11 = string.Format("update {0} set EndTime = GETDATE(), ConversionSuccessful = 1 WHERE OldPolID = @OldPolID;", DBtable);
        //                commandOne.CommandText = sql11;

        //                commandOne.Parameters.AddWithValue("@OldPolID", oPolId);
        //                commandOne.ExecuteNonQuery();
        //            }
        //            conn.Close();
        //        }
        //    }
        //}
        //public void CFNoFinalUpdate(int oPolId)
        //{
        //    if (PolicyCreated == 1 && LineUpdated == 1 )
        //    {
        //        if (FinalYesSQLSwitch == 1)
        //        {
        //            conn.Open();
        //            using (SqlCommand commandOne = conn.CreateCommand())
        //            {

        //                string sql22 = string.Format("update {0} set EndTime = GETDATE(), ConversionSuccessful = 1 WHERE OldPolID = @OldPolID;", DBtable);
        //                commandOne.CommandText = sql22;

        //                commandOne.Parameters.AddWithValue("@OldPolID", oPolId);
        //                commandOne.ExecuteNonQuery();
        //            }
        //            conn.Close();
        //        }
        //    }
        //}





    }
    public class ConversionVars
    {
        public int AccountID { get; set; }
        public int UniqEntity { get; set; }
        public string CustLookup { get; set; }
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
