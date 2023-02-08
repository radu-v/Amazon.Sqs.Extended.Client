using Amazon.S3;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests.S3PayloadStore
{
    public class S3PayloadStoreTestsBase
    {
        protected IAmazonS3 S3Sub { get; private set; } = null!;
        protected ILogger<Client.S3PayloadStore> DummyLogger = null!;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            DummyLogger = Substitute.For<ILogger<Client.S3PayloadStore>>();
        }

        [SetUp]
        public void Setup()
        {
            S3Sub = Substitute.For<IAmazonS3>();
        }
    }
}
