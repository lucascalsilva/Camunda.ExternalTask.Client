using System;
using System.Net.Http;
using Camunda.ExternalTask.Client.Backoff;
using Polly;
using Refit;

namespace Camunda.ExternalTask.Client.PolicyManager
{
	public class DefaultPolicyManager : IPolicyManager
	{
        private IBackoffStrategy backoffStrategy;
        private string topicName;
        
        public DefaultPolicyManager(IBackoffStrategy backoffStrategy, string topicName){
            this.backoffStrategy = backoffStrategy;
            this.topicName = topicName;
        }

		public Policy completePolicy()
		{
			return defaultPolicy(Operation.COMPLETE);
		}

		public Policy fetchAndLockPolicy()
		{
			return defaultPolicy(Operation.FETCH_AND_LOCK);
		}

		public Policy handleBpmnErrorPolicy()
		{
			return defaultPolicy(Operation.HANDLE_BPMN_ERROR);
		}

		public Policy handleFailurePolicy()
		{
			return defaultPolicy(Operation.HANDLE_FAILURE);
		}

		public Policy unlockPolicy()
		{
			return defaultPolicy(Operation.UNLOCK);
		}

		private Policy defaultPolicy(Operation operation){
            return Policy.HandleInner<ApiException>().OrInner<HttpRequestException>()
				.WaitAndRetryForever(retryAttempt =>
				{
					backoffStrategy.Reconfigure(0);
					return TimeSpan.FromMilliseconds(backoffStrategy.Calculate());
				},
				(ex, span) =>
				{
					Console.WriteLine($"Failure in operation {operation} topic {topicName} with error {ex.GetType().ToString()}. Will try again in {span} seconds...");
				}
			);
        }
	}
}