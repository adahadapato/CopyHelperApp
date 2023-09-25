
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace CopyHelperApp
{
    class Program
    {
        static bool IsDiscard = false;
        static string Exams;
        static string subject;

        static bool PingSvr()
        {
            bool pinable = false;
            Ping pinger = null;
            try
            {
                pinger = new Ping();
                PingReply reply = pinger.Send("10.0.1.31");
                pinable = reply.Status==IPStatus.Success;
            }
            catch (PingException)
            {
                pinable = true;
            }
            finally
            {
                if(pinger != null)
                {
                    pinger.Dispose();
                }
            }
            return pinable;
            
        }
        static int count = 0;
        static void Main(string[] args)
        {
            var fileName = args[0];
            Action(fileName);
            //return result;
        }



        private static void Action(string fileName)
        {

            //Task.Run(async() =>
            //{
                //var fileName = @"C:\beceans21\Main_95\1129537.001";
                string ExamYear = GetRegistry("examYear");
                IsDiscard = fileName.Contains("Disc_");
                var Job = GetRegistry("Job");
                Exams = GetRegistry("examType");
                string scanType;
                var Userid = GetRegistry("OperatorId");
                var FileName = Path.GetFileName(fileName);
                //var systemNo = (Exams.Contains("NCEE"))? $"SYS{FileName.Substring(1, 2)}" : $"SYS{FileName.Substring(3, 2)}";
                var systemNo = GetRegistry("DeviceId");// (Exams.Contains("NCEE")) ? FileName.Substring(1, 2) : FileName.Substring(3, 2);

                subject = GetRegistry("shortsubj");
                var paper = Convert.ToInt32(GetRegistry("Paper"));
                string scanDir;
                if (IsDiscard)
                {
                    var tempDir = GetRegistry("sosDir").ToString();
                    scanDir = tempDir.Substring(3, (tempDir.Length - 3));
                    scanType = $"Disc_{systemNo}";
                }
                else
                {
                    var tempDir = GetRegistry("sosDir").ToString();
                    scanDir = tempDir.Substring(3, (tempDir.Length - 3));
                    scanType = $"{GetRegistry("scanType")}_{systemNo}";
                }

                var scanData = GetData(fileName);
                var Data = new ScanDataDTO()
                {
                    ScanFile = FileName,
                    JobDir = scanDir,
                    Responses = scanData,
                    Job = Job,
                    ExamType = (Exams == "SSCE") ? $"{Exams} {GetRegistry("examination")}" : Exams,
                    ScanType = scanType,
                    SystemNo = systemNo,
                    OperatorId = Userid,
                    Subject = (Exams.Contains("NCEE") || Exams.Contains("GIFT")) ? $"Paper {subject}" : subject,
                    ExamYear = ExamYear
                };
                PostData(Data, Exams, Job);
            //});
            
            
            
        }

        private static async Task PostData(ScanDataDTO data, string exams, string job)
        {

            bool p = false;
            while (count <= 10 && !p)
            {
                p = PingSvr();
                count++;
            };

            if (!p) return;

            string examType = exams;
            if (Exams == "SSCE")
            {
                if (GetRegistry("examination") == "Internal")
                {
                    examType = "ssce";
                }
                else
                {
                    examType = "nov";
                }
            }

            if (Exams == "GIFTED")
            {
                examType = "gift";
            }

            var client = new RestClient("http://10.0.1.32/ManagerApi/api/");
            RestRequest request = new RestRequest($"scanning/{examType}/{job}/save", Method.Post);
            request.AddJsonBody(data);
            //client.Timeout = 20000;
            RestResponse response = client.Execute(request);
            var result = response.Content;

            await InventoryClass.PerformHouseKeeping();
        }


       

       

       

        public static List<string> GetData(string FileName)
        {
            var lines = File.ReadAllLines(FileName).ToList();
            if (lines.Count == 0)
                return null;

            var scanData = new List<string>();
            foreach (var l in lines)
            {

                scanData.Add(l);
                /*{
                    ReferenceNo = (Exams.Contains("NCEE")) ? $"{l.Substring(0, 9)}{subject}" : $"{l.Substring(0, 10)}{l.Substring(14, 4)}",
                    CandidateNo = l.Substring(0, 8),
                    SchoolNo = "9999999",
                    Subject = (Exams.Contains("NCEE")) ? $"{subject}" : l.Substring(14, 4),
                    SerialNo = (Exams.Contains("NCEE")) ? "000" : l.Substring(10, 4),
                    Response = (Exams.Contains("NCEE")) ? l.Substring(10, 120) : l.Substring(10, 100),
                    FileNo = ScanFile,
                    UserId = Userid,
                    DeviceId = SystemNo
                });*/
            }
            return scanData;
        }

        public static string GetRegistry(string rKey)//reading registry values
        {
            var rtValue = "";
            try
            {

                RegistryKey mICParams = Registry.CurrentUser; //declare registry variable
                mICParams = mICParams.OpenSubKey("software", true);//open the registry subkey(ie Software)
                foreach (string Keyname in mICParams.GetSubKeyNames())//loop through all entries in the CurrentUser/Software key
                {

                    if (Keyname == "necoscan")//check if necoscan exists 
                    {
                        mICParams = mICParams.OpenSubKey("necoscan", true);//open the key
                        rtValue = mICParams.GetValue(rKey).ToString();//read value into variable
                        break;//terminate the loop.

                    }

                }
                mICParams.Close();//close the reg key.
            }
            catch (Exception ex)
            {
               Console.WriteLine(ex.Message, "Export Filter - Registry");
            }

            return rtValue;//return the value read.
        }
       
    }

    /* public class ScanDataDTO
     {
         //[MaxLength(4)]
         //public string Exam { get; set; }
         public string ScanFile { get; set; }
         public bool IsDiscard { get; set; }
         public string Subject { get; set; }
         public int Paper { get; set; }
         public List<ScanData> Data { get; set; }
     }*/

    public class ScanDataDTO
    {
        public string ExamYear { get; set; }
        public string OperatorId { get; set; }
        public string ScanType { get; set; }
        public string ScanFile { get; set; }
        public string JobDir { get; set; }
        public string ExamType { get; set; }
        public string Job { get; set; }
        public string SystemNo { get; set; }
        public string Subject { get; set; }
        public List<string> Responses { get; set; }
    }

public class ScanData
    {
        public string ReferenceNo { get; set; }
        public string CandidateNo { get; set; }
        public string SchoolNo { get; set; }
        public string Subject { get; set; }
        public string SerialNo { get; set; }
        public string Response { get; set; }
        public string FileNo { get; set; }
        public string UserId { get; set; }
        public string DeviceId { get; set; }
        public DateTime Date { get; set; }
    }

    public class EMSDataDTO
    {
        public string SheetId { get; set; }
        public string FileNo { get; set; }
        public string Responses { get; set; }
        public string UserId { get; set; }
        public string DeviceId { get; set; }
        public DateTime Date { get; set; }
    }
   
}
