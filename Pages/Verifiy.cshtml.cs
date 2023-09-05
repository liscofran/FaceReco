﻿using Microsoft.AspNetCore.Mvc;
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

        public async Task<IActionResult> OnPostFotoAsync()
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                // Imposta il percorso di destinazione per salvare l'immagine 
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "tmps");
                string uniqueFileName = Guid.NewGuid().ToString() + "_webcam.jpg";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Salva l'immagine sul server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                using var httpClient = new HttpClient();

                var json = new
                {
                    imagePath = filePath
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(json), Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync("http://127.0.0.1:5000/api/analisi", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();

                    if (result.Length == 10)
                    {
                        FaceUser user = _service.GetFaceUser(result);

                        if (user != null)
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
                    else if (result == "0")
                    {
                        TempData["Result"] = "Volto non riconosciuto, ti sei già registrato ?";
                        TempData["ShowMessageLink"] = true;
                    }
                    else
                    {
                        TempData["Result"] = "Impossibile riconoscere un volto, cambia immagine e riprova";
                        TempData["ShowMessageLink"] = true;
                    }
                }
                else
                {
                    TempData["Result"] = "Errore nella richiesta POST";
                    TempData["ShowMessageLink"] = true;
                }
                // Elimina il file temporaneo
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            return Page();
            }
        }
}