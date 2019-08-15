using System;
using System.Collections.Generic;
using Camunda.ExternalTask.Client.Adapter;

namespace Camunda.ExternalTask.Client.TopicManager
{
    public class ExternalTaskTopicManagerInfo
    {
        public ExternalTaskTopicManagerInfo(){}
        
        public virtual int Retries { get; set; }
        public virtual long RetryTimeout { get; set; }
        public virtual Type Type { get; set; }
        public virtual string TopicName { get; set; }
        public virtual List<string> VariablesToFetch { get; set; } = null;
        public virtual IExternalTaskAdapter TaskAdapter { get; set; }
    }
}