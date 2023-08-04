using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Diagnostics;
using Python.Runtime;

namespace FaceRecognition.Pages
{
    public class VerifyModel : PageModel
    {
        private readonly ILogger<VerifyModel> _logger;
        private readonly HttpClient _httpClient = new HttpClient();

        public VerifyModel(ILogger<VerifyModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
            // This method will handle GET requests (when the page is loaded)
        }
        public IActionResult OnPost()
        {
            var pythonExecutable = "python"; // Assumi che python sia nel percorso
            var processStartInfo = new ProcessStartInfo
            {
                FileName = pythonExecutable,
                Arguments = "C:/Users/Utente/Desktop/.NET/Volantini/trunk/FaceRecognition/Volantini/analisi.py",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processStartInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    // Gestisci eventuali errori nel processo Python
                    // Puoi utilizzare il contenuto della variabile 'error'
                    throw new Exception(error);
                }

                TempData["Result"] = output;
            }
            return RedirectToAction("Verify");
            /* Invia una richiesta POST all'endpoint Python che eseguirà la funzione di analisi
            var pythonEndpointUrl = "http://localhost:7040/analisi"; // Inserisci qui l'URL del tuo endpoint Python
            var response = _httpClient.PostAsync(pythonEndpointUrl, null).Result;

            // Verifica lo stato della risposta e gestisci eventuali errori
            if (response.IsSuccessStatusCode)
            {
                var resultJson = await response.Content.ReadAsStringAsync();
                var resultObject = Newtonsoft.Json.JsonConvert.DeserializeObject<ResultModel>(resultJson);
                var result = resultObject.Result;

                if (result)
                {
                    return RedirectToAction("Verify");
                }
                else
                {
                    return BadRequest("Analisi non rilevata");
                }
            }
            else
            {
                // Gestisci il fallimento della richiesta
                return BadRequest();
            }*/
        }
        
        /*private class ResultModel
        {
            public bool Result { get; set; }
        }*/
    }
}