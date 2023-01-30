namespace Amazon.Sqs.Extended.Client;

public static class SqsExtendedClientConstants
{
    // This constant is shared with SNSExtendedClient
    // SNS team should be notified of any changes made to this
    public const string ReservedAttributeName = "ExtendedPayloadSize";

    // This constant is shared with SNSExtendedClient
    // SNS team should be notified of any changes made to this
    public const int MaxAllowedAttributes = 10 - 1; // 10 for SQS, 1 for the reserved attribute

    // This constant is shared with SNSExtendedClient
    // SNS team should be notified of any changes made to this
    public const int DefaultMessageSizeThreshold = 262144;

    public const string S3BucketNameMarker = "-..s3BucketName..-";
    public const string S3KeyMarker = "-..s3Key..-";
}