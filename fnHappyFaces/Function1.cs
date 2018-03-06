using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.ProjectOxford.Common.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System.Collections.Generic;
using System.IO;
using System;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;

namespace fnHappyFaces
{
    public static class HappyFaces
    {
        private static IFaceServiceClient faceServiceClient =
    new FaceServiceClient("bf4b96068387423d98b3e6f3bb9840a8", "https://virginia.api.cognitive.microsoft.us/face/v1.0");

        [FunctionName("HappyFaces")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            // parse query parameter
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            name = name ?? data?.blobname;

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(UploadAndDetectFaces(name, log)));
        }


        //private static Face DetectFace()

        private static async Task<Face[]> UploadAndDetectFaces(string blobname, TraceWriter log)
        {
            // The list of Face attributes to return.
            IEnumerable<FaceAttributeType> faceAttributes =
                new FaceAttributeType[] { FaceAttributeType.Gender, FaceAttributeType.Age, FaceAttributeType.Smile, FaceAttributeType.Emotion, FaceAttributeType.Glasses, FaceAttributeType.Hair };

            // Call the Face API.
            try
            {
                CloudStorageAccount storageAccount = null;
                CloudBlobClient blobClient = null;
                CloudBlobContainer container = null;
                CloudBlockBlob blockBlob = null;
                //storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["AzureWebJobsStorage"].ConnectionString);
                storageAccount = CloudStorageAccount.Parse(ConfigurationManager.ConnectionStrings["happyfacesstorageconnect"].ConnectionString);

                // Create the blob client.
                blobClient = storageAccount.CreateCloudBlobClient();

                // Retrieve reference to a previously created container.
                container = blobClient.GetContainerReference(ConfigurationManager.AppSettings["ImageContainer"]);

                // Retrieve reference to a blob.
                blockBlob = container.GetBlockBlobReference(blobname);

                Face[] faces = await faceServiceClient.DetectAsync(blockBlob.OpenRead(), returnFaceId: true, returnFaceLandmarks: false, returnFaceAttributes: faceAttributes);
                return faces;
            }
            // Catch and display Face API errors.
            catch (FaceAPIException f)
            {
                log.Error($"FaceAPIException Encountered. {f.ErrorCode}: {f.ErrorMessage}");

                return new Face[0];
            }
            // Catch and display all other errors.
            catch (Exception e)
            {
                log.Error($"Other Exception: {e.Message}");
                return new Face[0];
            }
        }
    }
}
