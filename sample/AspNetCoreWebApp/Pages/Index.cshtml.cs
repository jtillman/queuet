using Microsoft.AspNetCore.Mvc.RazorPages;
using QueueT.Tasks;

namespace AspNetCoreWebApp.Pages
{
    public class IndexModel : PageModel
    {
        IQueueTTaskService _taskService;

        public IndexModel(IQueueTTaskService taskService)
        {
            _taskService = taskService;
        }

        public void OnGet()
        {
        }
    }
}
