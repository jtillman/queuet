using System.Threading.Tasks;

namespace AspNetCoreWebApp
{
    public interface IWorkspaceHubClient
    {
        Task FileStatusChanged(string fileId, string hash);
    }
}
