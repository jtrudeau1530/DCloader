using System;

namespace DCLoader.Core
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ModInfoAttribute : Attribute
    {
        public string ID { get; }
        public string Name { get; }
        public string Version { get; }
        public string Author { get; }
        public string Description { get; }

        public ModInfoAttribute(string id, string name, string version,
                                string author = "", string description = "")
        {
            ID = id;
            Name = name;
            Version = version;
            Author = author;
            Description = description;
        }
    }
}
