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
    public class ContactService
    {
       static string AuthenticationKey;
       static string DataBase;
       static string ConnectionString = ConfigurationManager.ConnectionStrings["EpicBDE"].ConnectionString;
       CBLServiceReference.MessageHeader oMessageHeader;

       public ContactService()
       {
            AuthenticationKey = ConfigurationManager.AppSettings["AppliedSDKKey"];
            DataBase = ConfigurationManager.AppSettings["AppliedSDKDatabase"];
            oMessageHeader = new CBLServiceReference.MessageHeader();
            oMessageHeader.AuthenticationKey = AuthenticationKey;
            oMessageHeader.DatabaseName = DataBase;
       }

        //public CBLServiceReference.Contact GetContactInfo(int ContactID)
        //{
        //    CBLServiceReference.EpicSDK_2017_02Client SDK = new CBLServiceReference.EpicSDK_2017_02Client();
        //    CBLServiceReference.ContactGetResult oContactResult = new CBLServiceReference.ContactGetResult();
        //    CBLServiceReference.ContactFilter oFilter = new CBLServiceReference.ContactFilter();
        //    CBLServiceReference.ContactGetResult1 oContactResult1 = new CBLServiceReference.ContactGetResult1();

        //    oFilter.AccountID = ContactID;
        //    oFilter.AccountTypeCode = "CUST";

        //    oContactResult1 = SDK.Get_Contact(oMessageHeader, oFilter, 0);

        //    return oContactResult1;
               


        //}

   



    }
}
