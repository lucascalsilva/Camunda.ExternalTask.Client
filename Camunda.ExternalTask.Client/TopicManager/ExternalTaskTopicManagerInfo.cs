using System;
using System.Collections.Generic;
using Camunda.ExternalTask.Client.Adapter;

namespace Camunda.ExternalTask.Client.TopicManager
{
    public class ExternalTaskTopicManagerInfo
    {
        public int Retries { get; set; }
        public long RetryTimeout { get; set; }
        public Type Type { get; set; }
        public string TopicName { get; set; }
        public List<string> VariablesToFetch { get; set; } = null;
        public IExternalTaskAdapter TaskAdapter { get; set; }
    }
}