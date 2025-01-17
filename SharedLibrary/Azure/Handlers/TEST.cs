// using Microsoft.WindowsAzure.Storage.Blob;
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
//
// namespace SharedLibrary.Azure
// {
//     public class BlobHelper
//     {
//         private readonly CloudBlobContainer _rootContainer;
//
//         public BlobHelper(string containerName)
//         {
//             _rootContainer = GetContainerReference(containerName);
//         }
//
//         public async Task<List<CloudBlockBlob>> GetAllBlobsAsync(Guid installationId)
//         {
//             CloudBlobDirectory snDir = _rootContainer.GetDirectoryReference(installationId.ToString());
//             BlobContinuationToken continuationToken = null;
//             var blobs = new List<CloudBlockBlob>();
//
//             do
//             {
//                 var resultSegment = await snDir.ListBlobsSegmentedAsync(continuationToken);
//                 continuationToken = resultSegment.ContinuationToken;
//
//                 foreach (var item in resultSegment.Results)
//                 {
//                     if (item is CloudBlockBlob blob)
//                     {
//                         blobs.Add(blob);
//                     }
//                 }
//             } while (continuationToken != null);
//
//             return blobs;
//         }
//
//         private CloudBlobContainer GetContainerReference(string containerName)
//         {
//             // Assume this method is implemented to return a CloudBlobContainer reference
//             throw new NotImplementedException();
//         }
//     }
// }