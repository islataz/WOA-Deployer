﻿namespace Deployer.Core.Requirements
{
    public class FulfilledRequirement
    {
        public FulfilledRequirement(string key, object value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }
        public RequirementKind Kind { get; }
        public object Value { get; }
    }
}