using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amazon.Sqs.Extended.Client;

public sealed class AmazonSqsExtendedClient : AmazonSqsExtendedClientBase
{
    readonly ExtendedClientConfiguration _extendedClientConfiguration;
    readonly IPayloadStore _payloadStore;
    readonly ILogger<AmazonSqsExtendedClient> _logger;

    public AmazonSqsExtendedClient(
        IAmazonSQS amazonSqsToBeExtended,
        IPayloadStore payloadStore,
        IOptions<ExtendedClientConfiguration> extendedClientConfiguration,
        ILogger<AmazonSqsExtendedClient> logger) : base(
        amazonSqsToBeExtended)
    {
        _payloadStore = payloadStore;
        _extendedClientConfiguration = extendedClientConfiguration.Value;
        _logger = logger;
    }

    public override async Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(
        ChangeMessageVisibilityRequest request,
        CancellationToken cancellationToken = new())
    {
        request.ReceiptHandle = IsS3ReceiptHandle(request.ReceiptHandle)
            ? GetOriginalReceiptHandle(request.ReceiptHandle)
            : request.ReceiptHandle;

        return await base.ChangeMessageVisibilityAsync(request, cancellationToken);
    }

    public override async Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(
        ChangeMessageVisibilityBatchRequest request,
        CancellationToken cancellationToken = new())
    {
        foreach (var entry in request.Entries)
        {
            entry.ReceiptHandle = IsS3ReceiptHandle(entry.ReceiptHandle)
                ? GetOriginalReceiptHandle(entry.ReceiptHandle)
                : entry.ReceiptHandle;
        }

        return await base.ChangeMessageVisibilityBatchAsync(request, cancellationToken);
    }

    public override async Task<DeleteMessageResponse> DeleteMessageAsync(
        DeleteMessageRequest request,
        CancellationToken cancellationToken = new())
    {
        if (_extendedClientConfiguration is {LargePayloadSupport: true, CleanupPayload: true} && IsS3ReceiptHandle(request.ReceiptHandle))
        {
            var payloadS3Pointer = GetMessagePointerFromS3ReceiptHandle(request.ReceiptHandle);
            await _payloadStore.DeletePayloadAsync(payloadS3Pointer, cancellationToken);
        }

        request.ReceiptHandle = GetOriginalReceiptHandle(request.ReceiptHandle);

        return await base.DeleteMessageAsync(request, cancellationToken);
    }

    public override async Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(
        DeleteMessageBatchRequest request,
        CancellationToken cancellationToken = new())
    {
        foreach (var entry in request.Entries)
        {
            if (_extendedClientConfiguration is {LargePayloadSupport: true, CleanupPayload: true} && IsS3ReceiptHandle(entry.ReceiptHandle))
            {
                var payloadS3Pointer = GetMessagePointerFromS3ReceiptHandle(entry.ReceiptHandle);
                await _payloadStore.DeletePayloadAsync(payloadS3Pointer, cancellationToken);
            }

            entry.ReceiptHandle = GetOriginalReceiptHandle(entry.ReceiptHandle);
        }

        return await base.DeleteMessageBatchAsync(request, cancellationToken);
    }

    public override async Task<ReceiveMessageResponse> ReceiveMessageAsync(
        ReceiveMessageRequest request,
        CancellationToken cancellationToken = new())
    {
        if (!_extendedClientConfiguration.LargePayloadSupport)
        {
            return await base.ReceiveMessageAsync(request, cancellationToken);
        }

        if (!request.MessageAttributeNames.Contains(SqsExtendedClientConstants.ReservedAttributeName))
        {
            request.MessageAttributeNames.Add(SqsExtendedClientConstants.ReservedAttributeName);
        }

        var receiveMessageResponse = await base.ReceiveMessageAsync(request, cancellationToken);
        foreach (var message in receiveMessageResponse.Messages.Where(message =>
                     message.MessageAttributes.ContainsKey(SqsExtendedClientConstants.ReservedAttributeName)))
        {
            if (!TryGetMessagePointerFromMessageBody(message.Body, out var payloadS3Pointer))
                continue;

            message.Body = await _payloadStore.ReadPayloadAsync(payloadS3Pointer, cancellationToken);
            message.ReceiptHandle = EmbedS3PointerInReceiptHandle(message.ReceiptHandle, payloadS3Pointer);
            message.MessageAttributes.Remove(SqsExtendedClientConstants.ReservedAttributeName);
        }

        return receiveMessageResponse;
    }

