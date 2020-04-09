using System;

namespace Camunda.ExternalTask.Client.Backoff
{
	public class ExponentialBackoff : IBackoffStrategy
	{
		private long initTime;
		private double factor;
		private double maxTime;
		private long level;
		public ExponentialBackoff(long initTime, double factor, long maxTime)
		{
			this.initTime = initTime;
			this.factor = factor;
			this.maxTime = maxTime;
			this.level = 0;
		}
		public void Reconfigure(long externalSizeCount)
		{
			if (externalSizeCount == 0)
			{
				level++;
			}
			else
			{
				level = 0;
			}
		}

		public long Calculate()
		{
			if (level == 0)
			{
				return 0;
			}
			double backoffTime = initTime * Math.Pow(factor, level - 1);
			return Convert.ToInt64(Math.Min(backoffTime, maxTime));
		}
	}
}