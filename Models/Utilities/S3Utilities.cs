using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlmaDUploader.Models
{
    public static class S3Utilities
    {
        public static async Task CreateFile(string Key, string Content = null)
        {
            IAmazonS3 client = new AmazonS3Client(App.GetAWSCredentials(), GetEndPoint());
            PutObjectRequest request = new PutObjectRequest()
            {
                BucketName = Properties.Settings.Default.StorageBucket,
                Key = Key,
                ContentBody = Content
            };
            PutObjectResponse response = await client.PutObjectAsync(request); 
        }

        public static async Task DeleteFile(string Key)
        {
            IAmazonS3 client = new AmazonS3Client(App.GetAWSCredentials(), GetEndPoint());
            DeleteObjectRequest request = new DeleteObjectRequest()
            {
                BucketName = Properties.Settings.Default.StorageBucket,
                Key = Key
            };
            DeleteObjectResponse response = await client.DeleteObjectAsync(request); 
        }

        public static Amazon.RegionEndpoint GetEndPoint(string BucketName = null)
        {
            if (String.IsNullOrEmpty(BucketName))
                BucketName = Properties.Settings.Default.StorageBucket;

            if (BucketName.IndexOf("eu") >= 0)
                return Amazon.RegionEndpoint.EUWest1;
            else if (BucketName.IndexOf("ap") >= 0)
                return Amazon.RegionEndpoint.SAEast1;
            else
                return Amazon.RegionEndpoint.USEast1;
        }
    }
}
