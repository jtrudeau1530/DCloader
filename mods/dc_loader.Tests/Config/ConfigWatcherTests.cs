using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using DCLoader.Config;
using DCLoader.Core;
using Xunit;

namespace DCLoader.Tests.Config
{
    public class ConfigWatcherTests : IDisposable
    {
        private readonly string _tempDir;

        public ConfigWatcherTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"watchertest_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
            DcLogger.Initialize(Path.Combine(_tempDir, "logs"));
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }

        private ConfigFile MakeFile(string modId, Dictionary<string, ConfigFile> dict = null)
        {
            var file = new ConfigFile(modId, _tempDir);
            dict?.Add(modId, file);
            return file;
        }

        // ---- Constructor ----

        [Fact]
        public void Constructor_AcceptsDictionary()
        {
            var dict = new Dictionary<string, ConfigFile>();
            MakeFile("mod1", dict);

            using var watcher = new ConfigWatcher(dict);
            // Should not throw
        }

        // ---- Start ----

        [Fact]
        public void Start_WhenDirExists_DoesNotThrow()
        {
            var dict = new Dictionary<string, ConfigFile>();
            MakeFile("mod1", dict);

            using var watcher = new ConfigWatcher(dict);
            Exception ex = null;
            try { watcher.Start(_tempDir); }
            catch (Exception e) { ex = e; }
            Assert.Null(ex);
        }

        [Fact]
        public void Start_WhenDirDoesNotExist_DoesNotThrow()
        {
            var dict = new Dictionary<string, ConfigFile>();
            using var watcher = new ConfigWatcher(dict);
            var nonExistent = Path.Combine(_tempDir, "does_not_exist");
            Exception ex = null;
            try { watcher.Start(nonExistent); }
            catch (Exception e) { ex = e; }
            Assert.Null(ex);
        }

        // ---- Live reload ----

        [Fact]
        public void FileChange_TriggersReloadAndFiresOnValueChanged()
        {
            var dict = new Dictionary<string, ConfigFile>();
            var file = MakeFile("mymod", dict);
            var entry = file.Bind<float>("damage", 1.5f, "desc");
            file.Save();

            float newVal = 0f;
            var fired = new ManualResetEventSlim(false);
            entry.OnValueChanged += (o, n) =>
            {
                newVal = n;
                fired.Set();
            };

            using var watcher = new ConfigWatcher(dict);
            watcher.Start(_tempDir);

            // Write a changed value to the TOML file
            File.WriteAllText(Path.Combine(_tempDir, "mymod.toml"), "damage = 9.9\n");

            // Wait up to 1500 ms for reload to fire (debounce is 300ms)
            bool gotEvent = fired.Wait(1500);

            Assert.True(gotEvent, "OnValueChanged did not fire within 1500ms of file change");
            Assert.Equal(9.9f, newVal, precision: 4);
        }

        [Fact]
        public void FileChange_UnknownModId_DoesNotThrow()
        {
            var dict = new Dictionary<string, ConfigFile>();
            // empty dict — no mod registered

            using var watcher = new ConfigWatcher(dict);
            watcher.Start(_tempDir);

            Exception ex = null;
            try
            {
                File.WriteAllText(Path.Combine(_tempDir, "unknown_mod.toml"), "x = 1\n");
                Thread.Sleep(600); // wait past debounce
            }
            catch (Exception e) { ex = e; }

            Assert.Null(ex);
        }

        // ---- Dispose ----

        [Fact]
        public void Dispose_DoesNotThrow()
        {
            var dict = new Dictionary<string, ConfigFile>();
            MakeFile("mod1", dict);

            var watcher = new ConfigWatcher(dict);
            watcher.Start(_tempDir);

            Exception ex = null;
            try { watcher.Dispose(); }
            catch (Exception e) { ex = e; }
            Assert.Null(ex);
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var dict = new Dictionary<string, ConfigFile>();
            var watcher = new ConfigWatcher(dict);
            watcher.Start(_tempDir);
            watcher.Dispose();

            Exception ex = null;
            try { watcher.Dispose(); }
            catch (Exception e) { ex = e; }
            Assert.Null(ex);
        }
    }
}
