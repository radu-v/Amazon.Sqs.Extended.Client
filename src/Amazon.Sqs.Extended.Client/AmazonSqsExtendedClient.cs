using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace Amazon.Sqs.Extended.Client;

public class AmazonSqsExtendedClient : AmazonSqsExtendedClientBase
{
    readonly ExtendedClientConfiguration _extendedClientConfiguration;
    readonly IS3KeyProvider _s3KeyProvider;
    readonly ILogger<AmazonSqsExtendedClient> _logger;

    public AmazonSqsExtendedClient(IAmazonSQS amazonSqsToBeExtended, ILogger<AmazonSqsExtendedClient> logger)
        : this(amazonSqsToBeExtended, new ExtendedClientConfiguration(), new DefaultS3KeyProvider(), logger)
    {
    }

    public AmazonSqsExtendedClient(IAmazonSQS amazonSqsToBeExtended,
        ExtendedClientConfiguration extendedClientConfiguration, ILogger<AmazonSqsExtendedClient> logger)
        : this(amazonSqsToBeExtended, extendedClientConfiguration, new DefaultS3KeyProvider(), logger)
    {
    }

    public AmazonSqsExtendedClient(IAmazonSQS amazonSqsToBeExtended,
        ExtendedClientConfiguration extendedClientConfiguration, IS3KeyProvider s3KeyProvider,
        ILogger<AmazonSqsExtendedClient> logger) : base(
        amazonSqsToBeExtended)
    {
        _extendedClientConfiguration = extendedClientConfiguration;
        _s3KeyProvider = s3KeyProvider;
        _logger = logger;
    }

    public override async Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(
        ChangeMessageVisibilityRequest request, CancellationToken cancellationToken = new())
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

    public override async Task<DeleteMessageResponse> DeleteMessageAsync(DeleteMessageRequest request,
        CancellationToken cancellationToken = new())
    {
        if (_extendedClientConfiguration is { LargePayloadSupport: true, CleanupS3Payload: true } &&
            IsS3ReceiptHandle(request.ReceiptHandle))
        {
            var payloadS3Pointer = GetMessagePointerFromS3ReceiptHandle(request.ReceiptHandle);
            await DeletePayloadFromS3Async(payloadS3Pointer, cancellationToken);
        }

        request.ReceiptHandle = GetOriginalReceiptHandle(request.ReceiptHandle);

        return await base.DeleteMessageAsync(request, cancellationToken);
    }

