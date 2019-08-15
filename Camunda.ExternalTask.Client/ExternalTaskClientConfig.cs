namespace Camunda.ExternalTask.Client
{
    public class ExternalTaskClientConfig
    {
        public string WorkerId { get; set; }
        public int MaxTasks { get ; set; }
        public long PollingIntervalInMilliseconds { get ; set; }
		public int MaxDegreeOfParallelism { get ; set; }
		public long LockDurationInMilliseconds { get ; set; }
        public long MaxTimeBetweenConnections { get; set; }
    }
}