using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognition.Models;
using FaceRecognition.Services;
using Microsoft.AspNetCore.Http;
using System.Text;

namespace FaceRecognition.Pages
{
    public class FaceListModel : PageModel
    {
        public const string SessionKeyImageBase64 = "_Name";
        private const string alphanumericCharacters ="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        [BindProperty] 
        public FaceUser NewFace { get; set; } = default!;
        private readonly FaceService _service;
        public IList<FaceUser> FaceList { get;set; } = default!;
        public FaceListModel(FaceService service)
        {
            _service = service;
        }

        public IActionResult OnPost()
        {
            string? base64Image = HttpContext.Session.GetString(SessionKeyImageBase64);
            byte[] imageBytes = Convert.FromBase64String(base64Image);

            NewFace.Id = GenerateRandomString(10);

            string folderName = NewFace.Id;
            string imageFileName = NewFace.Id + ".jpg";
            
            string imageDirectory = Path.Combine("wwwroot", "imgs", folderName);
            if (!Directory.Exists(imageDirectory))
            {
                Directory.CreateDirectory(imageDirectory);
            }

            string imagePath = Path.Combine(imageDirectory, imageFileName);
            System.IO.File.WriteAllBytes(imagePath, imageBytes);

            if (!ModelState.IsValid || NewFace == null)
                return Page();

            _service.AddFaceUsers(NewFace);
            
            TempData["Message"] = "Utente creato con successo e immagine salvata.";

            return RedirectToAction("Get");
        }

        public void OnGetFoto(byte[] msg)
        {
            if (msg != null)
            {
                string base64Image = Convert.ToBase64String(msg);
                HttpContext.Session.SetString(SessionKeyImageBase64, base64Image);
            }
        }

        public static string GenerateRandomString(int length)
        {
            Random random = new Random();
            return new string(Enumerable.Repeat(alphanumericCharacters, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