    public override async Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest request,
        CancellationToken cancellationToken = new())
    {
        foreach (var entry in request.Entries)
        {
            if (_extendedClientConfiguration is { LargePayloadSupport: true, CleanupS3Payload: true } &&
                IsS3ReceiptHandle(entry.ReceiptHandle))
            {
                var payloadS3Pointer = GetMessagePointerFromS3ReceiptHandle(entry.ReceiptHandle);
                await DeletePayloadFromS3Async(payloadS3Pointer, cancellationToken);
            }

            entry.ReceiptHandle = GetOriginalReceiptHandle(entry.ReceiptHandle);
        }

        return await base.DeleteMessageBatchAsync(request, cancellationToken);
    }

    public override async Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request,
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

            message.Body = await ReadPayloadFromS3Async(payloadS3Pointer, cancellationToken);
            message.ReceiptHandle = EmbedS3PointerInReceiptHandle(message.ReceiptHandle, payloadS3Pointer);
            message.MessageAttributes.Remove(SqsExtendedClientConstants.ReservedAttributeName);
        }

        return receiveMessageResponse;
    }

    public override async Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request,
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
            request = await WritePayloadToS3Async(request, cancellationToken);
        }

        return await base.SendMessageAsync(request, cancellationToken);
    }

    public override async Task<SendMessageBatchResponse> SendMessageBatchAsync(SendMessageBatchRequest request,
        CancellationToken cancellationToken = new())
    {
        if (!_extendedClientConfiguration.LargePayloadSupport)
        {
            return await base.SendMessageBatchAsync(request, cancellationToken);
        }

        for (var i = 0; i < request.Entries.Count; i++)
        {
            var entry = request.Entries[i];
            if (_extendedClientConfiguration.AlwaysThroughS3 || IsLarge(entry))
            {
                request.Entries[i] = await WritePayloadToS3Async(entry, cancellationToken);
            }
        }

        return await base.SendMessageBatchAsync(request, cancellationToken);
    }

    internal void CheckMessageAttributes(IReadOnlyDictionary<string, MessageAttributeValue> messageAttributes)
    {
        var msgAttributesSize = GetMessageAttributesSize(messageAttributes);
        if (msgAttributesSize > _extendedClientConfiguration.PayloadSizeThreshold)
        {
            throw new AmazonClientException(
                $"Total size of Message attributes is {msgAttributesSize} bytes which is larger than the threshold of {_extendedClientConfiguration.PayloadSizeThreshold} Bytes. Consider including the payload in the message body instead of message attributes.");
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

    internal static long GetMessageAttributesSize(IReadOnlyDictionary<string, MessageAttributeValue> messageAttributes)
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

    static PayloadS3Pointer GetMessagePointerFromS3ReceiptHandle(string receiptHandle)
    {
        var s3MsgBucketName =
            GetFromReceiptHandleByMarker(receiptHandle, SqsExtendedClientConstants.S3BucketNameMarker);
        var s3MsgKey = GetFromReceiptHandleByMarker(receiptHandle, SqsExtendedClientConstants.S3KeyMarker);

        return new PayloadS3Pointer(s3MsgBucketName, s3MsgKey);
    }

    internal bool TryGetMessagePointerFromMessageBody(string messageBody, out PayloadS3Pointer payloadS3Pointer)
    {
        try
        {
            payloadS3Pointer = JsonSerializer.Deserialize<PayloadS3Pointer>(messageBody)!;
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to deserialize payload S3 pointer");

            payloadS3Pointer = default;
            return false;
        }
    }

    internal static string EmbedS3PointerInReceiptHandle(string receiptHandle, PayloadS3Pointer payloadS3Pointer)
    {
        return new StringBuilder(SqsExtendedClientConstants.S3BucketNameMarker)
            .Append(payloadS3Pointer.S3BucketName)
            .Append(SqsExtendedClientConstants.S3BucketNameMarker)
            .Append(SqsExtendedClientConstants.S3KeyMarker)
            .Append(payloadS3Pointer.S3Key)
            .Append(SqsExtendedClientConstants.S3KeyMarker)
            .Append(receiptHandle)
            .ToString();
    }

    internal static string GetFromReceiptHandleByMarker(string receiptHandle, string marker)
    {
        var valueStart = receiptHandle.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var valueEnd = receiptHandle.IndexOf(marker, valueStart, StringComparison.Ordinal);
        return receiptHandle.Substring(valueStart, valueEnd - valueStart);
    }

    async Task DeletePayloadFromS3Async(PayloadS3Pointer payloadS3Pointer, CancellationToken cancellationToken = new())
    {
        const string failedToDeleteMessage = "Failed to delete the S3 object which contains the payload";

        try
        {
            await _extendedClientConfiguration.S3!.DeleteObjectAsync(payloadS3Pointer.S3BucketName,
                payloadS3Pointer.S3Key, cancellationToken);
        }
        catch (AmazonServiceException e)
        {
            _logger.LogError(e, failedToDeleteMessage);
            throw new AmazonClientException(failedToDeleteMessage, e);
        }
        catch (AmazonClientException e)
        {
            _logger.LogError(e, failedToDeleteMessage);
            throw new AmazonClientException(failedToDeleteMessage, e);
        }
    }

    async Task<string> ReadPayloadFromS3Async(PayloadS3Pointer payloadS3Pointer,
        CancellationToken cancellationToken = new())
    {
        const string failedToReadMessage = "Failed to get the S3 object which contains the payload";

        try
        {
            using var response = await _extendedClientConfiguration.S3!.GetObjectAsync(payloadS3Pointer.S3BucketName,
                payloadS3Pointer.S3Key,
                cancellationToken);
            var stream = new StreamReader(response.ResponseStream);
            return await stream.ReadToEndAsync();
        }
        catch (AmazonServiceException e)
        {
            _logger.LogError(e, failedToReadMessage);
            throw new AmazonClientException(failedToReadMessage, e);
        }
        catch (AmazonClientException e)
        {
            _logger.LogError(e, failedToReadMessage);
            throw new AmazonClientException(failedToReadMessage, e);
        }
    }

    async Task<SendMessageRequest> WritePayloadToS3Async(SendMessageRequest request,
        CancellationToken cancellationToken = new())
    {
        var (updatedMessageAttributes, updatedMessageBody) =
            await WritePayloadToS3Async(request.MessageAttributes, request.MessageBody, cancellationToken);

        request.MessageAttributes = updatedMessageAttributes;
        request.MessageBody = updatedMessageBody;

        return request;
    }

    async Task<SendMessageBatchRequestEntry> WritePayloadToS3Async(SendMessageBatchRequestEntry request,
        CancellationToken cancellationToken = new())
    {
        var (updatedMessageAttributes, updatedMessageBody) =
            await WritePayloadToS3Async(request.MessageAttributes, request.MessageBody, cancellationToken);

        request.MessageAttributes = updatedMessageAttributes;
        request.MessageBody = updatedMessageBody;

        return request;
    }

    async Task<(Dictionary<string, MessageAttributeValue> updatedMessageAttributes, string updatedMessageBody)>
        WritePayloadToS3Async(IReadOnlyDictionary<string, MessageAttributeValue> messageAttributes, string messageBody,
            CancellationToken cancellationToken)
    {
        CheckMessageAttributes(messageAttributes);

        var messageContentSize = Encoding.UTF8.GetByteCount(messageBody);

        var updatedMessageAttributes = messageAttributes.WithExtendedPayloadSize(messageContentSize);

        var largeMessagePointer = await StoreOriginalPayloadAsync(messageBody, cancellationToken);
        var updatedMessageBody =
            JsonSerializer.Serialize(largeMessagePointer, new JsonSerializerOptions { WriteIndented = false });
        return (updatedMessageAttributes, updatedMessageBody);
    }

    async Task<PayloadS3Pointer> StoreOriginalPayloadAsync(string messageBody,
        CancellationToken cancellationToken = new())
    {
        var s3Key = _s3KeyProvider.GenerateKey();
        return await StoreOriginalPayloadAsync(messageBody, s3Key, cancellationToken);
    }

    async Task<PayloadS3Pointer> StoreOriginalPayloadAsync(string messageBody, string s3Key,
        CancellationToken cancellationToken = new())
    {
        const string failedToWriteMessage = "Failed to store the message content in an S3 object";

        var request = new PutObjectRequest
        {
            Key = s3Key,
            ContentBody = messageBody,
            BucketName = _extendedClientConfiguration.S3BucketName,
            CannedACL = _extendedClientConfiguration.S3CannedAcl
        };

        try
        {
            await _extendedClientConfiguration.S3!.PutObjectAsync(request, cancellationToken);
            return new PayloadS3Pointer(_extendedClientConfiguration.S3BucketName!, s3Key);
        }
        catch (AmazonServiceException e)
        {
            _logger.LogError(e, failedToWriteMessage);
            throw new AmazonClientException(failedToWriteMessage, e);
        }
        catch (AmazonClientException e)
        {
            _logger.LogError(e, failedToWriteMessage);
            throw new AmazonClientException(failedToWriteMessage, e);
        }
    }
}