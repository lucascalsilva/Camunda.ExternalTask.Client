﻿using System;

namespace Camunda.ExternalTask.Client.TopicManager
{
    [Serializable]
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
