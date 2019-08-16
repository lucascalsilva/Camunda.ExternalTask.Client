using System;
using System.Collections.Generic;
using Camunda.ExternalTask.Client.Adapter;

namespace Camunda.ExternalTask.Client.TopicManager
{
    public class ExternalTaskTopicManagerInfo
    {
        public ExternalTaskTopicManagerInfo(){}
        public int Retries { get; set; }
        public long RetryTimeout { get; set; }
        public Type Type { get; set; }
        public string TopicName { get; set; }
        public int MaxTasks { get ; set; }
        public long PollingIntervalInMilliseconds { get ; set; }
		public int MaxDegreeOfParallelism { get ; set; }
		public long LockDurationInMilliseconds { get ; set; }
        public long MaxTimeBetweenConnections { get; set; }
        public List<string> VariablesToFetch { get; set; }
        public IExternalTaskAdapter TaskAdapter { get; set; }
    }
}