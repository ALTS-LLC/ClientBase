using System;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class DisableIfAttribute : PropertyAttribute
    {
        public string ConditionFieldName { get; private set; }
        public bool Condition { get; private set; }

        public DisableIfAttribute(string conditionFieldName, bool condition = false)
        {
            this.ConditionFieldName = conditionFieldName;
            this.Condition = condition;
        }
    }
}