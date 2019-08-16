namespace Camunda.ExternalTask.Client.Adapter
{
    [System.AttributeUsage(System.AttributeTargets.Class |
                           System.AttributeTargets.Struct)
    ]
    public sealed class ExternalTaskTopicAttribute : System.Attribute
    {
        public string TopicName { get; }
        public int Retries { get; } = 5;
        public long RetryTimeout { get; } = 10 * 1000; // default: 10 seconds
        public int MaxTasks { get ; set; } = 10;
        public long PollingIntervalInMilliseconds { get ; set; } = 50;
		public int MaxDegreeOfParallelism { get ; set; } = 2;
		public long LockDurationInMilliseconds { get ; set; } = 1 * 60 * 1000;
        public long MaxTimeBetweenConnections { get; set; } = 8;

        public ExternalTaskTopicAttribute(string topicName)
        {
            TopicName = topicName;
        }

        public ExternalTaskTopicAttribute(string topicName, int retries, long retryTimeout)
        {
            TopicName = topicName;
            Retries = retries;
            RetryTimeout = retryTimeout;
        }
    }
}
