using System.Diagnostics.CodeAnalysis;
using Amazon.Runtime;
using Amazon.Runtime.Endpoints;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace Amazon.Sqs.Extended.Client;

[ExcludeFromCodeCoverage]
public abstract class AmazonSqsExtendedClientBase : IAmazonSQS
{
    private readonly IAmazonSQS _amazonSqsToBeExtended;

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

#if NETFRAMEWORK
    public Dictionary<string, string> GetAttributes(string queueUrl) => _amazonSqsToBeExtended.GetAttributes(queueUrl);
#endif

    public virtual Task<Dictionary<string, string>> GetAttributesAsync(string queueUrl)
        => _amazonSqsToBeExtended.GetAttributesAsync(queueUrl);

#if NETFRAMEWORK
    public void SetAttributes(string queueUrl, Dictionary<string, string> attributes)
        => _amazonSqsToBeExtended.SetAttributes(queueUrl, attributes);
#endif

    public virtual Task SetAttributesAsync(string queueUrl, Dictionary<string, string> attributes)
        => _amazonSqsToBeExtended.SetAttributesAsync(queueUrl, attributes);

    public IClientConfig Config => _amazonSqsToBeExtended.Config;

#if NETFRAMEWORK
    public string AuthorizeS3ToSendMessage(string queueUrl, string bucket)
        => _amazonSqsToBeExtended.AuthorizeS3ToSendMessage(queueUrl, bucket);
#endif

    public virtual Task<string> AuthorizeS3ToSendMessageAsync(string queueUrl, string bucket)
        => _amazonSqsToBeExtended.AuthorizeS3ToSendMessageAsync(queueUrl, bucket);

#if NETFRAMEWORK
    public AddPermissionResponse AddPermission(string queueUrl, string label, List<string> awsAccountIds, List<string> actions)
        => _amazonSqsToBeExtended.AddPermission(queueUrl, label, awsAccountIds, actions);

    public AddPermissionResponse AddPermission(AddPermissionRequest request)
        => _amazonSqsToBeExtended.AddPermission(request);
#endif

    public virtual Task<AddPermissionResponse> AddPermissionAsync(
        string queueUrl,
        string label,
        List<string> awsAccountIds,
        List<string> actions,
        CancellationToken cancellationToken = new())
        => AddPermissionAsync(new AddPermissionRequest(queueUrl, label, awsAccountIds, actions), cancellationToken);

