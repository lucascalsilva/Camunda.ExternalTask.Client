using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.DTO;

namespace Camunda.ExternalTask.Client.Worker
{
	public class ExternalTaskWorker : IDisposable
	{
		private ExternalTaskClient client;
		private Timer taskQueryTimer;
		private ExternalTaskService externalTaskService;
		private ExternalTaskWorkerInfo taskWorkerInfo;

		public ExternalTaskWorker(ExternalTaskClient client, ExternalTaskService externalTaskService, ExternalTaskWorkerInfo taskWorkerInfo)
		{
			this.client = client;
			this.externalTaskService = externalTaskService;
			this.taskWorkerInfo = taskWorkerInfo;
		}
		public void DoPolling()
		{
			// Query External Tasks
			try
			{
				var fetchExternalTasks = new FetchExternalTasks()
				{
					WorkerId = client.WorkerId,
					MaxTasks = client.MaxTasks,
					Topics = new List<FetchExternalTaskTopic>(){
					new FetchExternalTaskTopic(taskWorkerInfo.TopicName, client.LockDurationInMilliseconds){
						Variables = taskWorkerInfo.VariablesToFetch.Count > 0 ? taskWorkerInfo.VariablesToFetch : null
						}
					}
				};
				var tasks = externalTaskService.FetchAndLock(fetchExternalTasks).Result;

				// run them in parallel with a max degree of parallelism
				Parallel.ForEach(
					tasks,
					new ParallelOptions { MaxDegreeOfParallelism = client.MaxDegreeOfParallelism },
					externalTask => Execute(externalTask)
				);
			}
			catch (EngineException ex)
			{
				// Most probably server is not running or request is invalid
				Console.WriteLine(ex.Message);
			}

			// schedule next run (if not stopped in between)
			if (taskQueryTimer != null)
			{
				taskQueryTimer.Change(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(Timeout.Infinite));
			}
		}
		public void Execute(LockedExternalTask lockedExternalTask)
		{
			var resultVariables = new Dictionary<string, VariableValue>();

			Console.WriteLine($"Execute External Task from topic '{taskWorkerInfo.TopicName}': {lockedExternalTask}...");
			try
			{
				taskWorkerInfo.TaskAdapter.Execute(lockedExternalTask, ref resultVariables);
				Console.WriteLine($"...finished External Task {lockedExternalTask.Id}");
				var completeExternalTask = new CompleteExternalTask()
				{
					WorkerId = client.WorkerId,
					Variables = resultVariables
				};
				externalTaskService.Complete(lockedExternalTask.Id, completeExternalTask);
			}
			catch (UnrecoverableBusinessErrorException ex)
			{
				Console.WriteLine($"...failed with business error code from External Task  {lockedExternalTask.Id}");
				var externalTaskBpmnError = new ExternalTaskBpmnError
				{
					WorkerId = client.WorkerId,
					ErrorCode = ex.BusinessErrorCode,
					ErrorMessage = ex.Message
				};
				externalTaskService.HandleBpmnError(lockedExternalTask.Id, externalTaskBpmnError);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"...failed External Task  {lockedExternalTask.Id}");
				var retriesLeft = taskWorkerInfo.Retries; // start with default
				if (lockedExternalTask.Retries.HasValue) // or decrement if retries are already set
				{
					retriesLeft = lockedExternalTask.Retries.Value - 1;
				}
				var externalTaskFailure = new ExternalTaskFailure(){
					WorkerId = client.WorkerId,
					Retries = retriesLeft,
					ErrorMessage = ex.Message,
					ErrorDetails = ex.StackTrace
				};
				externalTaskService.HandleFailure(lockedExternalTask.Id, externalTaskFailure);
			}
		}
		public void StartWork()
		{
			this.taskQueryTimer = new Timer(_ => DoPolling(), null, client.PollingIntervalInMilliseconds, Timeout.Infinite);
		}
		public void StopWork()
		{
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
