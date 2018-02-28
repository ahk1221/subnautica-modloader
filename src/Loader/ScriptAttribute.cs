using System;

namespace Loader
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ScriptAttribute : Attribute
    {
        public ScriptStartup Start { get; }

        public ScriptAttribute(ScriptStartup start)
        {
            Start = start;
        }
    }
}