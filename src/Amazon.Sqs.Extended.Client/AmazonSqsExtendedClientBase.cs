using System.Diagnostics.CodeAnalysis;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Amazon.Sqs.Extended.Client;

[ExcludeFromCodeCoverage(Justification = "Abstract proxy implementation")]
public abstract class AmazonSqsExtendedClientBase : IAmazonSQS
{
    readonly IAmazonSQS _amazonSqsToBeExtended;

    protected AmazonSqsExtendedClientBase(IAmazonSQS amazonSqsToBeExtended)
    {
        _amazonSqsToBeExtended = amazonSqsToBeExtended;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing) _amazonSqsToBeExtended.Dispose();
    }

    public virtual Task<Dictionary<string, string>> GetAttributesAsync(string queueUrl) =>
        _amazonSqsToBeExtended.GetAttributesAsync(queueUrl);

    public virtual Task SetAttributesAsync(string queueUrl, Dictionary<string, string> attributes) =>
        _amazonSqsToBeExtended.SetAttributesAsync(queueUrl, attributes);

    public IClientConfig Config => _amazonSqsToBeExtended.Config;

    public virtual Task<string> AuthorizeS3ToSendMessageAsync(string queueUrl, string bucket)
        => _amazonSqsToBeExtended.AuthorizeS3ToSendMessageAsync(queueUrl, bucket);

    public virtual Task<AddPermissionResponse> AddPermissionAsync(string queueUrl, string label,
        List<string> awsAccountIds, List<string> actions, CancellationToken cancellationToken = new())
        => AddPermissionAsync(new AddPermissionRequest(queueUrl, label, awsAccountIds, actions), cancellationToken);

    public virtual Task<AddPermissionResponse> AddPermissionAsync(AddPermissionRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.AddPermissionAsync(request, cancellationToken);

    public virtual Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(string queueUrl,
        string receiptHandle, int visibilityTimeout, CancellationToken cancellationToken = new())
        => ChangeMessageVisibilityAsync(
            new ChangeMessageVisibilityRequest(queueUrl, receiptHandle, visibilityTimeout), cancellationToken);

    public virtual Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(
        ChangeMessageVisibilityRequest request, CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ChangeMessageVisibilityAsync(request, cancellationToken);

    public virtual Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(string queueUrl,
        List<ChangeMessageVisibilityBatchRequestEntry> entries, CancellationToken cancellationToken = new())
        => ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest(queueUrl, entries),
            cancellationToken);

    public virtual Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(
        ChangeMessageVisibilityBatchRequest request, CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ChangeMessageVisibilityBatchAsync(request, cancellationToken);

    public virtual Task<CreateQueueResponse> CreateQueueAsync(string queueName,
        CancellationToken cancellationToken = new())
        => CreateQueueAsync(new CreateQueueRequest(queueName), cancellationToken);

    public virtual Task<CreateQueueResponse> CreateQueueAsync(CreateQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.CreateQueueAsync(request, cancellationToken);

    public virtual Task<DeleteMessageResponse> DeleteMessageAsync(string queueUrl, string receiptHandle,
        CancellationToken cancellationToken = new())
        => DeleteMessageAsync(new DeleteMessageRequest(queueUrl, receiptHandle), cancellationToken);

    public virtual Task<DeleteMessageResponse> DeleteMessageAsync(DeleteMessageRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.DeleteMessageAsync(request, cancellationToken);

    public virtual Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(string queueUrl,
        List<DeleteMessageBatchRequestEntry> entries, CancellationToken cancellationToken = new())
        => DeleteMessageBatchAsync(new DeleteMessageBatchRequest(queueUrl, entries), cancellationToken);

    public virtual Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(DeleteMessageBatchRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.DeleteMessageBatchAsync(request, cancellationToken);

    public virtual Task<DeleteQueueResponse> DeleteQueueAsync(string queueUrl,
        CancellationToken cancellationToken = new())
        => DeleteQueueAsync(new DeleteQueueRequest(queueUrl), cancellationToken);

    public virtual Task<DeleteQueueResponse> DeleteQueueAsync(DeleteQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.DeleteQueueAsync(request, cancellationToken);

    public virtual Task<GetQueueAttributesResponse> GetQueueAttributesAsync(string queueUrl,
        List<string> attributeNames, CancellationToken cancellationToken = new())
        => GetQueueAttributesAsync(new GetQueueAttributesRequest(queueUrl, attributeNames), cancellationToken);

    public virtual Task<GetQueueAttributesResponse> GetQueueAttributesAsync(GetQueueAttributesRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.GetQueueAttributesAsync(request, cancellationToken);

    public virtual Task<GetQueueUrlResponse> GetQueueUrlAsync(string queueName,
        CancellationToken cancellationToken = new())
        => GetQueueUrlAsync(new GetQueueUrlRequest(queueName), cancellationToken);

    public virtual Task<GetQueueUrlResponse> GetQueueUrlAsync(GetQueueUrlRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.GetQueueUrlAsync(request, cancellationToken);

    public virtual Task<ListDeadLetterSourceQueuesResponse> ListDeadLetterSourceQueuesAsync(
        ListDeadLetterSourceQueuesRequest request, CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ListDeadLetterSourceQueuesAsync(request, cancellationToken);

    public virtual Task<ListQueuesResponse> ListQueuesAsync(string queueNamePrefix,
        CancellationToken cancellationToken = new())
        => ListQueuesAsync(new ListQueuesRequest(queueNamePrefix), cancellationToken);

    public virtual Task<ListQueuesResponse> ListQueuesAsync(ListQueuesRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ListQueuesAsync(request, cancellationToken);

    public virtual Task<ListQueueTagsResponse> ListQueueTagsAsync(ListQueueTagsRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ListQueueTagsAsync(request, cancellationToken);

    public virtual Task<PurgeQueueResponse> PurgeQueueAsync(string queueUrl,
        CancellationToken cancellationToken = new())
        => PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = queueUrl }, cancellationToken);

    public virtual Task<PurgeQueueResponse> PurgeQueueAsync(PurgeQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.PurgeQueueAsync(request, cancellationToken);

    public virtual Task<ReceiveMessageResponse> ReceiveMessageAsync(string queueUrl,
        CancellationToken cancellationToken = new())
        => ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl), cancellationToken);

    public virtual Task<ReceiveMessageResponse> ReceiveMessageAsync(ReceiveMessageRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ReceiveMessageAsync(request, cancellationToken);

    public virtual Task<RemovePermissionResponse> RemovePermissionAsync(string queueUrl, string label,
        CancellationToken cancellationToken = new())
        => RemovePermissionAsync(new RemovePermissionRequest(queueUrl, label), cancellationToken);

    public virtual Task<RemovePermissionResponse> RemovePermissionAsync(RemovePermissionRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.RemovePermissionAsync(request, cancellationToken);

    public virtual Task<SendMessageResponse> SendMessageAsync(string queueUrl, string messageBody,
        CancellationToken cancellationToken = new())
        => SendMessageAsync(new SendMessageRequest(queueUrl, messageBody), cancellationToken);

    public virtual Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.SendMessageAsync(request, cancellationToken);

    public virtual Task<SendMessageBatchResponse> SendMessageBatchAsync(string queueUrl,
        List<SendMessageBatchRequestEntry> entries, CancellationToken cancellationToken = new())
        => SendMessageBatchAsync(new SendMessageBatchRequest(queueUrl, entries), cancellationToken);

    public virtual Task<SendMessageBatchResponse> SendMessageBatchAsync(SendMessageBatchRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.SendMessageBatchAsync(request, cancellationToken);

    public virtual Task<SetQueueAttributesResponse> SetQueueAttributesAsync(string queueUrl,
        Dictionary<string, string> attributes, CancellationToken cancellationToken = new())
        => SetQueueAttributesAsync(new SetQueueAttributesRequest(queueUrl, attributes), cancellationToken);

    public virtual Task<SetQueueAttributesResponse> SetQueueAttributesAsync(SetQueueAttributesRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.SetQueueAttributesAsync(request, cancellationToken);

    public virtual Task<TagQueueResponse> TagQueueAsync(TagQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.TagQueueAsync(request, cancellationToken);

    public virtual Task<UntagQueueResponse> UntagQueueAsync(UntagQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.UntagQueueAsync(request, cancellationToken);

    public ISQSPaginatorFactory Paginators => _amazonSqsToBeExtended.Paginators;
}