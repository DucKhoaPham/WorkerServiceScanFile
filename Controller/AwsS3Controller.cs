using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using WorkerService.Model;

namespace WorkerService.Controller
{
    class AwsS3Controller
    {
        private UploadAWSService uploadAWS = new UploadAWSService();

        public async Task UploadFile(string connectionString, AwsS3Info awsS3Info)
        {
            Log _logger = new Logger();
            var data = uploadAWS.ScanUpload(connectionString);
            try
            {
                for (int i = 0; i < data.Rows.Count; i++)
                {
                    var filePath = "";
                    if (!String.IsNullOrEmpty(awsS3Info.FTPFolderRoot))
                    {
                        var credentials = new NetworkCredential(awsS3Info.FTPUserName, awsS3Info.FTPPassword);
                        var uri = new Uri(awsS3Info.FTPFolderRoot);
                        System.Net.CredentialCache credentialCache = new System.Net.CredentialCache { { uri, "Basic", credentials } };
                        filePath = awsS3Info.FTPFolderRoot + data.Rows[i]["URL"].ToString();
                    }
                    else
                    {
                        filePath = awsS3Info.FolderRoot + data.Rows[i]["URL"].ToString();
                    }
                    var id = data.Rows[i]["ID"].ToString();
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (!File.Exists(filePath))
                    {
                        _logger.Info(String.Format("{0} Không thấy đường dẫn {1} - ID: {2}", i + 1, filePath, id));
                        uploadAWS.UpdateStatusNotFound(connectionString, id);
                    }
                    else
                    {
                        _logger.Info(String.Format("{0} Đang upload đường dẫn {1} - ID: {2}", i + 1, filePath, id));
                        
                        using (FileStream fileStream = fileInfo.OpenRead())
                        {
                            bool result = await uploadAWS.UploadFileAsync(fileStream, fileInfo.Name, filePath, connectionString, id, awsS3Info);
                            if (result)
                                _logger.Info(String.Format("{0} Upload thành công {1} - ID: {2}", i + 1, filePath, id));
                            else
                            {
                                _logger.Info(String.Format("{0} Không thành công {1} - ID: {2}", i + 1, filePath, id));
                                uploadAWS.UpdateFail(connectionString, id);
                            }
                                
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(String.Format("Lỗi {0}", ex.Message));
                throw;
            }

        }
    }
}
