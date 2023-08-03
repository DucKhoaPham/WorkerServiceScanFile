using System;
using System.Data.SqlClient;

namespace WorkerService.Model
{
    public class AwsS3Info
    {
        public string Endpoint { get; set; }
        public string AccessKeyId { get; set; }
        public string SecretAccessKey { get; set; }
        public string BucketName { get; set; }
        public string FolderToUpload { get; set; }
        public string FolderRoot { get; set; }
        public string FTPFolderRoot { get; set; }
        public string FTPUserName { get; set; }
        public string FTPPassword { get; set; }
    }
}
