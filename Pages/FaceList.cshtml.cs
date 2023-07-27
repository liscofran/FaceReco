using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognition.Models;
using FaceRecognition.Services;


namespace FaceRecognition.Pages
{
    public class FaceListModel : PageModel
    {
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
            if (!ModelState.IsValid || NewFace == null)
            {
                return Page();
            }

            _service.AddFaceUsers(NewFace);

            return RedirectToAction("Get");
        }

        public IActionResult OnPostDelete(int id)
        {
            _service.DeleteFaceUsers(id);

            return RedirectToAction("Get");
        }
    }
}
