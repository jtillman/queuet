using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Threading.Tasks;

namespace AspNetCoreWebApp.Pages
{
    public static class RenderExtensions
    {
        public const string ComponentNameKey = "ComponentName";

        public static async Task<IHtmlContent> PartialComponentAsync(this IHtmlHelper helper, params string[] fileNames)
        {
            IHtmlContentBuilder content = new HtmlContentBuilder();

            foreach(var fileName in fileNames)
                content = content.AppendHtml(await helper.PartialAsync($"~/Pages/components/{fileName}.cshtml", new { ComponentName = fileName }));
            return content;
        }

        public static string GetComponentName(this ViewDataDictionary<dynamic> data)
        {
            return data.Eval(ComponentNameKey) as string;
        }
    }
}
