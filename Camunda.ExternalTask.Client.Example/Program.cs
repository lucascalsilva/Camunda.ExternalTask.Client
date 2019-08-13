using System;

namespace Camunda.ExternalTask.Client.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            IExternalTaskClient externalTaskClient = ExternalTaskClientBuilder.Create()
                .WorkerId("DOT-NET-WORKER").Build();
            externalTaskClient.Startup();
            Console.ReadLine();
            externalTaskClient.Shutdown();
        }
    }
}
