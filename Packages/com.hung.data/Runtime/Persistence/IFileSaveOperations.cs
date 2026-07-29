using System.IO;

namespace Hung.Data.Persistence
{
    public interface IFileSaveOperations
    {
        void CreateDirectory(string path);
        bool FileExists(string path);
        byte[] ReadAllBytes(string path);
        Stream CreateNew(string path);
        void Copy(string sourcePath, string destinationPath, bool overwrite);
        void Move(string sourcePath, string destinationPath);
        void Delete(string path);
    }

    public sealed class SystemFileSaveOperations : IFileSaveOperations
    {
        public void CreateDirectory(string path) => Directory.CreateDirectory(path);
        public bool FileExists(string path) => File.Exists(path);
        public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
        public Stream CreateNew(string path) => new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        public void Copy(string sourcePath, string destinationPath, bool overwrite) => File.Copy(sourcePath, destinationPath, overwrite);
        public void Move(string sourcePath, string destinationPath) => File.Move(sourcePath, destinationPath);
        public void Delete(string path) => File.Delete(path);
    }
}
