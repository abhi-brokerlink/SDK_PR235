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
using System.Data;

// PR235 - Conversion Code
// - By Abhishek Chhibber (achhibber@brokerlink.ca)
// V-1.0 - Feb 24, 2021


// ****IMPORTANT****:
//Before running the code, check the following:
//Check if any SQL or Updates are commented out, or their switches are off
// ADC
//BrokerCode



namespace EpicIntegrator
{
    class Program
    {
        static string ConnectionStrings = ConfigurationManager.ConnectionStrings["CBLReporting"].ConnectionString;
        public static SqlConnection conns = new SqlConnection(ConnectionStrings);
        public static string DBtables = "[CBL_Reporting].[dbo].[PR235Status]";
        

        static void Main(string[] args)
        {
            RunConversion();

            //For Testing Purposes:
            //PartTesting();
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
            List<int> PoliciesToDelete = new List<int>() {5989410,5989411,5989409,5989408,5989407,5989406,5989405,5989403,5989402,5989401,5989399,5989397,5989396,5989395,5989394,5989393,5989392,5989391,5989390,5989389,5989388,5989387,5989386,5989385,5989384,5989383,5989382,5989381,5989380,5989378,5989377,5989376,5989375,5989374,5989373,5989372,5989371,5989370,5989369,5989368,5989367,5989366,5989365,5989364,5989363,5989362,5989361,5989360,5989359,5989358,5989357,5989356,5989355,5989354,5989353,5989352,5989351,5989350,5989349,5989348,5989347,5989346,5989345,5989344,5989343,5989342,5989341,5989339,5989338,5989336,5989334,5989333,5989332,5989331,5989330,5989329,5989328,5989327,5989326,5989325,5989324,5989323,5989322,5989321,5989320,5989319,5989318,5989317,5989316,5989315,5989314,5989313,5989312,5989311,5989310,5989309,5989308,5989307,5989306,5989305,5989304,5989303,5989302,5989301,5989300,5989299,5989298,5989297,5989296,5989295,5989294,5989293,5989292,5989291,5989289,5989288,5989287,5989286,5989285,5989284,5989283,5989282,5989281,5989280,5989279,5989278,5989277,5989276,5989275,5989274,5989273,5989272,5989271,5989270,5989269,5989268,5989267,5989266,5989265,5989264,5989263,5989262,5989261,5989260,5989259,5989258,5989257,5989256,5989255,5989254,5989253,5989252,5989251,5989250,5989249,5989248,5989247,5989246,5989245,5989244,5989243,5989242,5989241,5989240,5989239,5989238,5989237,5989236,5989235,5989234,5989233,5989232,5989231,5989230,5989229,5989228,5989227,5989226,5989224,5989223,5989222,5989221,5989220,5989219,5989218,5989217,5989216,5989215,5989214,5989213,5989212,5989211,5989210,5989209,5989208,5989207,5989206,5989205,5989204,5989203,5989202,5989201,5989200,5989199,5989198,5989197,5989196,5989195,5989194,5989193,5989191,5989190,5989189,5989188,5989187,5989186,5989185,5989184,5989183,5989182,5989181,5989180,5989179,5989178,5989177,5989176,5989175,5989174,5989173,5989172,5989171,5989170,5989169,5989168,5989167,5989166,5989165,5989164,5989163,5989162,5989161,5989160,5989159,5989158,5989157,5989156,5989155,5989154,5989153,5989152,5989151,5989150,5989149,5989148,5989147,5989146,5989145,5989144,5989143,5989142,5989141,5989140,5989139,5989138,5989137,5989136,5989135,5989134,5989133,5989132,5989131,5989130,5989129,5989128,5989127,5989126,5989125,5989124,5989123,5989122,5989121,5989120,5989119,5989118,5989117,5989116,5989115,5989114,5989113,5989112,5989111,5989110,5989109,5989108,5989107,5989106,5989105,5989104,5989103,5989102,5989101,5989100,5989099,5989098,5989097,5989096,5989095,5989094,5989093,5989092,5989091,5989090,5989089,5989088,5989087,5989086,5989085,5989084,5989083,5989082,5989081,5989079,5989078,5989077,5989076,5989075,5989074,5989073,5989072,5989071,5989070,5989069,5989068,5989067,5989066,5989065,5989064,5989063,5989062,5989061,5989060,5989059,5989058,5989057,5989056,5989055,5989054,5989053,5989052,5989051,5989050,5989049,5989048,5989047,5989046,5989045,5989044,5989043,5989042,5989041,5989040,5989039,5989038,5989037,5989036,5989035,5989034,5989033,5989032,5989031,5989030,5989029,5989028,5989027,5989026,5989025,5989024,5989023,5989022,5989021,5989020,5989019,5989018,5989017,5989016,5989015,5989014,5989013,5989012,5989011,5989010,5989009,5989008,5989007,5989006,5989005,5989004,5989003,5989002,5989001,5989000,5988999,5988998,5988997,5988996,5988995,5988994,5988993,5988992,5988991,5988990,5988989,5988988,5988987,5988986,5988985,5988984,5988983,5988982,5988981,5988980,5988979,5988978,5988977,5988976,5988975,5988974,5988973,5988972,5988971,5988970,5988969,5988968,5988967,5988966,5988965,5988964,5988963,5988962,5988961,5988960,5988959,5988958,5988957,5988956,5988955,5988954,5988953,5988952,5988951,5988950,5988949,5988948,5988947,5988946,5988945,5988943,5988942,5988941,5988940,5988939,5988938,5988937,5988936,5988935,5988934,5988933,5988932,5988931,5988930,5988929,5988928,5988927,5988926,5988925,5988924,5988923,5988922,5988921,5988920,5988919,5988918,5988917,5988916,5988915,5988914,5988913,5988912,5988911,5988910,5988909,5988908,5988907,5988906,5988905,5988904,5988903,5988902,5988901,5988900,5988899,5988898,5988897,5988896,5988895,5988894,5988893,5988892,5988891,5988890,5988889,5988888,5988887,5988886,5988884,5988883,5988882,5988881,5988880,5988879,5988878,5988877,5988876,5988875,5988874,5988873,5988872,5988871,5988870,5988869,5988868,5988867,5988866,5988865,5988864,5988863,5988862,5988861,5988860,5988859,5988858,5988857,5988856,5988855,5988854,5988853,5988852,5988851,5988850,5988849,5988848,5988847,5988846,5988845,5988844,5988843,5988842,5988841,5988840,5988839,5988838,5988837,5988836,5988835,5988834,5988833,5988832,5988831,5988830,5988829,5988828,5988827,5988826,5988825,5988824,5988823,5988822,5988821,5988820,5988819,5988818,5988817,5988816,5988815,5988814,5988813,5988812,5988811,5988810,5988809,5988808,5988807,5988806,5988805,5988804,5988803,5988802,5988801,5988800,5988799,5988798,5988797,5988796,5988795,5988794,5988793,5988792,5988791,5988790,5988789,5988788,5988787,5988786,5988785,5988784,5988783,5988782,5988781,5988780,5988779,5988778,5988777,5988776,5988775,5988774,5988773,5988772,5988771,5988770,5988769,5988768,5988767,5988765,5988764,5988763,5988762,5988760,5988759,5988758,5988757,5988756,5988755,5988754,5988753,5988752,5988751,5988750,5988749,5988748,5988747,5988746,5988745,5988744,5988743,5988742,5988741,5988740,5988739,5988738,5988737,5988736,5988735,5988734,5988733,5988732,5988731,5988730,5988729,5988728,5988727,5988726,5988725,5988724,5988723,5988722,5988721,5988720,5988719,5988718,5988717,5988716,5988715,5988714,5988713,5988712,5988711,5988710,5988709,5988708,5988707,5988706,5988705,5988704,5988703,5988702,5988701,5988700,5988699,5988698,5988697,5988696,5988695,5988694,5988693,5988692,5988691,5988690,5988689,5988688,5988687,5988686,5988685,5988684,5988683,5988682,5988681,5988680,5988679,5988678,5988677,5988676,5988675,5988674,5988673,5988672,5988671,5988670,5988669,5988668,5988667,5988666,5988665,5988664,5988663,5988662,5988661,5988660,5988659,5988658,5988657,5988656,5988655,5988654,5988653,5988652,5988651,5988650,5988649,5988648,5988647,5988646,5988645,5988644,5988643,5988642,5988641,5988640,5988639,5988638,5988637,5988636,5988635,5988634,5988633,5988632,5988631,5988630,5988629,5988628,5988627,5988626,5988625,5988624,5988623,5988622,5988621,5988620,5988619,5988618,5988617,5988616,5988615,5988614,5988613,5988612,5988611,5988610,5988609,5988608,5988607,5988606,5988605,5988604,5988603,5988602,5988601,5988600,5988599,5988598,5988597,5988596,5988595,5988594,5988593,5988592,5988591,5988590,5988589,5988588,5988587,5988586,5988585,5988584,5988583,5988582,5988581,5988580,5988579,5988578,5988577,5988576,5988575,5988574,5988573,5988572,5988571,5988570,5988569,5988568,5988567,5988566,5988565,5988564,5988563,5988562,5988561,5988560,5988559,5988558,5988557,5988556,5988555,5988554,5988553,5988552,5988551,5988550,5988549,5988548,5988546,5988545,5988544,5988543,5988542,5988541,5988540,5988539,5988538,5988537,5988536,5988535,5988534,5988533,5988532,5988531,5988530,5988529,5988528,5988527,5988526,5988525,5988524,5988523,5988522,5988521,5988520,5988519,5988518,5988516,5988515,5988514,5988513,5988512,5988511,5988510,5988509,5988508,5988507,5988506,5988505,5988504,5988503,5988502,5988501,5988500,5988499,5988498,5988497,5988496,5988495,5988494,5988493,5988492,5988491,5988490,5988489,5988488,5988487,5988486,5988485,5988484,5988483,5988482,5988481,5988480,5988479,5988478,5988477,5988476,5988475,5988474,5988473,5988472,5988471,5988470,5988469,5988468,5988467,5988466,5988465,5988464,5988463,5988462,5988461,5988460,5988459,5988458,5988457,5988456,5988455,5988454,5988453,5988452,5988451,5988450,5988449,5988448,5988447,5988446,5988445,5988444,5988443,5988442,5988441,5988440,5988439,5988438,5988437,5988436,5988435,5988434,5988433,5988432,5988431,5988430,5988429,5988428,5988427,5988426,5988425,5988424,5988423,5988422,5988421,5988420,5988419,5988418,5988417,5988416,5988415,5988414,5988413,5988412,5988411,5988410,5988409,5988408,5988407,5988406,5988405,5988404,5988403,5988402,5988401,5988400,5988399,5988398,5988396,5988395,5988394,5988393,5988392,5988391,5988390,5988389,5988388,5988387,5988386,5988385,5988384,5988383,5988382,5988381,5988380,5988379,5988378,5988377,5988376,5988375,5988374,5988373,5988372,5988371,5988370,5988369,5988368,5988367,5988366,5988365,5988364,5988363,5988362,5988361,5988360,5988359,5988358,5988356,5988355,5988354,5988353,5988352,5988351,5988350,5988349,5988348,5988347,5988346,5988345,5988344,5988343,5988342,5988341,5988340,5988339,5988338,5988337,5988336,5988335,5988334,5988333,5988332,5988331,5988330,5988329,5988328,5988327,5988326,5988325,5988324,5988323,5988322,5988321,5988320,5988319,5988318,5988317,5988316,5988315,5988314,5988313,5988312,5988311,5988310,5988309,5988308,5988307,5988305,5988304,5988303,5988302,5988301,5988300,5988299,5988298,5988297,5988296,5988295,5988294,5988293,5988292,5988291,5988290,5988289,5988288,5988287,5988286,5988285,5988284,5988283,5988282,5988281,5988280,5988279,5988278,5988277,5988276,5988275,5988274,5988273,5988272,5988271,5988270,5988269,5988268,5988267,5988266,5988265,5988264,5988263,5988262,5988261,5988260,5988259,5988258,5988257,5988256,5988255,5988254,5988253,5988252,5988251,5988250,5988249,5988248,5988247,5988246,5988245,5988244,5988243,5988242,5988241,5988240,5988239,5988238,5988237,5988236,5988235,5988234,5988233,5988232,5988231,5988230,5988229,5988228,5988227,5988226,5988225,5988224,5988223,5988222,5988221,5988220,5988219,5988218,5988217,5988216,5988215,5988214,5988213,5988212,5988211,5988210,5988209,5988208,5988207,5988206,5988205,5988204,5988203,5988202,5988201,5988200,5988199,5988198,5988197,5988196,5988195,5988194,5988193,5988192,5988191,5988190,5988189,5988188,5988187,5988186,5988185,5988184,5988183,5988181,5988180,5988179,5988178,5988177,5988176,5988175,5988174,5988173,5988172,5988171,5988170,5988169,5988168,5988167,5988166,5988165,5988164,5988163,5988162,5988161,5988160,5988159,5988158,5988157,5988156,5988155,5988154,5988153,5988152,5988151,5988150,5988149,5988148,5988147,5988146,5988145,5988144,5988143,5988142,5988141,5988140,5988139,5988138,5988137,5988136,5988135,5988134,5988133,5988132,5988131,5988130,5988129,5988128,5988127,5988126,5988125,5988124,5988123,5988122,5988121,5988120,5988119,5988118,5988117,5988116,5988115,5988114,5988113,5988112,5988111,5988110,5988109,5988108,5988107,5988106,5988105,5988104,5988103,5988102,5988101,5988100,5988099,5988098,5988097,5988096,5988095,5988094,5988093,5988092,5988091,5988090,5988089,5988088,5988087,5988086,5988085,5988084,5988083,5988082,5988081,5988080,5988079,5988078,5988077,5988076,5988075,5988074,5988073,5988072,5988071,5988070,5988069,5988068,5988067,5988066,5988065,5988064,5988063,5988062,5988061,5988060,5988059,5988058,5988057,5988056,5988055,5988054,5988053,5988052,5988051,5988050,5988049,5988048,5988047,5988046,5988045,5988044,5988043,5988042,5988041,5988040,5988039,5988038,5988037,5988036,5988035,5988034,5988033,5988032,5988031,5988030,5988029,5988028,5988027,5988026,5988025,5988024,5988023,5988022,5988021,5988020,5988019,5988018,5988017,5988016,5988015,5988014,5988013,5988012,5988011,5988010,5988009,5988008,5988007,5988006,5988005,5988004,5988002,5988001,5988000,5987999,5987997,5987996,5987995,5987994,5987993,5987992,5987991,5987990,5987989,5987988,5987987,5987986,5987985,5987984,5987983,5987982,5987981,5987980,5987979,5987978,5987977,5987976,5987973,5987972,5987970,5987969,5987968,5987967,5987966,5987965,5987964,5987963,5987962,5987961,5987960,5987959,5987958,5987957,5987956,5987955,5987954,5987953,5987952,5987951,5987950,5987949,5987948,5987947,5987946,5987945,5987944,5987943,5987942,5987941,5987940,5987827,5987826,5987825,5987824,5987823,5987822,5987260,5987093,5987075,5987036,5987005,5987004,5986736,5986735,5986734,5986650};
            
            EpicIntegrator.ConversionService cs = new EpicIntegrator.ConversionService();
            foreach (int pol in PoliciesToDelete)
            {
                try
                {
                    cs.DeletePolicy(pol);
                }
                catch (Exception e)
                {
                    Console.WriteLine(" Policy Delete failed -#: " + e);
                }
            }
            Console.ReadKey();
        }

