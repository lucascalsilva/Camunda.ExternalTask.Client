using System;

namespace Camunda.ExternalTask.Client.TopicManager
{
    [Serializable]
    public class UnlockTaskException : Exception
    {
       
        public UnlockTaskException() : base("Task needs to be unlocked")
        {
            
        }

    }
}