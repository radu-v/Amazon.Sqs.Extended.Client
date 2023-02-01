using System.Text;
using System.Text.Json;
using Amazon.S3.Model;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.SQS.Model;
using NSubstitute;

namespace Amazon.Sqs.Extended.Client.Tests;

[TestFixture]
public class ReceiveMessageAsyncTests : AmazonSqsExtendedClientTestsBase
{
    [Test]
    public async Task DoesNotReadFromS3WhenPayloadSizeIsSmall()
    {
        // arrange
        var receiveMessageResponse = new ReceiveMessageResponse
        {
            Messages = new List<Message> { new() }
        };

        SqsClientSub.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(receiveMessageResponse));

        // act
        await ExtendedSqsWithLargePayloadEnabled.ReceiveMessageAsync(new ReceiveMessageRequest(SqsQueueUrl));

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.Received(1).ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(r =>
                r.QueueUrl == SqsQueueUrl
                && r.MessageAttributeNames.Contains(SqsExtendedClientConstants.ReservedAttributeName)));

            S3Sub.DidNotReceiveGetObjectAsyncCallsWithAnyArgs();
        });
    }

    [Test]
    public async Task DoesNotReadFromS3WhenLargePayloadIsDisabled()
    {
        // arrange
        var receiveMessageResponse = new ReceiveMessageResponse
        {
            Messages = new List<Message> { new() }
        };

        SqsClientSub.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(receiveMessageResponse));

        // act
        await ExtendedSqsWithLargePayloadDisabled.ReceiveMessageAsync(new ReceiveMessageRequest(SqsQueueUrl));

        // assert
        Assert.Multiple(async () =>
        {
            await SqsClientSub.Received(1).ReceiveMessageAsync(Arg.Is<ReceiveMessageRequest>(r =>
                r.QueueUrl == SqsQueueUrl
                && !r.MessageAttributeNames.Contains(SqsExtendedClientConstants.ReservedAttributeName)));

            S3Sub.DidNotReceiveGetObjectAsyncCallsWithAnyArgs();
        });
    }

    [Test]
    public async Task ReadsFromS3WhenPayloadIsLarge()
    {
        var pointer = new PayloadS3Pointer(S3BucketName, S3Key);
        var message = new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { SqsExtendedClientConstants.ReservedAttributeName, new MessageAttributeValue() }
            },
            Body = JsonSerializer.Serialize(pointer, new JsonSerializerOptions { WriteIndented = false })
        };

        const string expectedMessage = "LargeMessage";

        var s3Object = new GetObjectResponse
        {
            BucketName = S3BucketName,
            ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(expectedMessage))
        };

        SqsClientSub.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        S3Sub.GetObjectAsync(Arg.Is<GetObjectRequest>(g => g.BucketName == S3BucketName && g.Key == S3Key),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(s3Object));

        S3Sub.GetObjectAsync(S3BucketName, S3Key, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(s3Object));

        var messageRequest = new ReceiveMessageRequest();
        var actualReceiveMessageResponse = await ExtendedSqsWithLargePayloadEnabled.ReceiveMessageAsync(messageRequest);
        var actualMessage = actualReceiveMessageResponse.Messages.First();

        Assert.Multiple(() =>
        {
            Assert.That(actualMessage.Body, Is.EqualTo(expectedMessage));
            Assert.That(actualMessage.MessageAttributes,
                Does.Not.ContainKey(SqsExtendedClientConstants.ReservedAttributeName));

            S3Sub.ReceivedGetObjectAsyncCalls(1, S3BucketName, S3Key);
        });
    }
}