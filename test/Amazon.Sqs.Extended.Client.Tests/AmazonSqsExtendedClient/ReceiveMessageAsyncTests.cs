using Amazon.Sqs.Extended.Client.Models;
using Amazon.SQS.Model;
using NSubstitute;
using System.Text.Json;

namespace Amazon.Sqs.Extended.Client.Tests.AmazonSqsExtendedClient;

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

            await PayloadStoreSub.DidNotReceiveWithAnyArgs().ReadPayloadAsync(default);
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

            await PayloadStoreSub.DidNotReceiveWithAnyArgs().ReadPayloadAsync(default);
        });
    }

    [Test]
    public async Task ReadsFromS3WhenPayloadIsLarge()
    {
        // arrange
        var pointer = new PayloadPointer(S3BucketName, S3Key);
        var message = new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { SqsExtendedClientConstants.ReservedAttributeName, new MessageAttributeValue() }
            },
            Body = JsonSerializer.Serialize(pointer, new JsonSerializerOptions { WriteIndented = false })
        };

        const string expectedMessage = "LargeMessage";

        SqsClientSub.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        PayloadStoreSub.ReadPayloadAsync(pointer, Arg.Any<CancellationToken>())
            .Returns(expectedMessage);

        var messageRequest = new ReceiveMessageRequest();
        
        // act
        var actualReceiveMessageResponse = await ExtendedSqsWithLargePayloadEnabled.ReceiveMessageAsync(messageRequest);
        var actualMessage = actualReceiveMessageResponse.Messages.First<Message>();

        // assert
        Assert.Multiple(async () =>
        {
            Assert.That(actualMessage.Body, Is.EqualTo(expectedMessage));
            Assert.That(actualMessage.MessageAttributes,
                Does.Not.ContainKey(SqsExtendedClientConstants.ReservedAttributeName));

            await PayloadStoreSub.Received(1).ReadPayloadAsync(pointer, Arg.Any<CancellationToken>()); 
        });
    }
    
    [Test]
    public async Task DoesNotReadFromS3WhenPayloadIsNotS3PayloadPointer()
    {
        // arrange
        const string expectedMessage = "Not S3 payload";
        var message = new Message
        {
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                { SqsExtendedClientConstants.ReservedAttributeName, new MessageAttributeValue() }
            },
            Body = expectedMessage
        };

        SqsClientSub.ReceiveMessageAsync(Arg.Any<ReceiveMessageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ReceiveMessageResponse { Messages = new List<Message> { message } });

        var messageRequest = new ReceiveMessageRequest();
        
        // act
        var actualReceiveMessageResponse = await ExtendedSqsWithLargePayloadEnabled.ReceiveMessageAsync(messageRequest);
        var actualMessage = actualReceiveMessageResponse.Messages.First<Message>();

        // assert
        Assert.Multiple(() =>
        {
            Assert.That(actualMessage.Body, Is.EqualTo(expectedMessage));
            PayloadStoreSub.DidNotReceiveWithAnyArgs().ReadPayloadAsync(default);
        });
    }
}