using Polly;

namespace Camunda.ExternalTask.Client.PolicyManager
{
    public interface IPolicyManager
    {
         Policy fetchAndLockPolicy();
         Policy completePolicy();
         Policy handleBpmnErrorPolicy();
         Policy unlockPolicy();
         Policy handleFailurePolicy();         
    }
}