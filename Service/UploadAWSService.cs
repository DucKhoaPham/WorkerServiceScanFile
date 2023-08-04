using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WorkerService.Model;

namespace WorkerService
{
    class UploadAWSService
    {
        private List<FileInfo> fileInfos = new List<FileInfo>();
        private List<string> PathInfos = new List<string>();
        private readonly HttpClient httpClient = new HttpClient();
        Log _logger = new Logger();
        public DataTable ScanUpload(string connectionString)
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    string query = @"SELECT TOP 1000 ea.ID,ea.URL FROM eAttachment ea join eBook e ON ea.eBook_ID = e.ID 
                    WHERE ea.isUpload = 0 and ea.URL is not null";
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(dataTable);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return dataTable;
        }

        public int UpdateComplete(string connectionString, string ID)
        {
            int rowsAffected = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    string query = @"UPDATE eAttachment set isUpload = 1 WHERE ID = " + ID;
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        rowsAffected = command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return rowsAffected;
        }

        public int UpdateFail(string connectionString, string ID, int value)
        {
            int rowsAffected = 0;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    string query = @"UPDATE eAttachment set isUpload = " + value + " WHERE ID = " + ID;
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        rowsAffected = command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            return rowsAffected;
        }
        public async Task<bool> UploadCloud(System.IO.Stream inputStream, string fileName, string filePath, string connectionString, string ID, AwsS3Info awsS3Info)
        {
            try
            {
                var s3ClientConfig = new AmazonS3Config
                {
                    ServiceURL = awsS3Info.Endpoint
                };
                using (var client = new AmazonS3Client(awsS3Info.AccessKeyId, awsS3Info.SecretAccessKey, s3ClientConfig))
                {
                    int index = filePath.IndexOf(awsS3Info.FolderToUpload);
                    var realPath = "";
                    if (index != -1)
                    {
                        realPath = filePath.Substring(index);
                    }
                    var _bucketname = awsS3Info.BucketName + "/" + Path.GetDirectoryName(realPath).Replace("\\", "/");
                    var fileTransferUtility = new TransferUtility(client);
                    using (var memoryStream = new MemoryStream())
                    {
                        inputStream.CopyTo(memoryStream);
                        var fileTransferUtilityRequest = new TransferUtilityUploadRequest
                        {
                            BucketName = _bucketname.Substring(0, _bucketname.Length - 1),
                            StorageClass = S3StorageClass.StandardInfrequentAccess,
                            PartSize = 1048576 * 300, // 300 MB
                            Key = fileName,
                            InputStream = memoryStream,
                            CannedACL = S3CannedACL.PublicRead
                        };
                        await fileTransferUtility.UploadAsync(fileTransferUtilityRequest);
                        var isDone = UpdateComplete(connectionString, ID);
                        if (isDone > 0)
                            return true;
                        else
                            return false;
                    }
                }
            }
            catch (AmazonS3Exception ex)
            {
                UpdateFail(connectionString, ID, 3);
                _logger.Error(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                UpdateFail(connectionString, ID, 3);
                _logger.Error(ex.Message);
                throw ex;
            }
        }

        public async Task<bool> UploadFileAsync(System.IO.Stream inputStream, string fileName, string filePath, string connectionString, string ID, AwsS3Info awsS3Info)
        {
            try
            {
                var s3ClientConfig = new AmazonS3Config
                {
                    ServiceURL = awsS3Info.Endpoint
                };
                int index = filePath.IndexOf(awsS3Info.FolderToUpload);
                var realPath = "";
                if (index != -1)
                {
                    realPath = filePath.Substring(index);
                }
                var _bucketname = awsS3Info.BucketName + "/" + Path.GetDirectoryName(realPath).Replace("\\", "/");
                _bucketname = _bucketname.Substring(0, _bucketname.Length - 1);
                using (var client = new AmazonS3Client(awsS3Info.AccessKeyId, awsS3Info.SecretAccessKey, s3ClientConfig))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        inputStream.CopyTo(memoryStream);
                        var request = new PutObjectRequest
                        {
                            BucketName = _bucketname,
                            StorageClass = S3StorageClass.StandardInfrequentAccess,
                            InputStream = memoryStream,
                            Key = fileName, 
                            CannedACL = S3CannedACL.PublicRead
                        };
                        GetObjectRequest getObjectRequest = new GetObjectRequest { BucketName = _bucketname, Key = fileName };
                        try
                        {
                            //Check tồn tại file hay chưa
                            using (GetObjectResponse getResponse = await client.GetObjectAsync(getObjectRequest))
                            {
                                if (getResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                                {
                                    var isDone = UpdateComplete(connectionString, ID);
                                    if (isDone > 0)
                                        return true;
                                    else
                                        return false;
                                }
                            }
                        }
                        //Không có file báo lỗi
                        catch (AmazonS3Exception ex)
                        {
                            var response = await client.PutObjectAsync(request);
                            if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                            {
                                var isDone = UpdateComplete(connectionString, ID);
                                if (isDone > 0)
                                    return true;
                                else
                                    return false;
                            }
                            else
                            {
                                return false;
                            }
                        }
                         return false;
                    }
                }
            }
            catch (AmazonS3Exception ex)
            {
                UpdateFail(connectionString, ID, 3);
                _logger.Error(ex.Message);
                throw ex;
            }
            catch (Exception ex)
            {
                UpdateFail(connectionString, ID, 3);
                _logger.Error(ex.Message);
                throw ex;
            }
        }
    }
}
