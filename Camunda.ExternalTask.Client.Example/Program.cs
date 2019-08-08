using System;
using Camunda.ExternalTask.Client;

namespace Camunda.ExternalTask.Client.Example
{
    class Program
    {
        static void Main(string[] args)
        {
            ExternalTaskClient externalTaskClient = new ExternalTaskClient();
            externalTaskClient.Startup();
            Console.ReadLine();
            externalTaskClient.Shutdown();
        }
    }
}
