using System;
using System.IO;
using UnityEngine;

namespace Nexus.Persistence01
{
    // One local record. No profiles, slots, migrations or shared persistence architecture.
    public sealed class Persistence01StateStore
    {
        public const string TestPathVariable = "NEXUS_PERSISTENCE01_TEST_PATH";
        private readonly Action<string> warning;
        public string FilePath { get; }
        public static string DefaultPath => Path.Combine(Application.persistentDataPath, "Persistence01", "outcome.json");
        public static string RuntimePath
        {
            get
            {
                // Process-local test injection survives scene and Editor domain reloads.
                // Player builds always use the experiment's actual persistentDataPath.
#if UNITY_EDITOR
                string testPath = Environment.GetEnvironmentVariable(TestPathVariable);
                if (!string.IsNullOrEmpty(testPath)) return testPath;
#endif
                return DefaultPath;
            }
        }
        public Persistence01StateStore(string path, Action<string> warning = null)
        { FilePath = Path.GetFullPath(path); this.warning = warning; }

        public Persistence01State Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                if (new FileInfo(FilePath).Length > 4096) return Invalid();
                string json = File.ReadAllText(FilePath);
                if (string.IsNullOrWhiteSpace(json)) return Invalid();
                var state = JsonUtility.FromJson<Persistence01State>(json);
                return state != null && state.IsValid ? state : Invalid();
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException)
            { return Invalid(); }
        }
        private Persistence01State Invalid()
        { warning?.Invoke("Persistence01: unreadable or invalid memory; starting clean. Original file retained until save or clear."); return null; }

        public bool Save(Persistence01State state)
        {
            if (state == null || !state.IsValid) throw new ArgumentException("Only complete factual outcomes may be saved.", nameof(state));
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(state, true));
                // Publish only after the full small JSON has been written and closed.
                if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
                else File.Move(temporary, FilePath);
                return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { warning?.Invoke("Persistence01: memory was not saved. Check local file access and retry."); return false; }
        }
        public bool Clear()
        {
            try
            {
                // Exact experiment files only. Never recursively delete persistentDataPath.
                File.Delete(FilePath); File.Delete(FilePath + ".tmp"); return true;
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
            { warning?.Invoke("Persistence01: memory could not be cleared. Check local file access."); return false; }
        }
    }
}
