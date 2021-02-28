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
    public class ClientService
    {

        static string AuthenticationKey;
        static string DataBase;
        static string ConnectionString = ConfigurationManager.ConnectionStrings["EpicBDE"].ConnectionString;
        CBLServiceReference.MessageHeader oMessageHeader;

        public ClientService()
        {
            AuthenticationKey = ConfigurationManager.AppSettings["AppliedSDKKey"];
            DataBase = ConfigurationManager.AppSettings["AppliedSDKDatabase"];
            oMessageHeader = new CBLServiceReference.MessageHeader();
            oMessageHeader.AuthenticationKey = AuthenticationKey;
            oMessageHeader.DatabaseName = DataBase;
        }

        public List<EpicIntegrator.Client> GetClientSource(string query)
        {
            SqlConnection conn = new SqlConnection(ConnectionString);
            List<EpicIntegrator.Client> clients = new List<EpicIntegrator.Client>();

            SqlCommand command = new SqlCommand(query, conn);
            conn.Open();

            SqlDataReader rdr = command.ExecuteReader();

            if (rdr.HasRows)
            {
                while (rdr.Read())
                {
                    clients.Add(new EpicIntegrator.Client()
                    {
                        LookupCode = rdr["LookupCode"].ToString(),
                        NumberCallPermission = rdr["NumberCallPermission"].ToString(),
                        FaxCallPermission = rdr["FaxCallPermission"].ToString()
                    });
                }
            }
            else
            {
                return null;
            }
            rdr.Close();
            return clients;
        }

        public CBLServiceReference.ContactGetResult1 GetContact(int CustID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.ContactGetResult1 ContactResult = new CBLServiceReference.ContactGetResult1();
            CBLServiceReference.ContactFilter ContactFilter = new CBLServiceReference.ContactFilter();
            ContactFilter.AccountID = CustID;
            ContactFilter.AccountTypeCode = "CUST";
            ContactResult = EpicSDKClient.Get_Contact(oMessageHeader, ContactFilter, 0);
            return ContactResult;
        }

        public CBLServiceReference.ContactGetResult1 UpdateMarketingPref(int CustID)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.ContactGetResult1 ContactResult = new CBLServiceReference.ContactGetResult1();
            CBLServiceReference.ContactFilter ContactFilter = new CBLServiceReference.ContactFilter();
            List<CBLServiceReference.Contacts> ContactList = new List<CBLServiceReference.Contacts>();
            ContactFilter.AccountID = CustID;
            ContactFilter.AccountTypeCode = "CUST";
            ContactResult = EpicSDKClient.Get_Contact(oMessageHeader, ContactFilter, 0);
            int ContactCount = ContactResult.Contacts.Count();
            for (int i = 0; i < ContactCount; i++)
            {
                CBLServiceReference.Contact3 Con = ContactResult.Contacts[i];
                Con.ContactInfo.MarketingContactMethod = "Do Not Market";
                EpicSDKClient.Update_Contact(oMessageHeader, Con);
                Console.WriteLine("Contact Updated: " + i);
            }

            return ContactResult;
        }

        public CBLServiceReference.Client GetClientInfo(string lookupCode)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.Get_ClientResponse oGetClient = new CBLServiceReference.Get_ClientResponse();
            CBLServiceReference.ClientFilter oClientFilter = new CBLServiceReference.ClientFilter();

            oClientFilter.LookupCode = lookupCode;

            oGetClient.Get_ClientResult = EpicSDKClient.Get_Client(oMessageHeader, oClientFilter, 0);
            return oGetClient.Get_ClientResult.Clients[0];
        }


        public CBLServiceReference.Client UpdateName(string lookupCode)
        {
            CBLServiceReference.EpicSDK_2017_02Client EpicSDKClient = new CBLServiceReference.EpicSDK_2017_02Client();
            CBLServiceReference.Get_ClientResponse oGetClient = new CBLServiceReference.Get_ClientResponse();
            CBLServiceReference.ClientFilter oClientFilter = new CBLServiceReference.ClientFilter();

            oClientFilter.LookupCode = lookupCode;
            oGetClient.Get_ClientResult = EpicSDKClient.Get_Client(oMessageHeader, oClientFilter, 0);
            CBLServiceReference.Client ClientN = oGetClient.Get_ClientResult.Clients[0];

            ClientN.AccountName = "Phyllis M. Creaser (DEC***D)";
            

            EpicSDKClient.Update_Client(oMessageHeader, ClientN);
            return ClientN;
            
        }


        public List<CBLServiceReference.Client> GetClientsInfo(string sql)
        {
            CBLServiceReference.EpicSDK_2018_01Client EpicSDKClient = new CBLServiceReference.EpicSDK_2018_01Client();
            CBLServiceReference.Get_ClientResponse oGetClient = new CBLServiceReference.Get_ClientResponse();
            CBLServiceReference.ClientFilter oClientFilter = new CBLServiceReference.ClientFilter();

            List<CBLServiceReference.Client> EpicClients = new List<CBLServiceReference.Client>();

            ClientService svc = new ClientService();
            List<EpicIntegrator.Client> clients = new List<EpicIntegrator.Client>();
            clients = svc.GetClientSource(sql);

            foreach (EpicIntegrator.Client cli in clients)
            {
                oClientFilter.LookupCode = cli.LookupCode;
                oGetClient.Get_ClientResult = EpicSDKClient.Get_Client(oMessageHeader, oClientFilter, 0);
                EpicClients.Add(oGetClient.Get_ClientResult.Clients[0]);
            }

            return EpicClients;
        }

        public string UpdateServiceChargeFlag(EpicIntegrator.Client cli)
        {
            bool ServiceChargeFlag;

            try
            {
                CBLServiceReference.EpicSDK_2018_01Client SDK = new CBLServiceReference.EpicSDK_2018_01Client();
                CBLServiceReference.Get_ClientResponse oGetClient = new CBLServiceReference.Get_ClientResponse();
                CBLServiceReference.ClientFilter oClientFilter = new CBLServiceReference.ClientFilter();

                oClientFilter.LookupCode = cli.LookupCode;

                CBLServiceReference.ClientGetResult oClientGetResult = SDK.Get_Client(oMessageHeader, oClientFilter, 0);

                // Comment out this section if forcing false value, i.e. for testing
                ServiceChargeFlag = true;

                if (oClientGetResult.Clients[0].BillingValue.ServiceChargeFlag != ServiceChargeFlag)
                {
                    if (cli.NumberCallPermission != "") { 
                        oClientGetResult.Clients[0].AccountValue.NumberCallPermission = cli.NumberCallPermission;
                    }
                    if (cli.FaxCallPermission != "")
                    {
                        oClientGetResult.Clients[0].AccountValue.FaxCallPermission = cli.FaxCallPermission;
                    }
                    oClientGetResult.Clients[0].BillingValue.ServiceChargeFlag = ServiceChargeFlag;
                    SDK.Update_Client(oMessageHeader, oClientGetResult.Clients[0]);
                }
                SDK.Close();
                return "OK";
            } catch (Exception e) {
                return e.Message;
            }
        }

        public int UpdateSicCode(int accountID, int contactId, string sicCode)
        {
            try
            {
                CBLServiceReference.EpicSDK_2018_01Client SDK = new CBLServiceReference.EpicSDK_2018_01Client();
                CBLServiceReference.ContactFilter oContactFilter = new CBLServiceReference.ContactFilter();

                oContactFilter.AccountID = accountID;
                oContactFilter.AccountTypeCode = "CUST";
                oContactFilter.ContactID = contactId;

                CBLServiceReference.ContactGetResult1 oContactGetResult = SDK.Get_Contact(oMessageHeader, oContactFilter, 1);

                oContactGetResult.Contacts[0].BusinessInfo.SIC = sicCode;
                SDK.Update_Contact(oMessageHeader, oContactGetResult.Contacts[0]);
                SDK.Close();
                return 1;
            } catch {
                return 0;
            }
        }
    }

    public class Client
    {
        public string LookupCode { get; set; }
        public string NumberCallPermission { get; set; }
        public string FaxCallPermission { get; set; }

        public string NewClientName { get; set; }
    }
}
