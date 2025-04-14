using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amazon.Sqs.Extended.Client.Demo;

public class MessageConsumerService : BackgroundService
{
    private readonly IAmazonSQS _extendedClient;
    private readonly string _sqsUrl;
    private readonly ILogger<MessageConsumerService> _logger;

    public MessageConsumerService([FromKeyedServices("with-s3-backing")] IAmazonSQS extendedClient,
        [FromKeyedServices("SqsUrl")] string sqsUrl,
        ILogger<MessageConsumerService> logger)
    {
        _extendedClient = extendedClient;
        _sqsUrl = sqsUrl;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiveRequest = new ReceiveMessageRequest(_sqsUrl)
        {
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 5,
            VisibilityTimeout = 30
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _extendedClient.ReceiveMessageAsync(receiveRequest, stoppingToken);

                foreach (var message in result.Messages)
                {
                    _logger.LogInformation("id: {MessageId}, length of message: {BodyLength}", message.MessageId, message.Body.Length);
                    await _extendedClient.DeleteMessageAsync(_sqsUrl, message.ReceiptHandle, stoppingToken);
                }
            }
            catch (InvalidAddressException e)
            {
                _logger.LogError("{Message}", e.FlattenMessage());
                break;
            }
            catch (Exception e)
            {
                _logger.LogError("{Message}", e.FlattenMessage());
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}