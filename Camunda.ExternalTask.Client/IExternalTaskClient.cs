namespace Camunda.ExternalTask.Client
{
	public interface IExternalTaskClient
	{
		void Startup();

		void Shutdown();

		void StartWorkers();

		void StopWorkers();
	}
}