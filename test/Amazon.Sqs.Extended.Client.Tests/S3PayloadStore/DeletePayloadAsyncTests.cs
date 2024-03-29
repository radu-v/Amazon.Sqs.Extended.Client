using Amazon.Runtime;
using Amazon.S3;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Amazon.Sqs.Extended.Client.Tests.S3PayloadStore
{
    [TestFixture]
    public class DeletePayloadAsyncTests : IDisposable
    {
        const string BucketName = "bucket";
        const string S3Key = "test-key";

        Client.S3PayloadStore _s3PayloadStore = null!;
        IAmazonS3 _amazonS3Sub = null!;
        IPayloadStoreKeyProvider _payloadStoreKeyProviderSub = null!;
        PayloadPointer _payloadPointer;

        [SetUp]
        public void SetUp()
        {
            _amazonS3Sub = Substitute.For<IAmazonS3>();
            _payloadStoreKeyProviderSub = Substitute.For<IPayloadStoreKeyProvider>();
            var loggerSub = Substitute.For<ILogger<Client.S3PayloadStore>>();
            _s3PayloadStore = new Client.S3PayloadStore(_amazonS3Sub, _payloadStoreKeyProviderSub,
                new PayloadStoreConfiguration(BucketName), loggerSub);

            _payloadStoreKeyProviderSub.GenerateKey().Returns(S3Key);
            _payloadPointer = new PayloadPointer(BucketName, S3Key);
        }

        [Test]
        public async Task CallsDeleteObjectAsyncWithBucketNameAndKey()
        {
            // arrange, act
            await _s3PayloadStore.DeletePayloadAsync(_payloadPointer);

            // assert
            await _amazonS3Sub.Received(1).DeleteObjectAsync(BucketName, S3Key);
        }

        [Test]
        public void ThrowsAmazonClientExceptionWhenDeleteObjectAsyncFails()
        {
            // arrange
            _amazonS3Sub.DeleteObjectAsync(string.Empty, string.Empty)
                .ThrowsAsyncForAnyArgs(_ => new AmazonClientException(""));

            // act
            Task Act() => _s3PayloadStore.DeletePayloadAsync(_payloadPointer);

            // assert
            Assert.ThrowsAsync<AmazonClientException>(Act);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _amazonS3Sub.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}