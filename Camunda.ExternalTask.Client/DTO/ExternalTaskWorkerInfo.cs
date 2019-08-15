using System;
using System.Collections.Generic;
using Camunda.ExternalTask.Client.Worker;

namespace Camunda.ExternalTask.Client.DTO
{
    public class ExternalTaskWorkerInfo
    {
        public ExternalTaskWorkerInfo(){}
        
        public virtual int Retries { get; set; }
        public virtual long RetryTimeout { get; set; }
        public virtual Type Type { get; set; }
        public virtual string TopicName { get; set; }
        public virtual List<string> VariablesToFetch { get; set; } = null;
        public virtual IExternalTaskAdapter TaskAdapter { get; set; }
    }
}