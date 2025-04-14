using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Amazon.SQS;
using Amazon.Sqs.Extended.Client;
using Amazon.Sqs.Extended.Client.Demo;
using Amazon.Sqs.Extended.Client.Extensions;
using Amazon.Sqs.Extended.Client.Models;
using Amazon.Sqs.Extended.Client.Providers;
using DotNetEnv;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Env.Load(options: new LoadOptions(onlyExactPath: false));

var builder = Host.CreateApplicationBuilder(args);
var bucketName = builder.Configuration.GetValue<string>("S3:BucketName") ?? throw new InvalidOperationException("S3:BucketName not configured");

builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.SetMinimumLevel(LogLevel.Debug);
    loggingBuilder.ClearProviders().AddConsole();
});

builder.Services.AddKeyedSingleton<string>("SqsUrl", (sp, _) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    return config.GetValue<string>("SQS:Url") ?? throw new InvalidOperationException("SqsUrl not configured");
});

builder.Services.Configure<AWSOptions>(builder.Configuration.GetSection("AWS"));
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();

builder.Services.AddScoped<IAmazonS3>(sp =>
{
    var awsOptions = sp.GetRequiredService<AWSOptions>();

    var config = sp.GetRequiredService<IConfiguration>().GetSection("AWS");

    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = awsOptions.Region,
        ServiceURL = config.GetValue<string>("ServiceURL"),
        UseHttp = config.GetValue<bool>("UseHttp"),
        ForcePathStyle = config.GetValue<bool>("ForcePathStyle")
    };

    return new AmazonS3Client(s3Config);
});

builder.Services.AddSingleton<PayloadStoreConfiguration>(_ => new PayloadStoreConfiguration(bucketName));

builder.Services.AddScoped<S3PayloadStore>(sp =>
{
    var s3Client = sp.GetRequiredService<IAmazonS3>();

    return new S3PayloadStore(s3Client, new GuidPayloadStoreKeyProvider(),
        new PayloadStoreConfiguration(bucketName),
        sp.GetRequiredService<ILogger<S3PayloadStore>>());
});

builder.Services.AddKeyedScoped<IAmazonSQS>("with-s3-backing", (sp, _) =>
{
    var sqsClient = sp.GetRequiredService<IAmazonSQS>();
    var payloadStore = sp.GetRequiredService<S3PayloadStore>();
    var logger = sp.GetRequiredService<ILogger<AmazonSqsExtendedClient>>();

    return new AmazonSqsExtendedClient(sqsClient, payloadStore,
        new ExtendedClientConfiguration().WithLargePayloadSupportEnabled(true),
        logger);
});

builder.Services.AddHostedService<MessageProducerService>();
builder.Services.AddHostedService<MessageConsumerService>();

await builder.Build().RunAsync();