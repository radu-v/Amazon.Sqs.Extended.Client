using System.Text;

namespace Amazon.Sqs.Extended.Client.Demo;

public static class ExceptionExtensions
{
    public static string FlattenMessage(this Exception e)
    {
        var sb = new StringBuilder(e.Message);

        var inner = e.InnerException;

        while (inner is not null)
        {
            sb.Append(" ---> ").Append(inner.Message);
            inner = inner.InnerException;
        }

        return sb.ToString();
    }
}