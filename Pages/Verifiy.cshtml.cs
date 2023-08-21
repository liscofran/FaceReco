using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FaceRecognition.Pages
{
    public class VerifyModel : PageModel
    {
        private readonly ILogger<VerifyModel> _logger;

        public VerifyModel(ILogger<VerifyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // This method will handle GET requests (when the page is loaded)
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var formContent = new StringContent("{\"key\": \"value\"}", Encoding.UTF8, "application/json");

            using var httpClient = new HttpClient();
            var response = await httpClient.PostAsync("http://127.0.0.1:5000/api/analisi", formContent);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                TempData["Result"] = result;
            }
            else
            {
                TempData["Result"] = "Errore nella richiesta POST";
            }

            return RedirectToAction("Verify");
        }
    }
}