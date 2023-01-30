using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

public class ChangeMessageVisibilityAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [Test]
    public async Task CallsBaseMethodWithOriginalReceiptHandle([Values] bool isS3ReceiptHandle)
    {
        // arrange
        const string originalReceiptHandle = "originalReceiptHandle";
        var receiptHandle = isS3ReceiptHandle
            ? $"{SqsExtendedClientConstants.S3BucketNameMarker}{SqsExtendedClientConstants.S3BucketNameMarker}{SqsExtendedClientConstants.S3KeyMarker}{SqsExtendedClientConstants.S3KeyMarker}{originalReceiptHandle}"
            : originalReceiptHandle;

        var request = new ChangeMessageVisibilityRequest("url", receiptHandle, 120);

        // act
        await new AmazonSqsExtendedClient(SqsClientSub, ExtendedClientConfiguration)
            .ChangeMessageVisibilityAsync(request);

        //assert
        await SqsClientSub.Received(1).ChangeMessageVisibilityAsync(Arg.Is<ChangeMessageVisibilityRequest>(c =>
            c.QueueUrl == "url" && c.ReceiptHandle == originalReceiptHandle && c.VisibilityTimeout == 120));
    }
}