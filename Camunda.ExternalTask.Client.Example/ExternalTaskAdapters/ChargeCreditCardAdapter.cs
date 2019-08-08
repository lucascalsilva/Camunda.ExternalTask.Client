using System.Collections.Generic;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.ExternalTask.Client.Worker;

namespace Camunda.ExternalTask.Client.Example.ExternalTaskAdapters
{
  [ExternalTaskTopic("charge-credit-card", "DOT-NET-WORKER")]
  [ExternalTaskVariableRequirements("shouldFail")]
  class ChargeCreditCardAdapter : IExternalTaskAdapter
  {
		public void Execute(LockedExternalTask externalTask, ref Dictionary<string, VariableValue> resultVariables)
		{
			bool shouldFail = (bool) externalTask.Variables["shouldFail"].Value;
      if(shouldFail){
        throw new UnrecoverableBusinessErrorException("CreditCardFailedError", "Could not charge credit card");
      }
		}
	}
}