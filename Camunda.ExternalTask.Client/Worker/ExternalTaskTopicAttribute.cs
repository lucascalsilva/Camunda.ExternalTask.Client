namespace Camunda.ExternalTask.Client.Worker
{
    [System.AttributeUsage(System.AttributeTargets.Class |
                           System.AttributeTargets.Struct)
    ]
    public sealed class ExternalTaskTopicAttribute : System.Attribute
    {
        public string WorkerId {get; set;}
        public string TopicName { get; }
        public int Retries { get; } = 5; // default: 5 times
        public long RetryTimeout { get; } = 10 * 1000; // default: 10 seconds

        public ExternalTaskTopicAttribute(string topicName, string workerId)
        {
            TopicName = topicName;
            WorkerId = workerId;
        }

        public ExternalTaskTopicAttribute(string topicName, string workerId, int retries, long retryTimeout)
        {
            TopicName = topicName;
            WorkerId = workerId;
            Retries = retries;
            RetryTimeout = retryTimeout;
        }
    }
}
