using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Microsoft.Azure.WebJobs.Script.WebHost
{
    public interface ISecretsRepository
    {
        event EventHandler<SecretsChangedEventArgs> SecretsChanged;

        Task<string> ReadAsync(ScriptSecretsType type, string name);

        Task WriteAsync(ScriptSecretsType type, string name);

        Task<bool> DeleteSecretsAsync(ScriptSecretsType type, string name);
    }
}