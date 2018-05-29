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
            IAmazonS3 client = new AmazonS3Client(App.GetAWSCredentials(), GetConfig());
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
            IAmazonS3 client = new AmazonS3Client(App.GetAWSCredentials(), GetConfig());
            DeleteObjectRequest request = new DeleteObjectRequest()
            {
                BucketName = Properties.Settings.Default.StorageBucket,
                Key = Key
            };
            DeleteObjectResponse response = await client.DeleteObjectAsync(request); 
        }

        public static Amazon.S3.AmazonS3Config GetConfig(string BucketName = null)
        {
            if (String.IsNullOrEmpty(BucketName))
                BucketName = Properties.Settings.Default.StorageBucket;

            var config = new AmazonS3Config();
            if (BucketName.StartsWith("eu"))
                config.RegionEndpoint = Amazon.RegionEndpoint.EUCentral1;
            else if (BucketName.StartsWith("ap"))
                config.RegionEndpoint = Amazon.RegionEndpoint.SAEast1;
            else if (BucketName.StartsWith("ca"))
            { 
                // Canada region not available in the 2.* AWSSDK. Build config manually.
                config.ServiceURL = "https://s3.ca-central-1.amazonaws.com";
                config.SignatureMethod = Amazon.Runtime.SigningAlgorithm.HmacSHA256;
                config.SignatureVersion = "4";
            }
            else
                config.RegionEndpoint = Amazon.RegionEndpoint.USEast1;

            return config;
        }
    }
}
