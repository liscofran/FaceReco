using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognition.Models;
using FaceRecognition.Services;

namespace FaceRecognition.Pages
{
    public class FaceListModel : PageModel
    {
        public IFormFile? imageFile { get; set; }
        [TempData]
        public string? Message { get; set; }

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

                 // Elimina il file 'representations_vgg_face.pkl' se esiste
                string pklFilePath = Path.Combine("wwwroot", "imgs",  "representations_vgg_face.pkl");
                if (System.IO.File.Exists(pklFilePath))
                {
                    System.IO.File.Delete(pklFilePath);
                }

                Message = "Utente creato con successo e immagine salvata."; // Set the TempData message

                return RedirectToPage(); // Redirect to the same page
            }

            return RedirectToPage("/Verify");
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

    }
}