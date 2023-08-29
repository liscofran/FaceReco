using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net.Http.Headers;

namespace FaceRecognition.Pages
{
    public class VerifyModel : PageModel
    {
        private readonly ILogger<VerifyModel> _logger;
        public const string SessionKeyImageBase64 = "_Name";

        public VerifyModel(ILogger<VerifyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // This method will handle GET requests (when the page is loaded)
        }

        public void OnGetFoto(byte[] msg)
        {
            if (msg != null)
            {
                string base64Image = Convert.ToBase64String(msg);
                HttpContext.Session.SetString(SessionKeyImageBase64, base64Image);
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Convert the base64 image blob to byte array
            string? base64Image = HttpContext.Session.GetString(SessionKeyImageBase64);
            byte[] imageData = Convert.FromBase64String(base64Image);
                    
            using var httpClient = new HttpClient();
            
            // Create ByteArrayContent with the image data
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = await httpClient.PostAsync("http://127.0.0.1:5000/api/analisi", imageContent);

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