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
        public ExternalTaskClientConfig ClientConfig { get; set;}   
        public CamundaClient CamundaClient { get; set; }
        private IList<ExternalTaskTopicManager> topicManagers = new List<ExternalTaskTopicManager>();

        public ExternalTaskClient(CamundaClient camundaClient, ExternalTaskClientConfig clientConfig){
            CamundaClient = camundaClient;
            ClientConfig = clientConfig;
        }


        public virtual void Startup()
        {
            this.StartManagers();
        }

        public virtual void Shutdown()
        {
            this.StopManagers();
        }

        public virtual void StartManagers()
        {
            var assembly = Assembly.GetEntryAssembly();
            var externalTaskTopicManagers = RetrieveExternalTaskTopicManagerInfo(assembly);

            foreach (var topicManagerInfo in externalTaskTopicManagers)
            {
                Console.WriteLine($"Register Task Manager for Topic {topicManagerInfo.TopicName}...");
				ExternalTaskTopicManager topicManager = new ExternalTaskTopicManager(ClientConfig, CamundaClient.ExternalTasks, topicManagerInfo);
                topicManagers.Add(topicManager);
                topicManager.StartManager();
            }
        }

        public virtual void StopManagers()
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
                    VariablesToFetch = externalTaskVariableRequirements?.VariablesToFetch,
                    TaskAdapter = t.GetConstructor(Type.EmptyTypes)?.Invoke(null) as IExternalTaskAdapter
                };
            return externalTaskTopicManagers;
        }
    }
}