using System;
using System.Collections.Generic;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.Adapter;

namespace Camunda.ExternalTask.Client.Example.ExternalTaskAdapters
{
	[ExternalTaskTopic("refund-customer-credit")]
	class RefundCreditAdapter : IExternalTaskAdapter
	{
		public void Execute(LockedExternalTask externalTask, ref Dictionary<string, VariableValue> resultVariables)
		{
			Console.WriteLine("Refunding customer credit...");
		}
	}
}
