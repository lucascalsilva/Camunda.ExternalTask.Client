using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Polly;

namespace Camunda.ExternalTask.Client.TopicManager
{
	public class ExternalTaskTopicManager : IDisposable
	{
		private string workerId;
		private Timer taskQueryTimer;
		private long baseRetryExp = 1;
		private long timerRetrySeconds = 1;
		private ExternalTaskService externalTaskService;
		private ExternalTaskTopicManagerInfo topicManagerInfo;

		public ExternalTaskTopicManager(string workerId, ExternalTaskService externalTaskService, ExternalTaskTopicManagerInfo taskManagerInfo)
		{
			this.workerId = workerId;
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
					WorkerId = workerId,
					MaxTasks = topicManagerInfo.MaxTasks,
					Topics = new List<FetchExternalTaskTopic>(){
					new FetchExternalTaskTopic(topicManagerInfo.TopicName, topicManagerInfo.LockDurationInMilliseconds){
						Variables = topicManagerInfo.VariablesToFetch == null ? topicManagerInfo.VariablesToFetch : null
						}
					}
				};
				var tasks = new List<LockedExternalTask>();
				getStandardPolicy("Fetch and Lock").Execute(() => { 
					tasks = externalTaskService.FetchAndLock(fetchExternalTasks).Result;
				});

				if(tasks.Count == 0 && timerRetrySeconds < topicManagerInfo.MaxTimeBetweenConnections){
					timerRetrySeconds = Convert.ToInt64(Math.Pow(2, baseRetryExp));
					baseRetryExp += 1;
				}
				else if(tasks.Count > 0) {
					baseRetryExp = 1;
					timerRetrySeconds = 1;
				}
				Console.WriteLine($"Fetch and locked {tasks.Count} tasks in topic {topicManagerInfo.TopicName}. Will try again in {timerRetrySeconds} seconds");

				// run them in parallel with a max degree of parallelism
				Parallel.ForEach(
					tasks,
					new ParallelOptions { MaxDegreeOfParallelism = topicManagerInfo.MaxDegreeOfParallelism },
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
					taskQueryTimer.Change(TimeSpan.FromMilliseconds(timerRetrySeconds*1000), TimeSpan.FromMilliseconds(Timeout.Infinite));
				}
			}
		}
		private void Execute(LockedExternalTask lockedExternalTask)
		{
			var resultVariables = new Dictionary<string, VariableValue>();

			Console.WriteLine($"Executing External Task {lockedExternalTask.Id} from topic {topicManagerInfo.TopicName}");
			try
			{
				topicManagerInfo.TaskAdapter.Execute(lockedExternalTask, ref resultVariables);				
				var completeExternalTask = new CompleteExternalTask()
				{
					WorkerId = workerId,
					Variables = resultVariables
				};
				getStandardPolicy("Complete").Execute(() => {
					externalTaskService[lockedExternalTask.Id].Complete(completeExternalTask).Wait();
					Console.WriteLine($"Finished External Task {lockedExternalTask.Id} from topic {topicManagerInfo.TopicName}...");
				});
			}
			catch (UnrecoverableBusinessErrorException ex)
			{
				Console.WriteLine($"Failed with business error code {ex.BusinessErrorCode} for External Task  {lockedExternalTask.Id} in topic {topicManagerInfo.TopicName}...");
				var externalTaskBpmnError = new ExternalTaskBpmnError
				{
					WorkerId = workerId,
					ErrorCode = ex.BusinessErrorCode,
					ErrorMessage = ex.Message
				};
				getStandardPolicy("Handle BPMN Error").Execute(() => externalTaskService[lockedExternalTask.Id].HandleBpmnError(externalTaskBpmnError).Wait());
			}
			catch (UnlockTaskException ex)
			{
				Console.WriteLine($"Unlock requested for External Task  {lockedExternalTask.Id} in topic {topicManagerInfo.TopicName}...");
				getStandardPolicy("Unlock Task").Execute( () => externalTaskService[lockedExternalTask.Id].Unlock().Wait());
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed External Task  {lockedExternalTask.Id} in topic {topicManagerInfo.TopicName}...");
				var retriesLeft = topicManagerInfo.Retries; // start with default
				if (lockedExternalTask.Retries.HasValue) // or decrement if retries are already set
				{
					retriesLeft = lockedExternalTask.Retries.Value - 1;
				}
				var externalTaskFailure = new ExternalTaskFailure()
				{
					WorkerId = workerId,
					Retries = retriesLeft,
					ErrorMessage = ex.Message,
					ErrorDetails = ex.StackTrace
				};
				getStandardPolicy("Handle Failure").Execute(() => externalTaskService[lockedExternalTask.Id].HandleFailure(externalTaskFailure).Wait());
			}
		}
		public void StartManager()
		{
			Console.WriteLine($"Starting manager for topic {topicManagerInfo.TopicName}...");
			this.taskQueryTimer = new Timer(_ => DoPolling(), null, timerRetrySeconds*1000, Timeout.Infinite);
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

		private Policy getStandardPolicy(string operation)
		{
			return Policy.HandleInner<HttpRequestException>()
				.WaitAndRetryForever(retryAttempt =>
				{
					var maxConnectionTimeout =  TimeSpan.FromSeconds(topicManagerInfo.MaxTimeBetweenConnections);
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
					Console.WriteLine($"Failed to {operation} in topic {topicManagerInfo.TopicName} with error {ex.GetType().ToString()}. Will try again in {span.Seconds} seconds...");
				}
			);
		}
	}
}