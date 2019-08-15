using System;
using System.Net.Http;
using System.Text;
using Camunda.Api.Client;

namespace Camunda.ExternalTask.Client
{
	public class ExternalTaskClientBuilder
	{
		private string baseUrl = "http://localhost:8080/engine-rest";
		private string username;
		private string password;
		private string workerId = Guid.NewGuid().ToString();
		private int maxTasks = 10;
		private long pollingIntervalInMilliseconds = 50;
		private int maxDegreeOfParallelism = 2;
		private long lockDurationInMilliseconds = 1 * 60 * 1000;
		private long maxTimeBetweenConnections = 8;

		public static ExternalTaskClientBuilder Create()
		{
			return new ExternalTaskClientBuilder();
		}

		public ExternalTaskClientBuilder WorkerId(string workerId)
		{
			this.workerId = workerId;
			return this;
		}

		public ExternalTaskClientBuilder BaseUrl(string baseUrl)
		{
			this.baseUrl = baseUrl;
			return this;
		}

		public ExternalTaskClientBuilder Username(string username)
		{
			this.username = username;
			return this;
		}

		public ExternalTaskClientBuilder Password(string password)
		{
			this.password = password;
			return this;
		}

		public ExternalTaskClientBuilder PollingIntervalInMilliseconds(long pollingIntervalInMilliseconds)
		{
			this.pollingIntervalInMilliseconds = pollingIntervalInMilliseconds;
			return this;
		}

		public ExternalTaskClientBuilder MaxDegreeOfParallelism(int maxDegreeOfParallelism)
		{
			this.maxDegreeOfParallelism = maxDegreeOfParallelism;
			return this;
		}

		public ExternalTaskClientBuilder LockDurationInMilliseconds(int lockDurationInMilliseconds)
		{
			this.lockDurationInMilliseconds = lockDurationInMilliseconds;
			return this;
		}

		public ExternalTaskClientBuilder MaxTimeBetweenConnections(int maxTimeBetweenConnections){
			this.maxTimeBetweenConnections = maxTimeBetweenConnections;
			return this;
		}

		public ExternalTaskClient Build()
		{
			var httpClient = new HttpClient();
			httpClient.BaseAddress = new Uri(baseUrl);
			if (username != null && password != null)
			{
				var encodedCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(username + ":" + password));
				httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + encodedCredentials);
			}
			CamundaClient camundaClient = CamundaClient.Create(httpClient);

			ExternalTaskClientConfig clientConfig = new ExternalTaskClientConfig()
			{
				WorkerId = workerId,
				MaxTasks = maxTasks,
				LockDurationInMilliseconds = lockDurationInMilliseconds,
				MaxDegreeOfParallelism = maxDegreeOfParallelism,
				PollingIntervalInMilliseconds = pollingIntervalInMilliseconds,
				MaxTimeBetweenConnections = maxTimeBetweenConnections
			};

			return new ExternalTaskClient(camundaClient, clientConfig);
		}

	}
}