        //static void PartTesting(int oPolId,  int nPolId)
        static void PartTesting()
        {
            EpicIntegrator.ConversionService cs = new EpicIntegrator.ConversionService();
            List<Tuple<int, int>> polList = new List<Tuple<int, int>>();
            
             polList = cs.GetPolicyList();
            //cs.LongShortFormUpdate(oPolId, nPolId);
            //cs.UpdateLine(oPolId, nPolId);
            //cs.ReadMCS(oPolId, nPolId);
            //cs.CustomFormOrSupplimentalScreen(oPolId);
            foreach (Tuple<int, int> poli in polList)
            {
                int OldPolID = poli.Item1;
                int CFormExists = poli.Item2;

                Console.WriteLine("*-*-*-*-*");
                Console.WriteLine("Old Policy ID: " + OldPolID);

                conns.Open();
                using (SqlCommand commandOne = conns.CreateCommand())
                {

                    string sqltwo = string.Format("update {0} set StartTime = GETDATE() WHERE OldPolID = @OldPolID;", DBtables);
                    commandOne.CommandText = sqltwo;

                    commandOne.Parameters.AddWithValue("@OldPolID", OldPolID);
                    commandOne.ExecuteNonQuery();
                }
                conns.Close();




            }


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
            string ErrorString2 = "";

        // Get all policies
            polList = cs.GetPolicyList();
            if (polList == null)
            {
                Console.WriteLine("No record to process!");
                Console.ReadKey();
                return;
            }
            //Reading all policies one at a time

            Console.WriteLine("CONNS STATE " + conns.State);
            if (conns.State != ConnectionState.Closed)
            {
                conns.Close();
            }
            Console.WriteLine("CONNS STATE NEW " + conns.State);

            conns.Open();
            System.Threading.Thread.Sleep(2000);
            foreach (Tuple<int, int> poli in polList)
                //Parallel.ForEach(polList, poli =>
            {
                int OldPolID = poli.Item1;
                int CFormExists = poli.Item2;

                Console.WriteLine("*-*-*-*-*");
                Console.WriteLine("Old Policy ID: " + OldPolID);

                // SQL Start Time
                using (SqlCommand commandOne = conns.CreateCommand())
                {

                    string sqltwo = string.Format("update {0} set StartTime = GETDATE() WHERE OldPolID = @OldPolID;", DBtables);
                    commandOne.CommandText = sqltwo;

                    commandOne.Parameters.AddWithValue("@OldPolID", OldPolID);
                    commandOne.ExecuteNonQuery();
                }



                // Create a new policy
                Tuple<int, bool, string, string, int> NewPolicyResult = cs.CreatePolicy(OldPolID); //finalcheck
                int NewPolId = NewPolicyResult.Item1;
                Console.WriteLine("New Pol ID: " + NewPolId);
                bool HasMCS = NewPolicyResult.Item2;
                // wait 3 seconds for new policy data to commit to DB
                System.Threading.Thread.Sleep(2000);
                
                
                

                if (NewPolId.ToString().Length > 1)
                {
                    //Update table with new Policy Number 
                    using (SqlCommand commandTwo = conns.CreateCommand())
                    {
                        string sqlnine = string.Format("update {0} set NewPolID = @NewPolID, HasMCS = @hasMCS, NewPolLineTypeCode = @NewPolLineTypeCode, NewPolNum = @NewPolNum, NewPolicyInserted = GETDATE() WHERE OldPolID = @OldPolID;", DBtables);
                        commandTwo.CommandText = sqlnine;

                        commandTwo.Parameters.AddWithValue("@NewPolID", NewPolicyResult.Item1);
                        commandTwo.Parameters.AddWithValue("@hasMCS", NewPolicyResult.Item2);
                        commandTwo.Parameters.AddWithValue("@NewPolLineTypeCode", NewPolicyResult.Item3);
                        commandTwo.Parameters.AddWithValue("@NewPolNum", NewPolicyResult.Item4);
                        commandTwo.Parameters.AddWithValue("@OldPolID", NewPolicyResult.Item5);
                        commandTwo.ExecuteNonQuery();
                    }

                    //Update line
                    bool LineUpdateSuccess = false;
                    
                    try
                    {
                        cs.UpdateLine(OldPolID, NewPolId);
                        LineUpdateSuccess = true;
                        using (SqlCommand commandUpdateLine = conns.CreateCommand())
                        {
                            string sqlfour = string.Format("update {0} set LineInfoUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtables);
                            commandUpdateLine.CommandText = sqlfour;

                            commandUpdateLine.Parameters.AddWithValue("@OldPolID", OldPolID);
                            commandUpdateLine.ExecuteNonQuery();
                        }
                    }
                    catch (Exception e)
                    {
                        string e31 = OldPolID + " | Line Update failed | " + e;
                        ErrorString2 = ErrorString2 + e31 + System.Environment.NewLine;
                        Console.WriteLine(e31);
                    }
                    finally
                    {
                        if (LineUpdateSuccess == true)
                        {
                            // Try updating line PRBR
                            try
                            {
                                cs.UpdateLinePRBR(OldPolID, NewPolId);                                
                            }
                            catch (Exception e)
                            {
                                string e32 = OldPolID + " | Line PR/BR failed | " + e;
                                ErrorString2 = ErrorString2 + e32 + System.Environment.NewLine;
                                Console.WriteLine(e32);
                            }
                        }
                    }


                    // Update MCS
                    if (HasMCS == true)
                    {
                        try
                        {
                            cs.ReadMCS(OldPolID, NewPolId);
                            using (SqlCommand commandMCSupdate = conns.CreateCommand())
                            {
                                string sqlthree = string.Format("update {0} set MCSUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtables);
                                commandMCSupdate.CommandText = sqlthree;

                                commandMCSupdate.Parameters.AddWithValue("@OldPolID", OldPolID);
                                commandMCSupdate.ExecuteNonQuery();
                            }
                        }
                        catch (Exception e)
                        {
                            string e33 = OldPolID + " | MCS Update failed | " + e;
                            ErrorString2 = ErrorString2 + e33 + System.Environment.NewLine;
                            Console.WriteLine(e33);
                        }
                    }

                    // Updating Custom Forms
                    if (CFormExists == 1)
                    {
                        //Update Policy Info
                        try
                        {
                            cs.UpdatePolicyInfoApplicatLocation(OldPolID, NewPolId);
                            using (SqlCommand commandAppLoc = conns.CreateCommand())
                            {
                                string sqlfive = string.Format("update {0} set PolAppLocUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtables);
                                commandAppLoc.CommandText = sqlfive;

                                commandAppLoc.Parameters.AddWithValue("@OldPolID", OldPolID);
                                commandAppLoc.ExecuteNonQuery();
                            }
                        }
                        catch (Exception e)
                        {
                            string e34 = OldPolID + " | Policy Info Applicant Location failed | " + e;
                            ErrorString2 = ErrorString2 + e34 + System.Environment.NewLine;
                            Console.WriteLine(e34);
                        }

                        // Update Other Long Form Details
                        try
                        {
                            cs.UpdateOtherLongFormDetails(OldPolID, NewPolId);
                        }
                        catch (Exception e)
                        {
                            string e35 = OldPolID + " | Other Long Form Details failed | " + e;
                            ErrorString2 = ErrorString2 + e35 + System.Environment.NewLine;
                            Console.WriteLine(e35);
                        }


                        // Update Long Form / Short Form / BSCA1
                        try
                        {
                            bool LSformUpdate = cs.LongShortFormUpdate(OldPolID, NewPolId);
                            if (LSformUpdate == true)
                            {
                                using (SqlCommand commandLongFormUpdate = conns.CreateCommand())
                                {
                                    string sqlsix = string.Format("update {0} set CFormUpdated = GETDATE() WHERE OldPolID = @OldPolID;", DBtables);
                                    commandLongFormUpdate.CommandText = sqlsix;

                                    commandLongFormUpdate.Parameters.AddWithValue("@OldPolID", OldPolID);
                                    commandLongFormUpdate.ExecuteNonQuery();
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            string e36 = OldPolID + " - Long/Short/BSCA Form failed -#: " + e;
                            ErrorString2 = ErrorString2 + e36 + System.Environment.NewLine;
                            Console.WriteLine(e36);
                        }
                                                
                    }

                    try
                    {
                        using (SqlCommand commandOne = conns.CreateCommand())
                        {

                            string sql11 = string.Format("update {0} set EndTime = GETDATE(), ConversionSuccessful = 1 WHERE OldPolID = @OldPolID;", DBtables);
                            commandOne.CommandText = sql11;

                            commandOne.Parameters.AddWithValue("@OldPolID", OldPolID);
                            commandOne.ExecuteNonQuery();
                        }
                    }
                    catch (Exception e)
                    {
                        string e37 = OldPolID + " | Policy update failed | " + e;
                        ErrorString2 = ErrorString2 + e37 + System.Environment.NewLine;
                        Console.WriteLine(e37);
                    }





                

                }




                // Fred's Pre Policy insert SSR adjustments come here

                ////Update Line
                //cs.UpdateLine(OldPolID, NewPolId);

                //Update Line PRBR
                //cs.UpdateLinePRBR(OldPolID, NewPolId);


                ////Read MCS
                //if (HasMCS == true)
                //{
                //    cs.ReadMCS(OldPolID, NewPolId);
                //}
                //if (CFormExists == 1)
                //{
                //    //Update Policy Info
                //    cs.UpdatePolicyInfoApplicatLocation(OldPolID, NewPolId);

                //    // Update Other Long Form Details
                //    cs.UpdateOtherLongFormDetails(OldPolID, NewPolId);

                //    // Update Long Form / Short Form / BSCA1
                //    cs.LongShortFormUpdate(OldPolID, NewPolId);
                //}

                //// Fred's Post Policy insert SSR adjustments come here

                //if (CFormExists == 1)
                //{
                //    cs.CFYesFinalUpdate(OldPolID);
                //}
                //else if (CFormExists == 0)
                //{
                //    cs.CFNoFinalUpdate(OldPolID);
                //}

                Console.WriteLine("Completed Policy - " + OldPolID);
            }



            //); // Parallel threading for each ending

            Tuple<string, string> ErrorCarry = cs.CatchErrors();
            ErrorString2 = ErrorString2 + ErrorCarry.Item1;
            string [] ErrorString3 = new string[] { ErrorString2 };
            string ErrorPathFull = ErrorCarry.Item2 + DateTime.Now.ToString("yyyyMMddHHmmss")+".txt";
            File.WriteAllLines(ErrorPathFull, ErrorString3);


            conns.Close();
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
