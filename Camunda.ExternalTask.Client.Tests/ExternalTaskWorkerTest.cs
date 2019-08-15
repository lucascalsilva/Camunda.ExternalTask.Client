using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using NSubstitute;
using Camunda.ExternalTask.Client.Worker;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.DTO;

namespace Camunda.ExternalTask.Client.Tests
{
    public class ExternalTaskWorkerTest
    {
        [Fact]
        public void DoPollingHappyPath()
        {
            //mock dependencies
			var externalTaskService = Substitute.For<ExternalTaskService>();
            var externalTaskClient = Substitute.For<ExternalTaskClient>();
            var externalTaskWorkerInfo = Substitute.For<ExternalTaskWorkerInfo>();

            externalTaskWorkerInfo.TopicName.Returns("TestTopic");

            List<String> variablesToFetch = new List<String>();
            variablesToFetch.Add("Var1");
            externalTaskWorkerInfo.VariablesToFetch.Returns(variablesToFetch);
            externalTaskClient.LockDurationInMilliseconds.Returns(1000);
            externalTaskClient.MaxDegreeOfParallelism.Returns(1);

            //set up the ExternalTaskWorker under test
            var worker = Substitute.ForPartsOf<ExternalTaskWorker>(externalTaskClient, externalTaskService, externalTaskWorkerInfo);


            //set up the ExternalTaskService
            List<LockedExternalTask> letList = new List<LockedExternalTask>();
            letList.Add(Substitute.For<LockedExternalTask>());
            letList.Add(Substitute.For<LockedExternalTask>());
            letList.Add(Substitute.For<LockedExternalTask>());

            Func<List<LockedExternalTask>> getLetList = () =>  {
                    Console.WriteLine(">>>>> Executed getList Function !"); 
                    return letList;
                }; 

            Task<List<LockedExternalTask>>  task = new Task<List<LockedExternalTask>>(getLetList);
            task.Start();
            externalTaskService.FetchAndLock(Arg.Any<FetchExternalTasks>()).Returns(task);

            //set up the worker to not to call its Execute method
            worker.WhenForAnyArgs(x => x.Execute(Arg.Any<LockedExternalTask>())).DoNotCallBase(); // Make sure Send won't call real implementation

            //call the method under test
            worker.DoPolling();


            //assertionas
            externalTaskService.Received().FetchAndLock(Arg.Any<FetchExternalTasks>());
            worker.Received(3).Execute(Arg.Any<LockedExternalTask>());
        }
    }
}
