using System;
using System.IO;
using DCLoader.Config;
using DCLoader.Core;
using Xunit;

namespace DCLoader.Tests.Config
{
    public class ConfigFileTests : IDisposable
    {
        private readonly string _tempDir;

        public ConfigFileTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"cfgtest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            // Initialize DcPaths-like stub via ConfigFile constructor that accepts a dir
            DcLogger.Initialize(Path.Combine(_tempDir, "logs"));
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private ConfigFile MakeFile(string modId)
            => new ConfigFile(modId, _tempDir);

        // ---- Bind ----

        [Fact]
        public void Bind_ReturnsEntryWithDefaultValue()
        {
            var file = MakeFile("mymod");
            var entry = file.Bind<float>("damage", 1.5f, "Damage multiplier");
            Assert.Equal(1.5f, entry.Value);
        }

        [Fact]
        public void Bind_SameKeyTwice_ReturnsSameInstance()
        {
            var file = MakeFile("mymod");
            var a = file.Bind<float>("damage", 1.5f, "desc");
            var b = file.Bind<float>("damage", 2.0f, "desc2");
            Assert.Same(a, b);
        }

        // ---- Save ----

        [Fact]
        public void Save_CreatesTomlFile()
        {
            var file = MakeFile("mymod");
            file.Bind<float>("damage", 1.5f, "Damage multiplier");
            file.Save();

            Assert.True(File.Exists(Path.Combine(_tempDir, "mymod.toml")));
        }

        [Fact]
        public void Save_WritesKeyAndValue()
        {
            var file = MakeFile("mymod");
            file.Bind<float>("damage", 1.5f, "desc");
            file.Save();

            string content = File.ReadAllText(Path.Combine(_tempDir, "mymod.toml"));
            Assert.Contains("damage", content);
            Assert.Contains("1.5", content);
        }

        [Fact]
        public void Save_WritesDescriptionAsComment()
        {
            var file = MakeFile("mymod");
            file.Bind<float>("damage", 1.5f, "Damage multiplier");
            file.Save();

            string content = File.ReadAllText(Path.Combine(_tempDir, "mymod.toml"));
            Assert.Contains("Damage multiplier", content);
            Assert.Contains("#", content);
        }

        [Fact]
        public void Save_UsesAtomicWrite_NoTmpFileAfterwards()
        {
            var file = MakeFile("mymod");
            file.Bind<float>("x", 1f, "desc");
            file.Save();

            Assert.False(File.Exists(Path.Combine(_tempDir, "mymod.toml.tmp")));
        }

        // ---- Load ----

        [Fact]
        public void Load_AppliesPersistedValue()
        {
            // Write a TOML file manually
            File.WriteAllText(Path.Combine(_tempDir, "mymod.toml"),
                "damage = 2.0\n");

            var file = MakeFile("mymod");
            var entry = file.Bind<float>("damage", 1.5f, "desc");
            file.Load();

            Assert.Equal(2.0f, entry.Value);
        }

        [Fact]
        public void Load_MissingKey_LeavesDefault()
        {
            File.WriteAllText(Path.Combine(_tempDir, "mymod.toml"),
                "other_key = 99\n");

            var file = MakeFile("mymod");
            var entry = file.Bind<float>("damage", 1.5f, "desc");
            file.Load();

            Assert.Equal(1.5f, entry.Value);
        }

        [Fact]
        public void Load_NonExistentFile_DoesNotThrow()
        {
            var file = MakeFile("notexist");
            var entry = file.Bind<float>("x", 3.0f, "desc");

            var ex = Record.Exception(() => file.Load());
            Assert.Null(ex);
            Assert.Equal(3.0f, entry.Value);
        }

        [Fact]
        public void Load_DoesNotFireOnValueChanged()
        {
            File.WriteAllText(Path.Combine(_tempDir, "mymod.toml"),
                "damage = 2.0\n");

            var file = MakeFile("mymod");
            var entry = file.Bind<float>("damage", 1.5f, "desc");
            int callCount = 0;
            entry.OnValueChanged += (o, n) => callCount++;

            file.Load();

            Assert.Equal(0, callCount);
        }

