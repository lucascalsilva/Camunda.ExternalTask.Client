using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Polly;
using Polly.Timeout;

namespace Camunda.ExternalTask.Client.TopicManager
{
	public class ExternalTaskTopicManager : IDisposable
	{
		private ExternalTaskClientConfig clientConfig;
		private Timer taskQueryTimer;
		private ExternalTaskService externalTaskService;
		private ExternalTaskTopicManagerInfo topicManagerInfo;

		public ExternalTaskTopicManager(ExternalTaskClientConfig clientConfig, ExternalTaskService externalTaskService, ExternalTaskTopicManagerInfo taskManagerInfo)
		{
			this.clientConfig = clientConfig;
			this.externalTaskService = externalTaskService;
			this.topicManagerInfo = taskManagerInfo;
		}
		public void DoPolling()
		{
			// Query External Tasks
			// Exception handling is only showing the connect back message when the long polling is finished
			try
			{
				var fetchExternalTasks = new FetchExternalTasks()
				{
					WorkerId = clientConfig.WorkerId,
					MaxTasks = clientConfig.MaxTasks,
					Topics = new List<FetchExternalTaskTopic>(){
					new FetchExternalTaskTopic(topicManagerInfo.TopicName, clientConfig.LockDurationInMilliseconds){
						Variables = topicManagerInfo.VariablesToFetch == null ? topicManagerInfo.VariablesToFetch : null
						}
					}
				};
				var tasks = new List<LockedExternalTask>();
				getStandardPolicy().Execute(() => { tasks = externalTaskService.FetchAndLock(fetchExternalTasks).Result; });

				// run them in parallel with a max degree of parallelism
				Parallel.ForEach(
					tasks,
					new ParallelOptions { MaxDegreeOfParallelism = clientConfig.MaxDegreeOfParallelism },
					externalTask =>
					{
						Execute(externalTask);
					}
				);
			}
			finally
			{
				// schedule next run (if not stopped in between)
				if (taskQueryTimer != null)
				{
					taskQueryTimer.Change(TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(Timeout.Infinite));
				}
			}
		}
		public virtual void Execute(LockedExternalTask lockedExternalTask)
		{
			var resultVariables = new Dictionary<string, VariableValue>();

			Console.WriteLine($"Executing External Task from topic {topicManagerInfo.TopicName}: {lockedExternalTask}...");
			try
			{
				topicManagerInfo.TaskAdapter.Execute(lockedExternalTask, ref resultVariables);
				Console.WriteLine($"Finished External Task {lockedExternalTask.Id}...");
				var completeExternalTask = new CompleteExternalTask()
				{
					WorkerId = clientConfig.WorkerId,
					Variables = resultVariables
				};
				getStandardPolicy().Execute(() => externalTaskService.Complete(lockedExternalTask.Id, completeExternalTask));
			}
			catch (UnrecoverableBusinessErrorException ex)
			{
				Console.WriteLine($"Failed with business error code {ex.BusinessErrorCode} for External Task  {lockedExternalTask.Id}...");
				var externalTaskBpmnError = new ExternalTaskBpmnError
				{
					WorkerId = clientConfig.WorkerId,
					ErrorCode = ex.BusinessErrorCode,
					ErrorMessage = ex.Message
				};
				getStandardPolicy().Execute(() => externalTaskService.HandleBpmnError(lockedExternalTask.Id, externalTaskBpmnError));
			}
			catch (UnlockTaskException ex)
			{
				Console.WriteLine($"Unlock requested for External Task  {lockedExternalTask.Id}...");

				getStandardPolicy().Execute(() => externalTaskService.Unlock(lockedExternalTask.Id));
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed External Task  {lockedExternalTask.Id}...");
				var retriesLeft = topicManagerInfo.Retries; // start with default
				if (lockedExternalTask.Retries.HasValue) // or decrement if retries are already set
				{
					retriesLeft = lockedExternalTask.Retries.Value - 1;
				}
				var externalTaskFailure = new ExternalTaskFailure()
				{
					WorkerId = clientConfig.WorkerId,
					Retries = retriesLeft,
					ErrorMessage = ex.Message,
					ErrorDetails = ex.StackTrace
				};
				getStandardPolicy().Execute(() => externalTaskService.HandleFailure(lockedExternalTask.Id, externalTaskFailure));
			}
		}
		public void StartManager()
		{
			Console.WriteLine($"Starting manager for topic {topicManagerInfo.TopicName}...");
			this.taskQueryTimer = new Timer(_ => DoPolling(), null, clientConfig.PollingIntervalInMilliseconds, Timeout.Infinite);
		}
		public void StopManager()
		{
			Console.WriteLine($"Stopping manager for topic {topicManagerInfo.TopicName}...");
			this.taskQueryTimer.Dispose();
			this.taskQueryTimer = null;
		}
		public void Dispose()
		{
			if (this.taskQueryTimer != null)
			{
				this.taskQueryTimer.Dispose();
			}
		}

		private Policy getStandardPolicy()
		{
			return Policy.HandleInner<HttpRequestException>()
				.WaitAndRetryForever(retryAttempt =>
				{
					var maxConnectionTimeout =  TimeSpan.FromSeconds(clientConfig.MaxTimeBetweenConnections);
					var nextRetryAttempt = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
					if(nextRetryAttempt > maxConnectionTimeout){
						return maxConnectionTimeout;
					}
					else{
						return nextRetryAttempt;
					}
				},
				(ex, span) =>
				{
					Console.WriteLine($"Failed! Waiting {span}");
					Console.WriteLine($"Error was {ex.GetType().Name}");
				}
			);
		}
	}
}