    public override async Task<SendMessageResponse> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = new())
    {
        if (string.IsNullOrWhiteSpace(request.MessageBody))
        {
            throw new AmazonClientException("MessageBody cannot be null or empty");
        }

        if (!_extendedClientConfiguration.LargePayloadSupport)
        {
            return await base.SendMessageAsync(request, cancellationToken);
        }

        if (_extendedClientConfiguration.AlwaysThroughS3 || IsLarge(request))
        {
            (request.MessageAttributes, request.MessageBody) =
                await WritePayloadToS3Async(request.MessageAttributes, request.MessageBody, cancellationToken);
        }

        return await base.SendMessageAsync(request, cancellationToken);
    }

    public override async Task<SendMessageBatchResponse> SendMessageBatchAsync(
        SendMessageBatchRequest request,
        CancellationToken cancellationToken = new())
    {
        if (!_extendedClientConfiguration.LargePayloadSupport)
        {
            return await base.SendMessageBatchAsync(request, cancellationToken);
        }

        for (var i = 0; i < request.Entries.Count; i++)
        {
            var entry = request.Entries[i];
            if (!_extendedClientConfiguration.AlwaysThroughS3 && !IsLarge(entry))
                continue;
            
            (entry.MessageAttributes, entry.MessageBody) =
                await WritePayloadToS3Async(entry.MessageAttributes, entry.MessageBody, cancellationToken);

            request.Entries[i] = entry;
        }

        return await base.SendMessageBatchAsync(request, cancellationToken);
    }

    static void CheckMessageAttributes(IDictionary<string, MessageAttributeValue> messageAttributes, long payloadSizeThreshold)
    {
        var msgAttributesSize = GetMessageAttributesSize(messageAttributes);
        if (msgAttributesSize > payloadSizeThreshold)
        {
            throw new AmazonClientException(
                $"Total size of Message attributes is {msgAttributesSize} bytes which is larger than the threshold of {payloadSizeThreshold} Bytes. Consider including the payload in the message body instead of message attributes.");
        }

        var messageAttributesNum = messageAttributes.Count;
        if (messageAttributesNum > SqsExtendedClientConstants.MaxAllowedAttributes)
        {
            throw new AmazonClientException(
                $"Number of message attributes [{messageAttributesNum}] exceeds the maximum allowed for large-payload messages [{SqsExtendedClientConstants.MaxAllowedAttributes}].");
        }

        if (messageAttributes.ContainsKey(SqsExtendedClientConstants.ReservedAttributeName))
        {
            throw new AmazonClientException(
                $"Message attribute name {SqsExtendedClientConstants.ReservedAttributeName} is reserved for use by SQS extended client.");
        }
    }

    bool IsLarge(SendMessageRequest request)
    {
        var msgAttributesSize = GetMessageAttributesSize(request.MessageAttributes);
        var msgBodySize = Encoding.UTF8.GetByteCount(request.MessageBody);
        var totalMsgSize = msgAttributesSize + msgBodySize;
        return totalMsgSize > _extendedClientConfiguration.PayloadSizeThreshold;
    }

    bool IsLarge(SendMessageBatchRequestEntry request)
    {
        var msgAttributesSize = GetMessageAttributesSize(request.MessageAttributes);
        var msgBodySize = Encoding.UTF8.GetByteCount(request.MessageBody);
        var totalMsgSize = msgAttributesSize + msgBodySize;
        return totalMsgSize > _extendedClientConfiguration.PayloadSizeThreshold;
    }

    static long GetMessageAttributesSize(IDictionary<string, MessageAttributeValue> messageAttributes)
    {
        var totalMessageAttributesSize = 0L;
        foreach (var attribute in messageAttributes)
        {
            totalMessageAttributesSize += Encoding.UTF8.GetByteCount(attribute.Key);

            if (!string.IsNullOrEmpty(attribute.Value.DataType))
            {
                totalMessageAttributesSize += Encoding.UTF8.GetByteCount(attribute.Value.DataType);
            }

            if (!string.IsNullOrEmpty(attribute.Value.StringValue))
            {
                totalMessageAttributesSize += Encoding.UTF8.GetByteCount(attribute.Value.StringValue);
            }

            if (attribute.Value.BinaryValue is not null)
            {
                totalMessageAttributesSize += attribute.Value.BinaryValue.Length;
            }
        }

        return totalMessageAttributesSize;
    }

    static bool IsS3ReceiptHandle(string receiptHandle)
    {
        return receiptHandle.Contains(SqsExtendedClientConstants.S3BucketNameMarker)
            && receiptHandle.Contains(SqsExtendedClientConstants.S3KeyMarker);
    }

    static string GetOriginalReceiptHandle(string receiptHandle)
    {
        var s3KeyMarkerFirst =
            receiptHandle.IndexOf(SqsExtendedClientConstants.S3KeyMarker, StringComparison.Ordinal);

        var s3KeyMarkerSecond = receiptHandle.IndexOf(SqsExtendedClientConstants.S3KeyMarker, s3KeyMarkerFirst + 1,
            StringComparison.Ordinal);

        var span = receiptHandle.AsSpan();
        return span[(s3KeyMarkerSecond + SqsExtendedClientConstants.S3KeyMarker.Length)..].ToString();
    }

    static PayloadPointer GetMessagePointerFromS3ReceiptHandle(string receiptHandle)
    {
        var s3MsgBucketName =
            GetFromReceiptHandleByMarker(receiptHandle, SqsExtendedClientConstants.S3BucketNameMarker);
        var s3MsgKey = GetFromReceiptHandleByMarker(receiptHandle, SqsExtendedClientConstants.S3KeyMarker);

        return new PayloadPointer(s3MsgBucketName, s3MsgKey);
    }

    bool TryGetMessagePointerFromMessageBody(string messageBody, out PayloadPointer payloadPointer)
    {
        try
        {
            payloadPointer = JsonSerializer.Deserialize<PayloadPointer>(messageBody)!;
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize payload S3 pointer");

            payloadPointer = default;
            return false;
        }
    }

    static string EmbedS3PointerInReceiptHandle(string receiptHandle, PayloadPointer payloadPointer)
    {
        return new StringBuilder(SqsExtendedClientConstants.S3BucketNameMarker)
            .Append(payloadPointer.BucketName)
            .Append(SqsExtendedClientConstants.S3BucketNameMarker)
            .Append(SqsExtendedClientConstants.S3KeyMarker)
            .Append(payloadPointer.Key)
            .Append(SqsExtendedClientConstants.S3KeyMarker)
            .Append(receiptHandle)
            .ToString();
    }

    static string GetFromReceiptHandleByMarker(string receiptHandle, string marker)
    {
        var valueStart = receiptHandle.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var valueEnd = receiptHandle.IndexOf(marker, valueStart, StringComparison.Ordinal);
        return receiptHandle.Substring(valueStart, valueEnd - valueStart);
    }

    async Task<(Dictionary<string, MessageAttributeValue> updatedMessageAttributes, string updatedMessageBody)>
        WritePayloadToS3Async(
            IDictionary<string, MessageAttributeValue> messageAttributes,
            string messageBody,
            CancellationToken cancellationToken)
    {
        CheckMessageAttributes(messageAttributes, _extendedClientConfiguration.PayloadSizeThreshold);

        var messageContentSize = Encoding.UTF8.GetByteCount(messageBody);

        var updatedMessageAttributes = messageAttributes.WithExtendedPayloadSize(messageContentSize);

        var largeMessagePointer = await _payloadStore.StorePayloadAsync(messageBody, cancellationToken);
        var updatedMessageBody =
            JsonSerializer.Serialize(largeMessagePointer, new JsonSerializerOptions {WriteIndented = false});
        return (updatedMessageAttributes, updatedMessageBody);
    }
}
