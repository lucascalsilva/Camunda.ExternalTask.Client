using System;

namespace Camunda.ExternalTask.Client.Worker
{
    public class UnrecoverableBusinessErrorException : Exception
    {
        public string BusinessErrorCode { get; set; }
        
        public UnrecoverableBusinessErrorException(string businessErrorCode, string message)
        : base(message)
        {
            BusinessErrorCode = businessErrorCode;
        }

    }
}
