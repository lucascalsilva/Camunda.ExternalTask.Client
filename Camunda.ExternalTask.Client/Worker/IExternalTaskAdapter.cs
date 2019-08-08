using System.Collections.Generic;
using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;

namespace Camunda.ExternalTask.Client.Worker
{
    public interface IExternalTaskAdapter
    {
        void Execute(LockedExternalTask externalTask, ref Dictionary<string, VariableValue> resultVariables);
    }
}
