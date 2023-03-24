using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using Microsoft.Extensions.Logging;

namespace Amazon.Sqs.Extended.Client
{
    public sealed class S3PayloadStore : IPayloadStore
    {
        readonly IAmazonS3 _amazonS3;
        readonly IPayloadStoreKeyProvider _payloadStoreKeyProvider;
        readonly ILogger<S3PayloadStore> _logger;
        readonly PayloadStoreConfiguration _payloadStoreConfiguration;

        public S3PayloadStore(IAmazonS3 amazonS3,
            IPayloadStoreKeyProvider payloadStoreKeyProvider,
            PayloadStoreConfiguration configuration,
            ILogger<S3PayloadStore> logger)
        {
            _amazonS3 = amazonS3;
            _payloadStoreKeyProvider = payloadStoreKeyProvider;
            _logger = logger;
            _payloadStoreConfiguration = configuration;
        }

        public void Dispose()
        {
            _amazonS3.Dispose();
        }

        public async Task DeletePayloadAsync(PayloadPointer payloadPointer, CancellationToken cancellationToken = new())
        {
            const string failedToDeleteMessage = "Failed to delete the S3 object which contains the payload";

            try
            {
                await _amazonS3.DeleteObjectAsync(payloadPointer.BucketName,
                    payloadPointer.Key, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, failedToDeleteMessage);
                throw new AmazonClientException(failedToDeleteMessage, e);
            }
        }

        public async Task<string> ReadPayloadAsync(
            PayloadPointer payloadPointer,
            CancellationToken cancellationToken = new())
        {
            const string failedToReadMessage = "Failed to get the S3 object which contains the payload";

            try
            {
                using var response = await _amazonS3.GetObjectAsync(payloadPointer.BucketName,
                    payloadPointer.Key, cancellationToken);
                var stream = new StreamReader(response.ResponseStream);
                return await stream.ReadToEndAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, failedToReadMessage);
                throw new AmazonClientException(failedToReadMessage, e);
            }
        }

        public async Task<PayloadPointer> StorePayloadAsync(
            string payloadBody,
            CancellationToken cancellationToken = new())
        {
            var s3Key = _payloadStoreKeyProvider.GenerateKey();
            return await StorePayloadAsync(payloadBody, s3Key, cancellationToken);
        }

        public async Task<PayloadPointer> StorePayloadAsync(
            string payloadBody,
            string payloadKey,
            CancellationToken cancellationToken = new())
        {
            const string failedToWriteMessage = "Failed to store the message content in an S3 object";

            var request = new PutObjectRequest
            {
                Key = payloadKey,
                ContentBody = payloadBody,
                BucketName = _payloadStoreConfiguration.BucketName,
                CannedACL = _payloadStoreConfiguration.S3CannedAcl
            };

            try
            {
                await _amazonS3.PutObjectAsync(request, cancellationToken);
                return new PayloadPointer(_payloadStoreConfiguration.BucketName, payloadKey);
            }
            catch (Exception e)
            {
                _logger.LogError(e, failedToWriteMessage);
                throw new AmazonClientException(failedToWriteMessage, e);
            }
        }
    }
}