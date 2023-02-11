using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Amazon.Sqs.Extended.Client.Tests.S3PayloadStore
{
    [TestFixture]
    public class ReadPayloadFromAsyncTests
    {
        const string BucketName = "bucket";
        const string S3Key = "test-key";

        Client.S3PayloadStore _s3PayloadStore = null!;
        IAmazonS3 _amazonS3Sub = null!;
        IPayloadStoreKeyProvider _payloadStoreKeyProviderSub = null!;
        PayloadStoreConfiguration _payloadStoreConfiguration = null!;
        PayloadPointer _payloadPointer;

        [SetUp]
        public void SetUp()
        {
            _amazonS3Sub = Substitute.For<IAmazonS3>();
            _payloadStoreKeyProviderSub = Substitute.For<IPayloadStoreKeyProvider>();
            var loggerSub = Substitute.For<ILogger<Client.S3PayloadStore>>();
            _payloadStoreConfiguration = new PayloadStoreConfiguration(BucketName);
            var options = Options.Create(_payloadStoreConfiguration);
            _s3PayloadStore = new Client.S3PayloadStore(_amazonS3Sub, _payloadStoreKeyProviderSub, loggerSub, options);

            _payloadStoreKeyProviderSub.GenerateKey().Returns(S3Key);
            _payloadPointer = new PayloadPointer(BucketName, S3Key);
        }

        [Test]
        public async Task CallsGetObjectAsyncWithGeneratedKey()
        {
            // arrange
            var response = new GetObjectResponse();
            response.ResponseStream = new MemoryStream();
            _amazonS3Sub.GetObjectAsync(BucketName, S3Key, Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(response));
            
            // act
            await _s3PayloadStore.ReadPayloadAsync(_payloadPointer);

            // assert
            _amazonS3Sub.ReceivedGetObjectAsyncCalls(1, BucketName, S3Key);
        }
        
        [Test]
        public void ThrowsAmazonClientExceptionWhenGetObjectAsyncFails()
        {
            // arrange
            _amazonS3Sub.GetObjectAsync(Arg.Any<GetObjectRequest>())
                .ThrowsAsyncForAnyArgs(_ => new AmazonClientException(""));

            // act
            Task Act() => _s3PayloadStore.ReadPayloadAsync(_payloadPointer);
            
            // assert
            Assert.ThrowsAsync<AmazonClientException>(Act);
        }
    }
}