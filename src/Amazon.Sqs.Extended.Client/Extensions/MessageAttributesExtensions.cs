using Amazon.SQS.Model;

namespace Amazon.Sqs.Extended.Client.Extensions;

public static class MessageAttributesExtensions
{
    public static Dictionary<string, MessageAttributeValue> WithExtendedPayloadSize(
        this IDictionary<string, MessageAttributeValue> messageAttributes, int messageContentSize)
    {
        return new Dictionary<string, MessageAttributeValue>(messageAttributes)
        {
            [SqsExtendedClientConstants.ReservedAttributeName] = new()
            {
                DataType = "Number",
                StringValue = messageContentSize.ToString()
            }
        };
    }
}