        // ---- Auto-bind behaviour ----

        [Fact]
        public void Bind_WhenFileDoesNotExist_CallsSaveToCreateDefaults()
        {
            var file = MakeFile("newmod");
            file.Bind<int>("level", 10, "Starting level");

            Assert.True(File.Exists(Path.Combine(_tempDir, "newmod.toml")));
        }

        [Fact]
        public void Bind_WhenFileExists_LoadsPersistedValue()
        {
            File.WriteAllText(Path.Combine(_tempDir, "existmod.toml"),
                "level = 42\n");

            var file = MakeFile("existmod");
            var entry = file.Bind<int>("level", 10, "Starting level");

            Assert.Equal(42, entry.Value);
        }

        // ---- Reload ----

        [Fact]
        public void Reload_FiresOnValueChangedForChangedValues()
        {
            var file = MakeFile("mymod");
            var entry = file.Bind<float>("damage", 1.5f, "desc");
            file.Save();

            // Update file on disk
            File.WriteAllText(Path.Combine(_tempDir, "mymod.toml"),
                "damage = 9.9\n");

            float newVal = 0f;
            entry.OnValueChanged += (o, n) => newVal = n;
            file.Reload();

            Assert.Equal(9.9f, newVal, precision: 4);
        }

        [Fact]
        public void Reload_DoesNotFireOnValueChangedWhenValueUnchanged()
        {
            File.WriteAllText(Path.Combine(_tempDir, "mymod.toml"),
                "damage = 1.5\n");

            var file = MakeFile("mymod");
            var entry = file.Bind<float>("damage", 1.5f, "desc");

            int callCount = 0;
            entry.OnValueChanged += (o, n) => callCount++;
            file.Reload();

            Assert.Equal(0, callCount);
        }

        // ---- Type round-trips ----

        [Fact]
        public void RoundTrip_Bool()
        {
            var file = MakeFile("modx");
            file.Bind<bool>("flag", false, "desc");
            file.Save();

            var file2 = MakeFile("modx");
            var entry2 = file2.Bind<bool>("flag", true, "desc");
            file2.Load();

            Assert.False(entry2.Value);
        }

        [Fact]
        public void RoundTrip_Int()
        {
            var file = MakeFile("modx");
            file.Bind<int>("count", 7, "desc");
            file.Save();

            var file2 = MakeFile("modx");
            var entry2 = file2.Bind<int>("count", 0, "desc");
            file2.Load();

            Assert.Equal(7, entry2.Value);
        }

        [Fact]
        public void RoundTrip_Double()
        {
            var file = MakeFile("modx");
            file.Bind<double>("ratio", 3.14, "desc");
            file.Save();

            var file2 = MakeFile("modx");
            var entry2 = file2.Bind<double>("ratio", 0.0, "desc");
            file2.Load();

            Assert.Equal(3.14, entry2.Value, precision: 10);
        }

        [Fact]
        public void RoundTrip_String()
        {
            var file = MakeFile("modx");
            file.Bind<string>("name", "hello", "desc");
            file.Save();

            var file2 = MakeFile("modx");
            var entry2 = file2.Bind<string>("name", "", "desc");
            file2.Load();

            Assert.Equal("hello", entry2.Value);
        }

        private enum TestMode { Easy, Hard }

        [Fact]
        public void RoundTrip_Enum()
        {
            var file = MakeFile("modx");
            file.Bind<TestMode>("mode", TestMode.Easy, "desc");
            file.Save();

            var file2 = MakeFile("modx");
            var entry2 = file2.Bind<TestMode>("mode", TestMode.Hard, "desc");
            file2.Load();

            Assert.Equal(TestMode.Easy, entry2.Value);
        }
    }
}
