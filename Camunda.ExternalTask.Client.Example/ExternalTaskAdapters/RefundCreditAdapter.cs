using System;
using System.Collections.Generic;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.Worker;

namespace Camunda.ExternalTask.Client.Example.ExternalTaskAdapters
{
	[ExternalTaskTopic("refund-customer-credit")]
	[ExternalTaskVariableRequirements()]
	class RefundCreditAdapter : IExternalTaskAdapter
	{
		public void Execute(LockedExternalTask externalTask, ref Dictionary<string, VariableValue> resultVariables)
		{
			Console.WriteLine("Refunding customer credit...");
		}
	}
}
