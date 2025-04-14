using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Amazon.Sqs.Extended.Client.Tests.S3PayloadStore
{
    [TestFixture]
    public class StorePayloadAsyncTests : IDisposable
    {
        private const string BucketName = "bucket";
        private const string S3Key = "test-key";
        private const string PayloadBody = "test-body";

        private Client.S3PayloadStore _s3PayloadStore = null!;
        private IAmazonS3 _amazonS3Sub = null!;
        private IPayloadStoreKeyProvider _payloadStoreKeyProviderSub = null!;
        private PayloadStoreConfiguration _payloadStoreConfiguration = null!;

        [SetUp]
        public void SetUp()
        {
            _amazonS3Sub = Substitute.For<IAmazonS3>();
            _payloadStoreKeyProviderSub = Substitute.For<IPayloadStoreKeyProvider>();
            var loggerSub = Substitute.For<ILogger<Client.S3PayloadStore>>();
            _payloadStoreConfiguration = new PayloadStoreConfiguration(BucketName);
            _s3PayloadStore = new Client.S3PayloadStore(_amazonS3Sub, _payloadStoreKeyProviderSub, _payloadStoreConfiguration, loggerSub);

            _payloadStoreKeyProviderSub.GenerateKey().Returns(S3Key);
        }

        [Test]
        public async Task CallsPutObjectAsyncWithGeneratedKey()
        {
            // arrange, act
            await _s3PayloadStore.StorePayloadAsync(PayloadBody);

            // assert
            await _amazonS3Sub.Received(1).PutObjectAsync(Arg.Is<PutObjectRequest>(x =>
                x.BucketName == BucketName
                && x.Key == S3Key
                && x.ContentBody == PayloadBody
                && x.CannedACL == _payloadStoreConfiguration.S3CannedAcl));
        }

        [Test]
        public async Task CallsPutObjectAsyncWithProvidedKey()
        {
            // arrange
            const string key = "some-key";
            // act
            await _s3PayloadStore.StorePayloadAsync(PayloadBody, key);

            // assert
            await _amazonS3Sub.Received(1).PutObjectAsync(Arg.Is<PutObjectRequest>(x =>
                x.BucketName == BucketName
                && x.Key == key
                && x.ContentBody == PayloadBody
                && x.CannedACL == _payloadStoreConfiguration.S3CannedAcl));
        }

        [Test]
        public void ThrowsAmazonClientExceptionWhenPutObjectAsyncFails()
        {
            // arrange
            _amazonS3Sub.PutObjectAsync(Arg.Any<PutObjectRequest>())
                .ThrowsAsyncForAnyArgs(_ => new AmazonClientException(""));

            // act
            Task Act() => _s3PayloadStore.StorePayloadAsync("something");

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
