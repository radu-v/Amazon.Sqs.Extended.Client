using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

[TestFixture]
public class ChangeMessageVisibilityBatchAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [Test]
    public async Task CallsBaseMethodWithOriginalReceiptHandle()
    {
        // arrange
        const string receiptHandle = "originalReceiptHandle";
        const string s3ReceiptHandle =
            $"{SqsExtendedClientConstants.S3BucketNameMarker}{SqsExtendedClientConstants.S3BucketNameMarker}{SqsExtendedClientConstants.S3KeyMarker}{SqsExtendedClientConstants.S3KeyMarker}{receiptHandle}";

        var request = new ChangeMessageVisibilityBatchRequest(SqsQueueUrl, new List<ChangeMessageVisibilityBatchRequestEntry>
        {
            new("1", receiptHandle) { VisibilityTimeout = 120 },
            new("2", s3ReceiptHandle) { VisibilityTimeout = 120 }
        });
        
        var options = Options.Create(ExtendedClientConfiguration);

        // act
        await new AmazonSqsExtendedClient(SqsClientSub, PayloadStoreSub, options, DummyLogger)
            .ChangeMessageVisibilityBatchAsync(request);

        //assert
        await SqsClientSub.Received(1).ChangeMessageVisibilityBatchAsync(Arg.Is<ChangeMessageVisibilityBatchRequest>(
            b =>
                b.QueueUrl == SqsQueueUrl &&
                b.Entries.All(c => c.ReceiptHandle == receiptHandle && c.VisibilityTimeout == 120)));
    }
}