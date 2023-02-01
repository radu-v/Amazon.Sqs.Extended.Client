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
        var receiptHandle = GenerateReceiptHandle(isS3ReceiptHandle, originalReceiptHandle);

        var request = new ChangeMessageVisibilityRequest(SqsQueueUrl, receiptHandle, 120);

        // act
        await new AmazonSqsExtendedClient(SqsClientSub, ExtendedClientConfiguration, DummyLogger)
            .ChangeMessageVisibilityAsync(request);

        //assert
        await SqsClientSub.Received(1).ChangeMessageVisibilityAsync(Arg.Is<ChangeMessageVisibilityRequest>(c =>
            c.QueueUrl == SqsQueueUrl && c.ReceiptHandle == originalReceiptHandle && c.VisibilityTimeout == 120));
    }
}