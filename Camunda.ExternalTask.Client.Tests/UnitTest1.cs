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
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            //mock dependencies
			var externalTaskService = Substitute.For<ExternalTaskService>();
            var externalTaskClient = Substitute.For<ExternalTaskClient>();
            var externalTaskWorkerInfo = Substitute.For<ExternalTaskWorkerInfo>();

            //set up the ExternalTaskWorker under test
            var worker = Substitute.ForPartsOf<ExternalTaskWorker>(externalTaskClient, externalTaskService, externalTaskWorkerInfo);


            //set up the ExternalTaskService
            IList<LockedExternalTask> letList = new List<LockedExternalTask>();
            letList.Add(Substitute.For<LockedExternalTask>());
            letList.Add(Substitute.For<LockedExternalTask>());
            letList.Add(Substitute.For<LockedExternalTask>());

                Action<object> action = (object obj) =>
                                {
                                   Console.WriteLine("Hallo!");
                                };

            var listOfLockedExternalTasks = new Task(action, "alpha");

            externalTaskService.FetchAndLock(Arg.Any<FetchExternalTasks>()).Returns(listOfLockedExternalTasks);

            worker.WhenForAnyArgs(x => x.Execute(Arg.Any<LockedExternalTask>())).DoNotCallBase(); // Make sure Send won't call real implementation

            worker.DoPolling();


            //assertionas
            worker.Received(3).Execute(Arg.Any<LockedExternalTask>());

        }
    }
}
