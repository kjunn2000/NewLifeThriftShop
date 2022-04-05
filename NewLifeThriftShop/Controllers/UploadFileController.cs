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

        public IActionResult Index(string msg = "")
        {
            ViewBag.msg = msg;
            return View();
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

        public async Task<IActionResult> ViewImages(string msg = "")
        {
            ViewBag.msg = msg;
            List<string> credentialInfo = getAWSCredentialInfo();
            var displayResult = new List<S3Object>();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1]
                , credentialInfo[2], Amazon.RegionEndpoint.USEast1);
            string token = null;
            List<string> presignedURLs = new List<string>();
            try
            {
                do
                {
                    ListObjectsRequest viewRequest = new ListObjectsRequest()
                    {
                        BucketName = bucketName
                    };
                    ListObjectsResponse response = await S3Client.ListObjectsAsync(viewRequest).ConfigureAwait(false);
                    displayResult.AddRange(response.S3Objects);
                    token = response.NextMarker;
                }
                while (token != null);
                foreach (var item in displayResult)
                {
                    GetPreSignedUrlRequest request = new GetPreSignedUrlRequest()
                    {
                        BucketName = item.BucketName,
                        Key = item.Key,
                        Expires = DateTime.Now.AddMinutes(2)
                    };
                    presignedURLs.Add(S3Client.GetPreSignedURL(request));
                }
                ViewBag.ImageLinks = presignedURLs;
            }
            catch (Exception ex)
            {

            }
            return View(displayResult);
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

        public async Task<IActionResult> DownloadImage(string FileName, string Directory)
        {
            string message = "";
            List<string> credentialInfo = getAWSCredentialInfo();
            var S3Client = new AmazonS3Client(credentialInfo[0], credentialInfo[1],
                credentialInfo[2], Amazon.RegionEndpoint.USEast1);

            Directory = (!string.IsNullOrEmpty(Directory)) ? bucketName + "/" + Directory : bucketName;
            string downloadPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\" + FileName;
            try
            {
                TransferUtility transferFileToPC = new TransferUtility(S3Client);
                await transferFileToPC.DownloadAsync(downloadPath, Directory, FileName);
                message = FileName + " is successfully downloaded.";
            }
            catch (Exception ex)
            {
                message = FileName + " is unsuccessfully downloaded. \\n Error:" + ex.Message;
            }
            return RedirectToAction("ViewImages", "UploadFile", new { msg = message });
        }
    }
}
