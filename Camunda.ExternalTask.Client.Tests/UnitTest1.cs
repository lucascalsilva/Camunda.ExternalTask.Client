using System;
using Xunit;
using NSubstitute;
using Camunda.ExternalTask.Client.Worker;

namespace Camunda.ExternalTask.Client.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Substitute.For<IExternalTaskAdapter>();

        }
    }
}