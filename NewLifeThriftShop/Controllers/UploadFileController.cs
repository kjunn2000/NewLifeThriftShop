using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using Amazon.S3.Transfer;

namespace NewLifeThriftShop.Controllers
{
    public class UploadFileController : Controller
    {
        private const string bucketName = "newlifethriftshops3bucket";
        private const string resizedBucketName = "newlifethriftshops3bucket-resized";

        public UploadFileController()
        {
        }

        private List<string> getAWSCredentialInfo()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            IConfigurationRoot configure = builder.Build();
            List<string> credentialInfo = new List<string>();
            credentialInfo.Add(configure["AWSCredential:AccessKey"]);
            credentialInfo.Add(configure["AWSCredential:SecretKey"]);
            credentialInfo.Add(configure["AWSCredential:SessionToken"]);

            return credentialInfo;
        }

        public async Task Upload(List<IFormFile> images, string productId)
        {
            List<string> credentialInfo = getAWSCredentialInfo();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1],
                credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            foreach (var image in images)
            {
                string ext = Path.GetExtension(image.FileName);
                var uploadRequest = new PutObjectRequest()
                {
                    InputStream = image.OpenReadStream(),
                    BucketName = bucketName,
                    Key = productId + ext,
                    CannedACL = S3CannedACL.PublicRead
                };
                PutObjectResponse result = await S3Client.PutObjectAsync(uploadRequest);
            }
        }

        public async Task DeleteImage(string FileName)
        {
            List<string> credentialInfo = getAWSCredentialInfo();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1],
                credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            try
            {
                if (string.IsNullOrEmpty(FileName))
                    return;
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = bucketName,
                    Key = FileName
                };
                await S3Client.DeleteObjectAsync(deleteObjectRequest);
                var deleteResizedObjectRequest = new DeleteObjectRequest
                {
                    BucketName = resizedBucketName,
                    Key = "resized-"+FileName
                };
                await S3Client.DeleteObjectAsync(deleteResizedObjectRequest);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }
    }
}