    public virtual Task<AddPermissionResponse> AddPermissionAsync(
        AddPermissionRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.AddPermissionAsync(request, cancellationToken);

#if NETFRAMEWORK
    public CancelMessageMoveTaskResponse CancelMessageMoveTask(CancelMessageMoveTaskRequest request)
    {
        return _amazonSqsToBeExtended.CancelMessageMoveTask(request);
    }
#endif

    public Task<CancelMessageMoveTaskResponse> CancelMessageMoveTaskAsync(CancelMessageMoveTaskRequest request,
        CancellationToken cancellationToken = new())
    {
        return _amazonSqsToBeExtended.CancelMessageMoveTaskAsync(request, cancellationToken);
    }

    public ChangeMessageVisibilityResponse ChangeMessageVisibility(string queueUrl, string receiptHandle, int visibilityTimeout)
        => ChangeMessageVisibilityAsync(queueUrl, receiptHandle, visibilityTimeout).GetAwaiter().GetResult();

    public ChangeMessageVisibilityResponse ChangeMessageVisibility(ChangeMessageVisibilityRequest request)
        => ChangeMessageVisibilityAsync(request).GetAwaiter().GetResult();

    public virtual Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(
        string queueUrl,
        string receiptHandle,
        int visibilityTimeout,
        CancellationToken cancellationToken = new())
        => ChangeMessageVisibilityAsync(
            new ChangeMessageVisibilityRequest(queueUrl, receiptHandle, visibilityTimeout), cancellationToken);

    public virtual Task<ChangeMessageVisibilityResponse> ChangeMessageVisibilityAsync(
        ChangeMessageVisibilityRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ChangeMessageVisibilityAsync(request, cancellationToken);

    public ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(string queueUrl,
        List<ChangeMessageVisibilityBatchRequestEntry> entries)
        => ChangeMessageVisibilityBatchAsync(queueUrl, entries).GetAwaiter().GetResult();

    public ChangeMessageVisibilityBatchResponse ChangeMessageVisibilityBatch(ChangeMessageVisibilityBatchRequest request)
        => ChangeMessageVisibilityBatchAsync(request).GetAwaiter().GetResult();

    public virtual Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(
        string queueUrl,
        List<ChangeMessageVisibilityBatchRequestEntry> entries,
        CancellationToken cancellationToken = new())
        => ChangeMessageVisibilityBatchAsync(new ChangeMessageVisibilityBatchRequest(queueUrl, entries),
            cancellationToken);

    public virtual Task<ChangeMessageVisibilityBatchResponse> ChangeMessageVisibilityBatchAsync(
        ChangeMessageVisibilityBatchRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ChangeMessageVisibilityBatchAsync(request, cancellationToken);

#if NETFRAMEWORK
    public CreateQueueResponse CreateQueue(string queueName)
        => _amazonSqsToBeExtended.CreateQueue(queueName);

    public CreateQueueResponse CreateQueue(CreateQueueRequest request)
        => _amazonSqsToBeExtended.CreateQueue(request);
#endif

    public virtual Task<CreateQueueResponse> CreateQueueAsync(
        string queueName,
        CancellationToken cancellationToken = new())
        => CreateQueueAsync(new CreateQueueRequest(queueName), cancellationToken);

    public virtual Task<CreateQueueResponse> CreateQueueAsync(
        CreateQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.CreateQueueAsync(request, cancellationToken);

    public DeleteMessageResponse DeleteMessage(string queueUrl, string receiptHandle)
        => DeleteMessageAsync(queueUrl, receiptHandle).GetAwaiter().GetResult();

    public DeleteMessageResponse DeleteMessage(DeleteMessageRequest request)
        => DeleteMessageAsync(request).GetAwaiter().GetResult();

    public virtual Task<DeleteMessageResponse> DeleteMessageAsync(
        string queueUrl,
        string receiptHandle,
        CancellationToken cancellationToken = new())
        => DeleteMessageAsync(new DeleteMessageRequest(queueUrl, receiptHandle), cancellationToken);

    public virtual Task<DeleteMessageResponse> DeleteMessageAsync(
        DeleteMessageRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.DeleteMessageAsync(request, cancellationToken);

    public DeleteMessageBatchResponse DeleteMessageBatch(string queueUrl, List<DeleteMessageBatchRequestEntry> entries)
        => DeleteMessageBatchAsync(queueUrl, entries).GetAwaiter().GetResult();

    public DeleteMessageBatchResponse DeleteMessageBatch(DeleteMessageBatchRequest request)
        => DeleteMessageBatchAsync(request).GetAwaiter().GetResult();

    public virtual Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(
        string queueUrl,
        List<DeleteMessageBatchRequestEntry> entries,
        CancellationToken cancellationToken = new())
        => DeleteMessageBatchAsync(new DeleteMessageBatchRequest(queueUrl, entries), cancellationToken);

    public virtual Task<DeleteMessageBatchResponse> DeleteMessageBatchAsync(
        DeleteMessageBatchRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.DeleteMessageBatchAsync(request, cancellationToken);

#if NETFRAMEWORK
    public DeleteQueueResponse DeleteQueue(string queueUrl)
        => _amazonSqsToBeExtended.DeleteQueue(queueUrl);

    public DeleteQueueResponse DeleteQueue(DeleteQueueRequest request)
        => _amazonSqsToBeExtended.DeleteQueue(request);
#endif

    public virtual Task<DeleteQueueResponse> DeleteQueueAsync(
        string queueUrl,
        CancellationToken cancellationToken = new())
        => DeleteQueueAsync(new DeleteQueueRequest(queueUrl), cancellationToken);

    public virtual Task<DeleteQueueResponse> DeleteQueueAsync(
        DeleteQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.DeleteQueueAsync(request, cancellationToken);

#if NETFRAMEWORK
    public GetQueueAttributesResponse GetQueueAttributes(string queueUrl, List<string> attributeNames)
        => _amazonSqsToBeExtended.GetQueueAttributes(queueUrl, attributeNames);

    public GetQueueAttributesResponse GetQueueAttributes(GetQueueAttributesRequest request)
        => _amazonSqsToBeExtended.GetQueueAttributes(request);
#endif

    public virtual Task<GetQueueAttributesResponse> GetQueueAttributesAsync(
        string queueUrl,
        List<string> attributeNames,
        CancellationToken cancellationToken = new())
        => GetQueueAttributesAsync(new GetQueueAttributesRequest(queueUrl, attributeNames), cancellationToken);

    public virtual Task<GetQueueAttributesResponse> GetQueueAttributesAsync(
        GetQueueAttributesRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.GetQueueAttributesAsync(request, cancellationToken);

#if NETFRAMEWORK
    public GetQueueUrlResponse GetQueueUrl(string queueName)
        => _amazonSqsToBeExtended.GetQueueUrl(queueName);

    public GetQueueUrlResponse GetQueueUrl(GetQueueUrlRequest request)
        => _amazonSqsToBeExtended.GetQueueUrl(request);
#endif

    public virtual Task<GetQueueUrlResponse> GetQueueUrlAsync(
        string queueName,
        CancellationToken cancellationToken = new())
        => GetQueueUrlAsync(new GetQueueUrlRequest(queueName), cancellationToken);

    public virtual Task<GetQueueUrlResponse> GetQueueUrlAsync(
        GetQueueUrlRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.GetQueueUrlAsync(request, cancellationToken);

#if NETFRAMEWORK
    public ListDeadLetterSourceQueuesResponse ListDeadLetterSourceQueues(ListDeadLetterSourceQueuesRequest request)
        => _amazonSqsToBeExtended.ListDeadLetterSourceQueues(request);
#endif

    public virtual Task<ListDeadLetterSourceQueuesResponse> ListDeadLetterSourceQueuesAsync(
        ListDeadLetterSourceQueuesRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ListDeadLetterSourceQueuesAsync(request, cancellationToken);

#if NETFRAMEWORK
    public ListMessageMoveTasksResponse ListMessageMoveTasks(ListMessageMoveTasksRequest request)
    {
        return _amazonSqsToBeExtended.ListMessageMoveTasks(request);
    }
#endif

    public Task<ListMessageMoveTasksResponse> ListMessageMoveTasksAsync(ListMessageMoveTasksRequest request,
        CancellationToken cancellationToken = new())
    {
        return _amazonSqsToBeExtended.ListMessageMoveTasksAsync(request, cancellationToken);
    }

#if NETFRAMEWORK
    public ListQueuesResponse ListQueues(string queueNamePrefix)
        => _amazonSqsToBeExtended.ListQueues(queueNamePrefix);

    public ListQueuesResponse ListQueues(ListQueuesRequest request)
        => _amazonSqsToBeExtended.ListQueues(request);
#endif

    public virtual Task<ListQueuesResponse> ListQueuesAsync(
        string queueNamePrefix,
        CancellationToken cancellationToken = new())
        => ListQueuesAsync(new ListQueuesRequest(queueNamePrefix), cancellationToken);

    public virtual Task<ListQueuesResponse> ListQueuesAsync(
        ListQueuesRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ListQueuesAsync(request, cancellationToken);

#if NETFRAMEWORK
    public ListQueueTagsResponse ListQueueTags(ListQueueTagsRequest request)
        => _amazonSqsToBeExtended.ListQueueTags(request);
#endif

    public virtual Task<ListQueueTagsResponse> ListQueueTagsAsync(
        ListQueueTagsRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ListQueueTagsAsync(request, cancellationToken);

#if NETFRAMEWORK
    public PurgeQueueResponse PurgeQueue(string queueUrl)
        => _amazonSqsToBeExtended.PurgeQueue(queueUrl);

    public PurgeQueueResponse PurgeQueue(PurgeQueueRequest request)
        => _amazonSqsToBeExtended.PurgeQueue(request);
#endif

    public virtual Task<PurgeQueueResponse> PurgeQueueAsync(
        string queueUrl,
        CancellationToken cancellationToken = new())
        => PurgeQueueAsync(new PurgeQueueRequest { QueueUrl = queueUrl }, cancellationToken);

    public virtual Task<PurgeQueueResponse> PurgeQueueAsync(
        PurgeQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.PurgeQueueAsync(request, cancellationToken);

    public ReceiveMessageResponse ReceiveMessage(string queueUrl)
        => ReceiveMessageAsync(queueUrl).GetAwaiter().GetResult();

    public ReceiveMessageResponse ReceiveMessage(ReceiveMessageRequest request)
        => ReceiveMessageAsync(request).GetAwaiter().GetResult();

    public virtual Task<ReceiveMessageResponse> ReceiveMessageAsync(
        string queueUrl,
        CancellationToken cancellationToken = new())
        => ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl), cancellationToken);

    public virtual Task<ReceiveMessageResponse> ReceiveMessageAsync(
        ReceiveMessageRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.ReceiveMessageAsync(request, cancellationToken);

#if NETFRAMEWORK
    public RemovePermissionResponse RemovePermission(string queueUrl, string label)
        => _amazonSqsToBeExtended.RemovePermission(queueUrl, label);

    public RemovePermissionResponse RemovePermission(RemovePermissionRequest request)
        => _amazonSqsToBeExtended.RemovePermission(request);
#endif

    public virtual Task<RemovePermissionResponse> RemovePermissionAsync(
        string queueUrl,
        string label,
        CancellationToken cancellationToken = new())
        => RemovePermissionAsync(new RemovePermissionRequest(queueUrl, label), cancellationToken);

    public virtual Task<RemovePermissionResponse> RemovePermissionAsync(
        RemovePermissionRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.RemovePermissionAsync(request, cancellationToken);

    public SendMessageResponse SendMessage(string queueUrl, string messageBody)
        => SendMessageAsync(queueUrl, messageBody).GetAwaiter().GetResult();

    public SendMessageResponse SendMessage(SendMessageRequest request)
        => SendMessageAsync(request).GetAwaiter().GetResult();

    public virtual Task<SendMessageResponse> SendMessageAsync(
        string queueUrl,
        string messageBody,
        CancellationToken cancellationToken = new())
        => SendMessageAsync(new SendMessageRequest(queueUrl, messageBody), cancellationToken);

    public virtual Task<SendMessageResponse> SendMessageAsync(
        SendMessageRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.SendMessageAsync(request, cancellationToken);

    public SendMessageBatchResponse SendMessageBatch(string queueUrl, List<SendMessageBatchRequestEntry> entries)
        => SendMessageBatchAsync(queueUrl, entries).GetAwaiter().GetResult();

    public SendMessageBatchResponse SendMessageBatch(SendMessageBatchRequest request)
        => SendMessageBatchAsync(request).GetAwaiter().GetResult();

    public virtual Task<SendMessageBatchResponse> SendMessageBatchAsync(
        string queueUrl,
        List<SendMessageBatchRequestEntry> entries,
        CancellationToken cancellationToken = new())
        => SendMessageBatchAsync(new SendMessageBatchRequest(queueUrl, entries), cancellationToken);

    public virtual Task<SendMessageBatchResponse> SendMessageBatchAsync(
        SendMessageBatchRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.SendMessageBatchAsync(request, cancellationToken);

#if NETFRAMEWORK
    public SetQueueAttributesResponse SetQueueAttributes(string queueUrl, Dictionary<string, string> attributes)
        => _amazonSqsToBeExtended.SetQueueAttributes(queueUrl, attributes);

    public SetQueueAttributesResponse SetQueueAttributes(SetQueueAttributesRequest request)
        => _amazonSqsToBeExtended.SetQueueAttributes(request);
#endif

    public virtual Task<SetQueueAttributesResponse> SetQueueAttributesAsync(
        string queueUrl,
        Dictionary<string, string> attributes,
        CancellationToken cancellationToken = new())
        => SetQueueAttributesAsync(new SetQueueAttributesRequest(queueUrl, attributes), cancellationToken);

    public virtual Task<SetQueueAttributesResponse> SetQueueAttributesAsync(
        SetQueueAttributesRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.SetQueueAttributesAsync(request, cancellationToken);

#if NETFRAMEWORK
    public StartMessageMoveTaskResponse StartMessageMoveTask(StartMessageMoveTaskRequest request)
    {
        return _amazonSqsToBeExtended.StartMessageMoveTask(request);
    }
#endif

    public Task<StartMessageMoveTaskResponse> StartMessageMoveTaskAsync(StartMessageMoveTaskRequest request,
        CancellationToken cancellationToken = new())
    {
        return _amazonSqsToBeExtended.StartMessageMoveTaskAsync(request, cancellationToken);
    }

#if NETFRAMEWORK
    public TagQueueResponse TagQueue(TagQueueRequest request)
        => _amazonSqsToBeExtended.TagQueue(request);
#endif

    public virtual Task<TagQueueResponse> TagQueueAsync(
        TagQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.TagQueueAsync(request, cancellationToken);

#if NETFRAMEWORK
    public UntagQueueResponse UntagQueue(UntagQueueRequest request)
        => _amazonSqsToBeExtended.UntagQueue(request);
#endif

    public virtual Task<UntagQueueResponse> UntagQueueAsync(
        UntagQueueRequest request,
        CancellationToken cancellationToken = new())
        => _amazonSqsToBeExtended.UntagQueueAsync(request, cancellationToken);

    public Endpoint DetermineServiceOperationEndpoint(AmazonWebServiceRequest request)
    {
        return _amazonSqsToBeExtended.DetermineServiceOperationEndpoint(request);
    }

    public ISQSPaginatorFactory Paginators => _amazonSqsToBeExtended.Paginators;
}