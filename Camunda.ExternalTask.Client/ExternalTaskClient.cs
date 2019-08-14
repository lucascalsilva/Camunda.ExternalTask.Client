using Camunda.Api.Client;
using Camunda.ExternalTask.Client.DTO;
using Camunda.ExternalTask.Client.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;

namespace Camunda.ExternalTask.Client
{

    public class ExternalTaskClient : IExternalTaskClient
    {
        public ExternalTaskClient(){}
        
        public string WorkerId { get; set; }
        public int MaxTasks { get ; set; }
        public long PollingIntervalInMilliseconds { get ; set; }
		public int MaxDegreeOfParallelism { get ; set; }
		public long LockDurationInMilliseconds { get ; set; }
        public CamundaClient CamundaClient { get; set; }
        private IList<ExternalTaskWorker> workers = new List<ExternalTaskWorker>();

        public void Startup()
        {
            this.StartWorkers();
        }

        public void Shutdown()
        {
            this.StopWorkers();
        }

        public void StartWorkers()
        {
            var assembly = Assembly.GetEntryAssembly();
            var externalTaskWorkers = RetrieveExternalTaskWorkerInfo(assembly);

            foreach (var taskWorkerInfo in externalTaskWorkers)
            {
                Console.WriteLine($"Register Task Worker for Topic '{taskWorkerInfo.TopicName}'");
                ExternalTaskWorker worker = new ExternalTaskWorker(this, CamundaClient.ExternalTasks, taskWorkerInfo);
                workers.Add(worker);
                worker.StartWork();
            }
        }

        public void StopWorkers()
        {
            foreach (ExternalTaskWorker worker in workers)
            {
                worker.StopWork();
            }
        }

        private static IEnumerable<ExternalTaskWorkerInfo> RetrieveExternalTaskWorkerInfo(System.Reflection.Assembly assembly)
        {
            // find all classes with CustomAttribute [ExternalTask("name")]
            var externalTaskWorkers =
                from t in assembly.GetTypes()
                let externalTaskTopicAttribute = t.GetCustomAttributes(typeof(ExternalTaskTopicAttribute), true).FirstOrDefault() as ExternalTaskTopicAttribute
                let externalTaskVariableRequirements = t.GetCustomAttributes(typeof(ExternalTaskVariableRequirementsAttribute), true).FirstOrDefault() as ExternalTaskVariableRequirementsAttribute
                where externalTaskTopicAttribute != null
                select new ExternalTaskWorkerInfo
                {
                    Type = t,
                    TopicName = externalTaskTopicAttribute.TopicName,
                    Retries = externalTaskTopicAttribute.Retries,
                    RetryTimeout = externalTaskTopicAttribute.RetryTimeout,
                    VariablesToFetch = externalTaskVariableRequirements?.VariablesToFetch,
                    TaskAdapter = t.GetConstructor(Type.EmptyTypes)?.Invoke(null) as IExternalTaskAdapter
                };
            return externalTaskWorkers;
        }
    }
}