namespace Camunda.ExternalTask.Client.PolicyManager
{
    public enum Operation
    {
        FETCH_AND_LOCK, COMPLETE, UNLOCK, HANDLE_FAILURE, HANDLE_BPMN_ERROR
    }
}