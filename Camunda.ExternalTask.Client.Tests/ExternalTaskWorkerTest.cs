using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;
using NSubstitute;
using Camunda.ExternalTask.Client.TopicManager;
using Camunda.Api.Client.ExternalTask;

namespace Camunda.ExternalTask.Client.Tests
{
    public class ExternalTaskWorkerTest
    {
        [Fact]
        public void DoPollingHappyPath()
        {
            //mock dependencies
			var externalTaskService = Substitute.For<ExternalTaskService>();

            var clientConfig = new ExternalTaskClientConfig();
            clientConfig.WorkerId = "DemoWorker";
            clientConfig.LockDurationInMilliseconds = 1000;
            clientConfig.MaxDegreeOfParallelism = 1;

            var managerInfo = new ExternalTaskTopicManagerInfo();
            managerInfo.TopicName = "TestTopic";
            List<String> variablesToFetch = new List<String>();
            variablesToFetch.Add("Var1");
            managerInfo.VariablesToFetch = variablesToFetch;

            

            //set up the ExternalTaskWorker under test
            var worker = Substitute.ForPartsOf<ExternalTaskTopicManager>(clientConfig, externalTaskService, managerInfo);


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
