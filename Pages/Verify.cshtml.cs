using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognition.Services;

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
    }
}