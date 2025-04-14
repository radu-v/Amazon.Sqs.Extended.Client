using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amazon.Sqs.Extended.Client.Demo;

public class MessageProducerService : BackgroundService
{
    private readonly IAmazonSQS _extendedClient;
    private readonly string _sqsUrl;
    private readonly ILogger<MessageProducerService> _logger;

    public MessageProducerService([FromKeyedServices("with-s3-backing")] IAmazonSQS extendedClient,
        [FromKeyedServices("SqsUrl")] string sqsUrl,
        ILogger<MessageProducerService> logger)
    {
        _extendedClient = extendedClient;
        _sqsUrl = sqsUrl;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var messageBody = $$"""
                            {"value": "{{new string('a', 256 * 1024 + 1)}}"}
                            """;

        while (!stoppingToken.IsCancellationRequested)
        {
            var sqsSendRequest = new SendMessageRequest(_sqsUrl, messageBody)
            {
                MessageGroupId = "LargePayloadTest"
            };

            try
            {
                var response = await _extendedClient.SendMessageAsync(sqsSendRequest, stoppingToken);
                _logger.LogInformation("Sent message id: {MessageId}", response.MessageId);
                await Task.Delay(5000, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError("{Message}", e.FlattenMessage());
                break;
            }
        }
    }
}