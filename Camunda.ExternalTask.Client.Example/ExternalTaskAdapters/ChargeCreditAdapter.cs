using System;
using System.Collections.Generic;
using System.Threading;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.Adapter;

namespace Camunda.ExternalTask.Client.Example.ExternalTaskAdapters
{
	[ExternalTaskTopic("charge-credit")]
	class ChargeCreditAdapter : IExternalTaskAdapter
	{
		public void Execute(LockedExternalTask externalTask, ref Dictionary<string, VariableValue> resultVariables)
		{
			Thread.Sleep(8000);
			var amountLeft = ((double)externalTask.Variables["amount"].Value) - 1000;
			resultVariables.Add("amountLeft", VariableValue.FromObject(amountLeft));
			Console.WriteLine("Charging credit...");
		}
	}
}
