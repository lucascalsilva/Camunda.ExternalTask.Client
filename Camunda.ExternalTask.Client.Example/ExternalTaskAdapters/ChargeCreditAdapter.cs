using System;
using System.Collections.Generic;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.Worker;

namespace Camunda.ExternalTask.Client.Example.ExternalTaskAdapters
{
  [ExternalTaskTopic("charge-credit", "DOT-NET-WORKER")]
  [ExternalTaskVariableRequirements("amount","shouldFail")]
  class ChargeCreditAdapter : IExternalTaskAdapter
  {
		public void Execute(LockedExternalTask externalTask, ref Dictionary<string, VariableValue> resultVariables)
		{
			var amountLeft = ((double) externalTask.Variables["amount"].Value) - 1000;
			resultVariables.Add("amountLeft", VariableValue.FromObject(amountLeft));
			Console.WriteLine("Charging credit...");
		}
	}
}
