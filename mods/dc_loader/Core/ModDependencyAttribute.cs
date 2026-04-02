using System;

namespace DCLoader.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ModDependencyAttribute : Attribute
    {
        public string ModID { get; }

        public ModDependencyAttribute(string modID)
        {
            ModID = modID;
        }
    }
}
