using System;
using System.Collections.Generic;
using Camunda.ExternalTask.Client.Worker;

namespace Camunda.ExternalTask.Client.DTO
{
    public class ExternalTaskWorkerInfo
    {
        public string WorkerId { get; set; }
        public int Retries { get; set; }
        public long RetryTimeout { get; set; }
        public Type Type { get; set; }
        public string TopicName { get; set; }
        public List<string> VariablesToFetch { get; set; }
        public IExternalTaskAdapter TaskAdapter { get; set; }
    }
}