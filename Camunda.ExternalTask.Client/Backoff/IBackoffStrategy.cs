using System.Collections.Generic;
using Camunda.Api.Client.ExternalTask;

namespace Camunda.ExternalTask.Client.Backoff
{
	public interface IBackoffStrategy{
		void Reconfigure(long externalSizeCount);
		long Calculate();
	}
}