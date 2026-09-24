using System;
using System.IO;
using Farm.Simulation;
using UnityEngine;

namespace Farm.UnityAdapters
{
    public sealed class JsonProgressStore : IProgressStore
    {
        private readonly string path;
        public JsonProgressStore(string fileName = "farm-progress.json")
            => path = Path.Combine(Application.persistentDataPath, fileName);

        public bool TryLoad(out ProgressSnapshot snapshot, out string error)
        {
            snapshot = null;
            error = null;
            if (!File.Exists(path))
            {
                if (File.Exists(path + ".bak"))
                    error = "Primary save is missing but a backup exists; it was preserved and will not be overwritten.";
                return false;
            }
            try
            {
                snapshot = JsonUtility.FromJson<ProgressSnapshot>(File.ReadAllText(path));
                if (snapshot == null || snapshot.schemaVersion != ProgressSnapshot.CurrentSchemaVersion)
                {
                    snapshot = null;
                    error = "Save file has an unsupported schema; it was preserved and will not be overwritten.";
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                error = "Save file could not be read; it was preserved and will not be overwritten. " + exception.Message;
                return false;
            }
        }

        public void Save(ProgressSnapshot snapshot)
        {
            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            var temp = path + ".tmp";
            var backup = path + ".bak";
            File.WriteAllText(temp, JsonUtility.ToJson(snapshot, true));
            if (!File.Exists(path)) { File.Move(temp, path); return; }
            try { File.Replace(temp, path, backup, true); }
            catch (PlatformNotSupportedException)
            {
                File.Copy(path, backup, true);
                File.Delete(path);
                File.Move(temp, path);
            }
        }
    }
}
