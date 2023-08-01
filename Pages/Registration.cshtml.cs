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
        public byte[]? tmpface;
        public const string SessionKeyName = "_Name";

        private static readonly Random random = new Random();
        private const string alphanumericCharacters ="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        [BindProperty] 
        public FaceUser NewFace { get; set; } = default!;
        private readonly FaceService _service;
        public IList<FaceUser> FaceList { get;set; } = default!;
        public FaceListModel(FaceService service)
        {
            _service = service;
        }

        public void OnGet()
        {
            FaceList = _service.GetFaceUsers();
        }

        public IActionResult OnPost()
        {
            string? sess = HttpContext.Session.GetString(SessionKeyName);

            NewFace.Id = GenerateRandomString(10);

            if(sess != null)
                NewFace.Face = Encoding.ASCII.GetBytes(sess);

            if (!ModelState.IsValid || NewFace == null)
                return Page();
                
            _service.AddFaceUsers(NewFace);
            return RedirectToAction("Get");
        }

        public void OnGetFoto(byte[] msg)
        {
            string bitString = BitConverter.ToString(msg);
            HttpContext.Session.SetString(SessionKeyName, bitString);
        }

        public IActionResult OnPostDelete(int id)
        {
            _service.DeleteFaceUsers(id);
            return RedirectToAction("Get");
        }

        public static string GenerateRandomString(int length)
        {
            return new string(Enumerable.Repeat(alphanumericCharacters, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }
}
