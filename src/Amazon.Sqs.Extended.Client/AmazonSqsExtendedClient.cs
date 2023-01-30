using System.Text;
using System.Text.Json;
using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.SQS;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using Amazon.SQS.Model;

namespace Amazon.Sqs.Extended.Client;

public class AmazonSqsExtendedClient : AmazonSqsExtendedClientBase
{
    readonly ExtendedClientConfiguration _extendedClientConfiguration;
    readonly IS3KeyProvider _s3KeyProvider;

    public AmazonSqsExtendedClient(IAmazonSQS amazonSqsToBeExtended)
        : this(amazonSqsToBeExtended, new ExtendedClientConfiguration(), new DefaultS3KeyProvider())
    {
    }

    public AmazonSqsExtendedClient(IAmazonSQS amazonSqsToBeExtended,
        ExtendedClientConfiguration extendedClientConfiguration)
        : this(amazonSqsToBeExtended, extendedClientConfiguration, new DefaultS3KeyProvider())
    {
    }

    public AmazonSqsExtendedClient(IAmazonSQS amazonSqsToBeExtended,
        ExtendedClientConfiguration extendedClientConfiguration, IS3KeyProvider s3KeyProvider) : base(
        amazonSqsToBeExtended)
    {
        _extendedClientConfiguration = extendedClientConfiguration;
        _s3KeyProvider = s3KeyProvider;
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
        if (!_extendedClientConfiguration.LargePayloadSupport || !IsS3ReceiptHandle(request.ReceiptHandle))
        {
            return await base.DeleteMessageAsync(request, cancellationToken);
        }

        if (_extendedClientConfiguration.CleanupS3Payload)
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
        if (!_extendedClientConfiguration.LargePayloadSupport)
        {
            return await base.DeleteMessageBatchAsync(request, cancellationToken);
        }

        foreach (var entry in request.Entries)
        {
            if (_extendedClientConfiguration.CleanupS3Payload && IsS3ReceiptHandle(entry.ReceiptHandle))
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
        foreach (var message in receiveMessageResponse.Messages)
        {
            if (!message.MessageAttributes.ContainsKey(SqsExtendedClientConstants.ReservedAttributeName))
                continue;

            var payloadS3Pointer = GetMessagePointerFromMessageBody(message.Body);
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

    internal static PayloadS3Pointer GetMessagePointerFromMessageBody(string messageBody)
    {
        try
        {
            return JsonSerializer.Deserialize<PayloadS3Pointer>(messageBody)!;
        }
        catch (Exception e)
        {
            throw new AmazonClientException(
                "Failed to read the S3 object pointer from an SQS message. Message was not received.", e);
        }
    }

    internal static string EmbedS3PointerInReceiptHandle(string receiptHandle, PayloadS3Pointer payloadS3Pointer)
    {
        var receiptHandleBuilder = new StringBuilder(SqsExtendedClientConstants.S3BucketNameMarker);
        receiptHandleBuilder.Append(payloadS3Pointer.S3BucketName);
        receiptHandleBuilder.Append(SqsExtendedClientConstants.S3BucketNameMarker);
        receiptHandleBuilder.Append(SqsExtendedClientConstants.S3KeyMarker);
        receiptHandleBuilder.Append(payloadS3Pointer.S3Key);
        receiptHandleBuilder.Append(SqsExtendedClientConstants.S3KeyMarker);
        receiptHandleBuilder.Append(receiptHandle);

        return receiptHandleBuilder.ToString();
    }

    internal static string GetFromReceiptHandleByMarker(string receiptHandle, string marker)
    {
        var valueStart = receiptHandle.IndexOf(marker, StringComparison.Ordinal) + marker.Length;
        var valueEnd = receiptHandle.IndexOf(marker, valueStart + 1, StringComparison.Ordinal);
        return receiptHandle.Substring(valueStart, valueEnd - valueStart);
    }

    async Task DeletePayloadFromS3Async(PayloadS3Pointer payloadS3Pointer, CancellationToken cancellationToken = new())
    {
        try
        {
            await _extendedClientConfiguration.S3!.DeleteObjectAsync(payloadS3Pointer.S3BucketName,
                payloadS3Pointer.S3Key, cancellationToken);
        }
        catch (AmazonServiceException e)
        {
            throw new AmazonClientException(
                "Failed to delete the S3 object which contains the SQS message payload.",
                e);
        }
        catch (AmazonClientException e)
        {
            throw new AmazonClientException(
                "Failed to delete the S3 object which contains the SQS message payload.",
                e);
        }
    }

    async Task<string> ReadPayloadFromS3Async(PayloadS3Pointer payloadS3Pointer,
        CancellationToken cancellationToken = new())
    {
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
            throw new AmazonClientException(
                "Failed to get the S3 object which contains the payload.",
                e);
        }
        catch (AmazonClientException e)
        {
            throw new AmazonClientException(
                "Failed to get the S3 object which contains the payload.",
                e);
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
            throw new AmazonClientException(
                "Failed to store the message content in an S3 object.",
                e);
        }
        catch (AmazonClientException e)
        {
            throw new AmazonClientException(
                "Failed to store the message content in an S3 object.",
                e);
        }
    }
}