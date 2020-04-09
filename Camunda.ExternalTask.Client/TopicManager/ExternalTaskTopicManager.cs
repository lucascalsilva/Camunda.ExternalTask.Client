using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.Backoff;
using Camunda.ExternalTask.Client.PolicyManager;

namespace Camunda.ExternalTask.Client.TopicManager
{
	public class ExternalTaskTopicManager : IDisposable
	{
		private string workerId;
		private Timer taskQueryTimer;
		private IBackoffStrategy backoffStrategy;
		private IPolicyManager policyManager;
		private ExternalTaskService externalTaskService;

		private ExternalTaskTopicManagerInfo topicManagerInfo;

		public ExternalTaskTopicManager(string workerId, ExternalTaskService externalTaskService,
			ExternalTaskTopicManagerInfo taskManagerInfo) 
			: this(workerId, externalTaskService, taskManagerInfo, new ExponentialBackoff(500, 2, 64000)) {}
		public ExternalTaskTopicManager(string workerId, ExternalTaskService externalTaskService,
			ExternalTaskTopicManagerInfo taskManagerInfo, IBackoffStrategy backoffStrategy) 
			: this(workerId, externalTaskService, taskManagerInfo, backoffStrategy, new DefaultPolicyManager(backoffStrategy, taskManagerInfo.TopicName)) {}
		public ExternalTaskTopicManager(string workerId, ExternalTaskService externalTaskService,
			ExternalTaskTopicManagerInfo taskManagerInfo, IBackoffStrategy backoffStrategy, IPolicyManager policyManager)
		{
			this.workerId = workerId;
			this.externalTaskService = externalTaskService;
			this.topicManagerInfo = taskManagerInfo;
			this.backoffStrategy = backoffStrategy;
			this.policyManager = policyManager;
		}
		public void DoPolling()
		{
			// Query External Tasks
			var fetchAndLockBackoff = 0L;
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
				policyManager.fetchAndLockPolicy().Execute(() =>
				{
					tasks = externalTaskService.FetchAndLock(fetchExternalTasks).Result;
				});
				backoffStrategy.Reconfigure(tasks.Count);
				fetchAndLockBackoff = backoffStrategy.Calculate();
				Console.WriteLine($"Fetch and locked {tasks.Count} tasks in topic {topicManagerInfo.TopicName}. Will try again in {TimeSpan.FromMilliseconds(fetchAndLockBackoff)} seconds");

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
					taskQueryTimer.Change(TimeSpan.FromMilliseconds(fetchAndLockBackoff), TimeSpan.FromMilliseconds(Timeout.Infinite));
				}
			}
		}
		public void Execute(LockedExternalTask lockedExternalTask)
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
				policyManager.completePolicy().Execute(() =>
				{
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
				policyManager.handleBpmnErrorPolicy().Execute(() => externalTaskService[lockedExternalTask.Id].HandleBpmnError(externalTaskBpmnError).Wait());
			}
			catch (UnlockTaskException ex)
			{
				Console.WriteLine($"Unlock requested for External Task  {lockedExternalTask.Id} in topic {topicManagerInfo.TopicName}...");
				policyManager.unlockPolicy().Execute(() => externalTaskService[lockedExternalTask.Id].Unlock().Wait());
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
				policyManager.handleFailurePolicy().Execute(() => externalTaskService[lockedExternalTask.Id].HandleFailure(externalTaskFailure).Wait());
			}
		}
		public void StartManager()
		{
			Console.WriteLine($"Starting manager for topic {topicManagerInfo.TopicName}...");
			this.taskQueryTimer = new Timer(_ => DoPolling(), null, 0, Timeout.Infinite);
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
	}
}