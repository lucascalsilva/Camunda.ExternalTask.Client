using System;
using System.Collections.Generic;

namespace Camunda.ExternalTask.Client.Adapter
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)
    ]
    public sealed class ExternalTaskVariableRequirementsAttribute : Attribute
    {
        public List<string> VariablesToFetch { get; }

        public ExternalTaskVariableRequirementsAttribute(){}

        public ExternalTaskVariableRequirementsAttribute(params string[] variablesToFetch)
        {
            VariablesToFetch = new List<string>(variablesToFetch);
        }

    }
}
