using System;
using System.IO;

namespace Hung.LiveOps.Energy
{
    /// <summary>
    /// File-backed <see cref="IEnergyStateStore"/>. Writes are crash-safe (temp file,
    /// flush, then atomic replace); corrupt/unsupported payloads are quarantined to a
    /// sibling evidence file rather than destroyed.
    /// </summary>
    internal sealed class LocalEnergyStateStore : IEnergyStateStore
    {
        private readonly string _filePath;
        private readonly string _tempFilePath;

        public LocalEnergyStateStore(string filePath)
        {
            _filePath = filePath;
            _tempFilePath = filePath + ".tmp";
        }

        public EnergyStateLoadResult Load()
        {
            // ponytail: orphaned temp from a crash mid-save never belongs to real state; drop it.
            if (File.Exists(_tempFilePath))
            {
                try { File.Delete(_tempFilePath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }

            if (!File.Exists(_filePath))
            {
                return EnergyStateLoadResult.NotFound();
            }

            string json = File.ReadAllText(_filePath);

            if (!EnergyStateMapper.TryParseJson(json, out EnergyState state, out EnergyStateDtoParseOutcome outcome))
            {
                Quarantine(json);
                return outcome == EnergyStateDtoParseOutcome.UnsupportedVersion
                    ? EnergyStateLoadResult.UnsupportedVersion()
                    : EnergyStateLoadResult.Corrupt();
            }

            return EnergyStateLoadResult.Loaded(state);
        }

        public bool Save(EnergyState state)
        {
            try
            {
                string directory = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = EnergyStateMapper.ToJson(state);

                using (FileStream stream = new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(json);
                    writer.Flush();
                    stream.Flush(true);
                }

                if (File.Exists(_filePath))
                {
                    File.Replace(_tempFilePath, _filePath, null);
                }
                else
                {
                    File.Move(_tempFilePath, _filePath);
                }

                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is ArgumentException || ex is NotSupportedException)
            {
                try { if (File.Exists(_tempFilePath)) File.Delete(_tempFilePath); } catch (Exception) { /* best effort cleanup */ }
                return false;
            }
        }

        private void Quarantine(string rawContent)
        {
            string evidencePath = $"{_filePath}.corrupt-{DateTime.UtcNow.Ticks}-{Guid.NewGuid():N}.json";
            try
            {
                File.WriteAllText(evidencePath, rawContent);
            }
            catch (Exception) { /* best-effort evidence capture; never block Load's result */ }
        }
    }
}
