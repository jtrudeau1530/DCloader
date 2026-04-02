using System;
using DCLoader.Config;
using Xunit;

namespace DCLoader.Tests.Config
{
    public class ConfigEntryTests
    {
        [Fact]
        public void Value_ReturnsDefaultOnConstruction()
        {
            var entry = CreateEntry("speed", "fast");
            Assert.Equal("fast", entry.Value);
        }

        [Fact]
        public void Value_Set_UpdatesValue()
        {
            var entry = CreateEntry("speed", "fast");
            entry.Value = "slow";
            Assert.Equal("slow", entry.Value);
        }

        [Fact]
        public void Value_Set_FiresOnValueChanged()
        {
            var entry = CreateEntry("speed", "fast");
            string oldFired = null;
            string newFired = null;
            entry.OnValueChanged += (o, n) => { oldFired = o; newFired = n; };

            entry.Value = "slow";

            Assert.Equal("fast", oldFired);
            Assert.Equal("slow", newFired);
        }

        [Fact]
        public void Value_SetToSameValue_DoesNotFireOnValueChanged()
        {
            var entry = CreateEntry("speed", "fast");
            int callCount = 0;
            entry.OnValueChanged += (o, n) => callCount++;

            entry.Value = "fast";

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void InternalSet_UpdatesValue()
        {
            var entry = CreateEntry("speed", "fast");
            entry.InternalSet("overridden");
            Assert.Equal("overridden", entry.Value);
        }

        [Fact]
        public void InternalSet_DoesNotFireOnValueChanged()
        {
            var entry = CreateEntry("speed", "fast");
            int callCount = 0;
            entry.OnValueChanged += (o, n) => callCount++;

            entry.InternalSet("overridden");

            Assert.Equal(0, callCount);
        }

        [Fact]
        public void Key_MatchesConstructorArg()
        {
            var entry = CreateEntry("mykey", "val");
            Assert.Equal("mykey", entry.Key);
        }

        [Fact]
        public void DefaultValue_MatchesConstructorArg()
        {
            var entry = CreateEntry("speed", "fast");
            Assert.Equal("fast", entry.DefaultValue);
        }

        [Fact]
        public void Description_MatchesConstructorArg()
        {
            var entry = CreateEntryWithDesc("speed", "fast", "Speed of the thing");
            Assert.Equal("Speed of the thing", entry.Description);
        }

        // Helper: uses internal constructor via reflection since ConfigEntry<T> ctor is internal
        private static ConfigEntry<string> CreateEntry(string key, string defaultValue)
            => CreateEntryWithDesc(key, defaultValue, "");

        private static ConfigEntry<string> CreateEntryWithDesc(string key, string defaultValue, string description)
        {
            // ConfigEntry<T> has an internal constructor — invoke via Activator
            var ctor = typeof(ConfigEntry<string>).GetConstructors(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)[0];
            return (ConfigEntry<string>)ctor.Invoke(new object[] { key, defaultValue, description });
        }
    }
}
