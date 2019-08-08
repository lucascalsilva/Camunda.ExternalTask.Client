using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.DTO;
using Camunda.ExternalTask.Client.Worker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Camunda.ExternalTask.Client
{

    public class ExternalTaskClient
    {
        public static string DEFAULT_URL = "http://localhost:8080/engine-rest";

        private IList<ExternalTaskWorker> _workers = new List<ExternalTaskWorker>();
        private CamundaClient camundaClient;

        public ExternalTaskClient() {
            camundaClient = CamundaClient.Create(DEFAULT_URL);
        }
        public ExternalTaskClient(string restUrl) { 
            camundaClient = CamundaClient.Create(restUrl);
        }

        public ExternalTaskClient(string restUrl, string userName, string password)
        {
            string encodedCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(userName + ":" + password));
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(restUrl);
            httpClient.DefaultRequestHeaders.Add("Authorization", "Basic "+encodedCredentials);
            
            camundaClient = CamundaClient.Create(httpClient);
        }

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
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var externalTaskWorkers = RetrieveExternalTaskWorkerInfo(assembly);

            foreach (var taskWorkerInfo in externalTaskWorkers)
            {
                Console.WriteLine($"Register Task Worker for Topic '{taskWorkerInfo.TopicName}'");
                ExternalTaskWorker worker = new ExternalTaskWorker(camundaClient.ExternalTasks, taskWorkerInfo);
                _workers.Add(worker);
                worker.StartWork();
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
                    WorkerId = externalTaskTopicAttribute.WorkerId,
                    Type = t,
                    TopicName = externalTaskTopicAttribute.TopicName,
                    Retries = externalTaskTopicAttribute.Retries,
                    RetryTimeout = externalTaskTopicAttribute.RetryTimeout,
                    VariablesToFetch = externalTaskVariableRequirements?.VariablesToFetch,
                    TaskAdapter = t.GetConstructor(Type.EmptyTypes)?.Invoke(null) as IExternalTaskAdapter
                };
            return externalTaskWorkers;
        }

        public void StopWorkers()
        {
            foreach (ExternalTaskWorker worker in _workers)
            {
                worker.StopWork();
            }
        }
    }
}