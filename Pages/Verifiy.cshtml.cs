using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognition.Services;
using FaceRecognition.Models;
using System.Text;
using System.Text.Json;


namespace FaceRecognition.Pages
{
    public class VerifyModel : PageModel
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly FaceService _service;
        public IFormFile? imageFile { get; set; }

        public VerifyModel(IWebHostEnvironment webHostEnvironment, FaceService service)
        {
            _webHostEnvironment = webHostEnvironment;
            _service = service;
        }

        public void OnGet()
        {
            // This method will handle GET requests (when the page is loaded)
        }

        public IActionResult OnPostFoto()
        {

            using var httpClient = new HttpClient();

            var response = httpClient.PostAsync("http://127.0.0.1:5000/api/analisi", null).Result;

            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;

                if (result.Length == 10)
                {
                    FaceUser user = _service.GetFaceUser(result);

                    if(user != null)
                    {
                        TempData["Result"] = "Benvenuto " + user.Nome;
                        TempData["ShowMessageLink"] = true; 
                    }
                    else
                    {
                        TempData["Result"] = "Utente non trovato, si prega di riprovare";
                        TempData["ShowMessageLink"] = true; 
                    }            
                }
                else
                {
                    TempData["Result"] = result;
                    TempData["ShowMessageLink"] = true; 
                }
            }
            else
            {
                TempData["Result"] = "Errore nella richiesta POST";
            }

            return RedirectToPage("/Verify");
        }
    }
}