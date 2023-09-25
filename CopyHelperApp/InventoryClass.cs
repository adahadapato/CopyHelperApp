using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CopyHelperApp
{
    public class InventoryClass
    {
        public async static Task PerformHouseKeeping()
        {
            var missingFiles = new List<string>();
            try
            {
                var _rHelper = Program.GetRegistry("SosDir").ToString();
                var exts = new[] { ".sos", ".def" };


                var localFileList = Directory.GetFiles(_rHelper, "*.*", SearchOption.AllDirectories)
                        .Where(d => !d.Contains("Images") && !exts.Any(x => d.EndsWith(x, StringComparison.OrdinalIgnoreCase))).ToList();

                if (localFileList == null || localFileList.Count == 0) return ;

                var result =  GetServerInventory();

                if (result == null)
                    return ;

                var remoteFiles = await result;// GetRemoteInventory();
                                              //if (remoteFiles == null) return null;
                                              //if (remoteFiles.Count == 0 && localFileList.Count == 0) return null;
                var remoteFileList = remoteFiles.Select(r => r.FileName).ToList();

                missingFiles = localFileList.Where(d => !remoteFileList.Any(r => r == Path.GetFileName(d))).ToList();
                if (missingFiles.Count > 0)
                {
                    var data = new List<HouseKeeping>();
                    foreach (var m in missingFiles)
                    {
                        data.Add(new HouseKeeping
                        {
                            FileName = m,
                            DeviceId = Program.GetRegistry("DeviceId").ToString(),
                            ExamYear = Program.GetRegistry("examYear").ToString(),
                            ExamType = (Program.GetRegistry("examType")=="SSCE") ? Program.GetRegistry("examination").ToString(): Program.GetRegistry("examType")
                        });
                    }

                    WriteServerInventory(data);
                }
                //return missingFiles;
            }
            catch (Exception )
            {
               // MessageBox.Show(ex.Message);
            }


            //return null;
        }

        private async static Task<List<InventoryApiModel>> GetServerInventory()
        {
            string ExamYear = Program.GetRegistry("examYear");
            var Job = Program.GetRegistry("Job");
            var system = Program.GetRegistry("DeviceId");
            string Exams = Program.GetRegistry("examType");
            var examType = Exams;
            if (Exams == "SSCE")
            {
                examType = Program.GetRegistry("examination").ToString();
            }

            var client = new RestClient("http://10.0.1.31/ManagerApi/api/");
            RestRequest request = new RestRequest($"inventory/{examType}/{Job}/{ExamYear}/{system}", Method.Get);
            //client.Timeout = 20000;
            RestResponse response = await client.GetAsync(request);
            if(response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var result = response.Content;
                if (result != null)
                {
                   var data =  JsonConvert.DeserializeObject<List<InventoryApiModel>>(result);
                    return data;
                }
            }
            return null;
        }

        private static async Task WriteServerInventory(List<HouseKeeping> data)
        {
            string ExamYear = Program.GetRegistry("examYear");
            var Job = Program.GetRegistry("Job");
            var system = Program.GetRegistry("DeviceId");
            string Exams = Program.GetRegistry("examType");
            var examType = Exams;
            if (Exams == "SSCE")
            {
                examType = Program.GetRegistry("examination").ToString();
            }

            var client = new RestClient("http://10.0.1.31/ManagerApi/api/");
            RestRequest request = new RestRequest($"inventory/{examType}/{Job}/{ExamYear}/{system}", Method.Post);
            request.AddJsonBody(data);
            //client.Timeout = 20000;
            RestResponse response = await client.PostAsync(request);
            var result = response.Content;
        }
    }

    public class InventoryApiModel
    {
        public string FileName { get; set; }
        public int Records { get; set; }
        public string SystemNo { get; set; }
        public bool IsDiscard { get; set; }
    }

    public class HouseKeeping
    {
        public string FileName { get; set; }
        public string DeviceId { get; set; }
        public string ExamType { get; set; }
        public string ExamYear { get; set; }
    }
}
