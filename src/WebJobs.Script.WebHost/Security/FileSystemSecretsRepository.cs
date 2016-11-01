using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public class FileSystemSecretsRepository : ISecretsRepository
    {
        private readonly string _secretsPath;
        private readonly string _hostSecretsPath;
        private readonly FileSystemWatcher _fileWatcher;

        public event EventHandler<SecretsChangedEventArgs> SecretsChanged;

        public FileSystemSecretsRepository(string secretsPath)
        {
            _secretsPath = secretsPath;
            _hostSecretsPath = Path.Combine(_secretsPath, ScriptConstants.HostMetadataFileName);

            Directory.CreateDirectory(_secretsPath);

            _fileWatcher = new FileSystemWatcher(_secretsPath, "*.json")
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += OnChanged;
            _fileWatcher.Created += OnChanged;
            _fileWatcher.Deleted += OnChanged;
            _fileWatcher.Renamed += OnChanged;
        }

        public Task<bool> DeleteSecretsAsync(ScriptSecretsType type, string name)
        {
            throw new NotImplementedException();
        }

        private string GetFunctionSecretsFilePath(string functionName)
        {
            string secretFileName = string.Format(CultureInfo.InvariantCulture, "{0}.json", functionName);
            return Path.Combine(_secretsPath, secretFileName);
        }

        public Task<string> ReadAsync(ScriptSecretsType type, string name)
        {
            throw new NotImplementedException();
        }

        public Task WriteAsync(ScriptSecretsType type, string name)
        {
            throw new NotImplementedException();
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            var changeHandler = SecretsChanged;
            if (changeHandler != null)
            {
                var args = new SecretsChangedEventArgs { Type = ScriptSecretsType.Host };

                if (string.Compare(Path.GetFileName(e.FullPath), ScriptConstants.HostMetadataFileName, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    args.Type = ScriptSecretsType.Function;
                    args.Name = Path.GetFileNameWithoutExtension(e.FullPath).ToLowerInvariant();
                }

                changeHandler(this, args);
            }
        }
    }
}