using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceRecognition.Services;
using FaceRecognition.Models;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net.WebSockets;

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

        /*public IActionResult OnPostFoto()
        {

            using var httpClient = new HttpClient();

            var response = httpClient.PostAsync("http://127.0.0.1:5000/api/stream", null).Result;

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
        }*/

        public async Task<IActionResult> OnPostFotoAsync()
        {
            var webSocket = new ClientWebSocket();
            var serverUri = new Uri("ws://localhost:8765/");

            await webSocket.ConnectAsync(serverUri, CancellationToken.None);
            
            var receiveTask = Task.Run(async ()=>
            {
                var buffer = new byte[1024 * 4];

                while (true)
                {
                    var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if(result.MessageType == WebSocketMessageType.Close)
                    {
                        continue;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                    if (message.Length == 10)
                    {
                        FaceUser user = _service.GetFaceUser(message);
                        if(user != null)
                        {
                            TempData["Result"] = "Benvenuto " + user.Nome;
                            TempData["ShowMessageLink"] = true; 
                        }
                        else
                        {
                            TempData["Result"] = "Utente non trovato, si prega di riprovare";
                        }            
                    }
                    else
                    {
                        TempData["Result"] = "Impossibile riconoscere il Volto, cambia immagine e riprova";
                    }
                 
                }
        
            });

            await receiveTask;

            return RedirectToPage("/Verify");
        }

    }
}