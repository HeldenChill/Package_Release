using System;
using System.IO;
using System.Text;

namespace Hung.Data.Persistence
{
    public sealed class FileLegacySaveSource : ILegacySaveSource
    {
        private readonly string root;
        private readonly IFileSaveOperations operations;

        public FileLegacySaveSource(string root, IFileSaveOperations operations = null)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.operations = operations ?? new SystemFileSaveOperations();
        }

        public bool TryRead(string key, out string json)
        {
            try
            {
                string path = Path.Combine(root, key);
                if (!operations.FileExists(path))
                {
                    json = null;
                    return false;
                }

                byte[] bytes = operations.ReadAllBytes(path);
                json = Encoding.UTF8.GetString(bytes);
                return true;
            }
            catch
            {
                json = null;
                return false;
            }
        }
    }
}
