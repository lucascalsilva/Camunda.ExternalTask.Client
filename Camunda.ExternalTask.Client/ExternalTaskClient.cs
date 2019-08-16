using Camunda.Api.Client;
using Camunda.ExternalTask.Client.Adapter;
using Camunda.ExternalTask.Client.TopicManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Camunda.ExternalTask.Client
{

    public class ExternalTaskClient : IExternalTaskClient
    {
        private string workerId;
        private CamundaClient camundaClient;
        private IList<ExternalTaskTopicManager> topicManagers = new List<ExternalTaskTopicManager>();

        public ExternalTaskClient(CamundaClient camundaClient, string workerId){
            this.camundaClient = camundaClient;
            this.workerId = workerId;
        }

        public void Startup()
        {
            this.StartManagers();
        }

        public void Shutdown()
        {
            this.StopManagers();
        }

        public void StartManagers()
        {
            var assembly = Assembly.GetEntryAssembly();
            var externalTaskTopicManagers = RetrieveExternalTaskTopicManagerInfo(assembly);

            foreach (var topicManagerInfo in externalTaskTopicManagers)
            {
                Console.WriteLine($"Register Task Manager for Topic {topicManagerInfo.TopicName}...");
				ExternalTaskTopicManager topicManager = new ExternalTaskTopicManager(workerId, camundaClient.ExternalTasks, topicManagerInfo);
                topicManagers.Add(topicManager);
                topicManager.StartManager();
            }
        }

        public void StopManagers()
        {
            foreach (ExternalTaskTopicManager manager in topicManagers)
            {
                manager.StartManager();
            }
        }

        private static IEnumerable<ExternalTaskTopicManagerInfo> RetrieveExternalTaskTopicManagerInfo(Assembly assembly)
        {
            // find all classes with CustomAttribute [ExternalTask("name")]
            var externalTaskTopicManagers =
                from t in assembly.GetTypes()
                let externalTaskTopicAttribute = t.GetCustomAttributes(typeof(ExternalTaskTopicAttribute), true).FirstOrDefault() as ExternalTaskTopicAttribute
                let externalTaskVariableRequirements = t.GetCustomAttributes(typeof(ExternalTaskVariableRequirementsAttribute), true).FirstOrDefault() as ExternalTaskVariableRequirementsAttribute
                where externalTaskTopicAttribute != null
                select new ExternalTaskTopicManagerInfo
                {
                    Type = t,
                    TopicName = externalTaskTopicAttribute.TopicName,
                    Retries = externalTaskTopicAttribute.Retries,
                    RetryTimeout = externalTaskTopicAttribute.RetryTimeout,
                    MaxTasks = externalTaskTopicAttribute.MaxTasks,
                    PollingIntervalInMilliseconds = externalTaskTopicAttribute.PollingIntervalInMilliseconds,
                    MaxDegreeOfParallelism = externalTaskTopicAttribute.MaxDegreeOfParallelism,
                    LockDurationInMilliseconds = externalTaskTopicAttribute.LockDurationInMilliseconds,
                    MaxTimeBetweenConnections = externalTaskTopicAttribute.MaxTimeBetweenConnections,
                    VariablesToFetch = externalTaskVariableRequirements?.VariablesToFetch,
                    TaskAdapter = t.GetConstructor(Type.EmptyTypes)?.Invoke(null) as IExternalTaskAdapter
                };
            return externalTaskTopicManagers;
        }
    }
}