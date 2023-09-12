using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognition.Models;
using FaceRecognition.Services;
using System.Text.Json;
using System.Text;


namespace FaceRecognition.Pages
{
    public class FaceListModel : PageModel
    {
        public IFormFile? imageFile { get; set; }
        [TempData]
        public string? Message { get; set; }
        public string? Error { get; set; }
        [BindProperty] 
        public FaceUser NewFace { get; set; } = default!;
        private readonly FaceService _service;
        public IList<FaceUser> FaceList { get;set; } = default!;
        private const string alphanumericCharacters ="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        public FaceListModel(FaceService service)
        {
            _service = service;
        }

        public IActionResult OnPostFoto()
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                if (ControllaImmagine(imageFile))
                {
                    NewFace.Id = GenerateRandomString(10);

                    string folderName = NewFace.Id;
                    string imageFileName = NewFace.Id + ".jpg";

                    string imageDirectory = Path.Combine("wwwroot", "imgs", folderName);

                    if (!Directory.Exists(imageDirectory))
                    {
                        Directory.CreateDirectory(imageDirectory);
                    }

                    string imagePath = Path.Combine(imageDirectory, imageFileName);

                    using (var memoryStream = new MemoryStream())
                    {
                        imageFile.CopyTo(memoryStream);
                        byte[] imageBytes = memoryStream.ToArray();
                        System.IO.File.WriteAllBytes(imagePath, imageBytes);
                    }

                    if (!ModelState.IsValid || NewFace == null)
                        return Page();

                    _service.AddFaceUsers(NewFace);

                    Message = "Utente creato con successo e immagine salvata."; // Set the TempData message

                    // Elimina il file 'representations_vgg_face.pkl' se esiste
                    string pklFilePath = Path.Combine("wwwroot", "imgs",  "representations_vgg_face.pkl");
                    if (System.IO.File.Exists(pklFilePath))
                    {
                        System.IO.File.Delete(pklFilePath);
                    }
                }
            }

            return Page();
        }

        public static string GenerateRandomString(int length)
        {
            string timestamp = DateTime.Now.Ticks.ToString();
            Random random = new();
            
            char[] result = new char[length];
            
            for (int i = 0; i < length; i++)
            {
                if (i < timestamp.Length)
                {
                    result[i] = timestamp[i];
                }
                else
                {
                    result[i] = alphanumericCharacters[random.Next(alphanumericCharacters.Length)];
                }
            }
            
            return new string(result);
        }

        public bool ControllaImmagine(IFormFile? image)
        {
            // Imposta il percorso di destinazione per salvare l'immagine 
                string uploadsFolder = Path.Combine("wwwroot", "tmps");
                string uniqueFileName = Guid.NewGuid().ToString() + "_webcam.jpg";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Salva l'immagine sul server
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                   image.CopyTo(stream); 
                }

                using var httpClient = new HttpClient();

                var json = new
                {
                    imagePath = filePath
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(json), Encoding.UTF8, "application/json");

                var response = httpClient.PostAsync("http://127.0.0.1:5000/api/verifica", jsonContent).Result;

                // Elimina il file temporaneo
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;
                   
                    if (result == "0")
                    {
                        return true;
                    }
                    else if(result.Length == 10)
                    {
                        Error = "Errore, volto già registrato"; // Set the TempData message
                        return false;
                    }
                    {
                        Error = "Errore, impossibile identificare un volto"; // Set the TempData message
                        return false;
                    }
                }

            Error = "Errore, si prega di riprovare"; // Set the TempData message
            return false;
        }
    }
}