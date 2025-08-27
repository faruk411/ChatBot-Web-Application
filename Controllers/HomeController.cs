using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using chatbot.Models;
using Newtonsoft.Json;
using System.Data.Entity;
using System.Web.Security;
using System.Web;
using System.IO;
using chatbot.Helpers;

namespace chatbot.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // Herkese açık anasayfa yönlendirmesi
        [AllowAnonymous]
        public ActionResult Index()
        {
     
            return View("Anasayfa");
        }

        // Giriş yapmış kullanıcılar için sohbet sayfası
        [Authorize]
        public ActionResult Chat(int? id)
        {
            var userEmail = User.Identity.Name;
            var currentUser = db.Users.FirstOrDefault(u => u.Email == userEmail);
            if (currentUser == null)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Login", "Account");
            }
            var userId = currentUser.Id;

            ViewBag.ChatSessions = db.ChatSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToList();

            if (id.HasValue)
            {
                var selectedSession = db.ChatSessions.Include(s => s.Messages)
                    .FirstOrDefault(s => s.Id == id.Value && s.UserId == userId);
                if (selectedSession != null)
                {
                    // mesajları viewe göndermeden önce şifreleri çözüyoruz
                    foreach (var message in selectedSession.Messages)
                    {
                       if(message.Question != null)
                        {
                            message.Question = CryptoHelper.Decrypt(message.Question);
                        }
                       if(message.Answer != null)
                        {
                            message.Answer = CryptoHelper.Decrypt(message.Answer);
                        }
                    }

                    ViewBag.SelectedSession = selectedSession;
                    ViewBag.CurrentChatMessages = selectedSession.Messages.OrderBy(m => m.Timestamp).ToList();
                }
            }
            return View("Chat");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendMessage(string question, int? chatSessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }
            if (string.IsNullOrWhiteSpace(question))
            {
                TempData["ErrorMessage"] = "Soru Boş Olamaz";
                return RedirectToAction("Chat", new { id = chatSessionId });
            }

            // apiye göndermek için mevcut sohbetin geçmişini al
            var historyForApi = db.ChatMessages
                .Where(m => m.ChatSessionId ==  chatSessionId)
                .OrderBy(m => m.Timestamp) // doğru sıralama için
                .ToList()
            // Apiye göndermeden önce şifreli verileri çöz
                .Select(h => new {
                             question = CryptoHelper.Decrypt(h.Question),
                             answer = CryptoHelper.Decrypt(h.Answer)
                 })
                .ToList();

            // Yeni mesajı veritabanına ekle
            string answer = await GetTextApiResponse(question, historyForApi);
            var session = GetOrCreateChatSession(chatSessionId, userId, question);

            // Şifrelenmiş mesajı oluştur
            var message = new ChatMessage
            {
                Question = CryptoHelper.Encrypt(question),
                Answer = CryptoHelper.Encrypt(answer),
                Timestamp = DateTime.Now,
                ChatSessionId = session.Id
            };
            db.ChatMessages.Add(message);
            await db.SaveChangesAsync();
            return RedirectToAction("Chat", new { id = session.Id });

        }

        // görsel için yeni metot
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendImage(HttpPostedFileBase imageFile, string question, int? chatSessionId)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }

            if(imageFile == null || imageFile.ContentLength == 0)
            {
                TempData["ErrorMessage"] = "Görsel seçilmedi.";
                return RedirectToAction("Chat", new { id = chatSessionId });
            }

            byte[] imageBytes;
            using(var memoryStream = new MemoryStream())
            {
                await imageFile.InputStream.CopyToAsync(memoryStream);
                imageBytes = memoryStream.ToArray();
            }
            // api ye byte dizisini gönder
            string answer = await GetImageApiResponse(imageBytes, imageFile.FileName);
            var session  = GetOrCreateChatSession(chatSessionId, userId, "Görsel analizi: " + Path.GetFileName(imageFile.FileName));
             // aynnı byte dizisini base64 e çevir
            string imageBase64 = Convert.ToBase64String(imageBytes);

            // veriyi şifreleme işlemi
            var message = new ChatMessage
            {
                Question = CryptoHelper.Encrypt(question), // opsiyonel soru
                ImageBase64 = imageBase64, // base64 görsel
                Answer = CryptoHelper.Encrypt(answer),
                Timestamp = DateTime.Now,
                ChatSessionId = session.Id
            };
            db.ChatMessages.Add(message);
            await db.SaveChangesAsync();
            return RedirectToAction("Chat", new { id = session.Id });

            //string answer = await GetImageApiResponse(imageFile);
            //var session = GetOrCreateChatSession(chatSessionId, userId, "Görsel analizi: " + Path.GetFileName(imageFile.FileName));
            //// görseli base64 e çevir
            //string imageBase64;
            //using (var memoryStream = new MemoryStream())
            //{
            //    imageFile.InputStream.CopyTo(memoryStream);
            //    byte[] imageBytes = memoryStream.ToArray();
            //    imageBase64 = Convert.ToBase64String(imageBytes);
            //}
            //var message = new ChatMessage
            //{
            //    Question = question, // opsiyonel soru
            //    ImageBase64 = imageBase64, // base64 görsel
            //    Answer = answer,
            //    Timestamp = DateTime.Now,
            //    ChatSessionId = session.Id

            //};
            //db.ChatMessages.Add(message);
            //await db.SaveChangesAsync();
            //return RedirectToAction("Chat", new { id = session.Id });

        }


        [HttpPost]
        [Authorize]
        public ActionResult CreateNewChat()
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }
            var newSession = new ChatSession
            {
                UserId = userId,
                Title = "Yeni Sohbet",
                CreatedAt = DateTime.Now
            };
            db.ChatSessions.Add(newSession);
            db.SaveChanges();
            return RedirectToAction("Chat", new { id = newSession.Id });
        }

        [HttpPost]
        [Authorize]
        public JsonResult RenameChat(int id, string newTitle)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı." });
            }   
            if(string.IsNullOrWhiteSpace(newTitle))
            {
                return Json(new { success = false, message = "Yeni başlık boş olamaz." });
            }

            var session = db.ChatSessions.FirstOrDefault(s => s.Id == id && s.UserId == userId);
            if (session != null)
            {
               session.Title = newTitle;
                db.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Sohbet bulunamadı." });
        }

        [HttpPost]
        [Authorize]
        public ActionResult DeleteChat(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == -1)
            {
                return RedirectToAction("Login", "Account");
            }
           
            var session = db.ChatSessions.Include(s => s.Messages)
                .FirstOrDefault(s => s.Id == id && s.UserId == userId);

            if (session != null)
            {
                db.ChatMessages.RemoveRange(session.Messages);
                db.ChatSessions.Remove(session);
                db.SaveChanges();
            }
            return RedirectToAction("Chat");
        }

        // Bu metotlar herkese açık
        [AllowAnonymous]
        public ActionResult About() => View();
        [AllowAnonymous]
        public ActionResult Contact() => View();

        private async Task<string> GetTextApiResponse(string question, object history)
        {
            string apiUrl = "http://127.0.0.1:5000/api/chat";
            using (var client = new HttpClient())
            {
                var payload = new { question, history };
                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                try
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(result);
                        return apiResponse?.Answer ?? "API'den yanıt alınamadı.";
                    }
                    return "API Hatası: " + response.StatusCode;
                }
                catch (Exception ex)
                {
                    return "API Bağlantı Hatası: " + ex.Message;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        private int GetCurrentUserId()
        {
            var userEmail = User.Identity.Name;
            var currentUser = db.Users.FirstOrDefault(u => u.Email == userEmail);
            if (currentUser == null)
            {
                FormsAuthentication.SignOut();
                return -1; // Geçersiz kullanıcı
            }
            return currentUser.Id;
        }
        private ChatSession GetOrCreateChatSession (int? chatSessionId, int userId, string title)
        {
            ChatSession session;
            if(!chatSessionId.HasValue || db.ChatSessions.Find(chatSessionId.Value) == null)
            {
                session = new ChatSession
                {
                    Title = title.Length > 50 ? title.Substring(0, 50) + "..." : title,
                    CreatedAt = DateTime.Now,
                    UserId = userId
                };
                db.ChatSessions.Add(session);
            }
            else
            {
                session = db.ChatSessions.Find(chatSessionId.Value);
                if( session == null || session.UserId != userId)
                {
                    // hata yönetimi, bu durum normelde olmaması gereken bir durum
                    //şimdilik yeni bir session oluşturuyoruz
                    session = new ChatSession
                    {
                        Title = title.Length > 50 ? title.Substring(0, 50) + "..." : title,
                        CreatedAt = DateTime.Now,
                        UserId = userId
                    };
                    db.ChatSessions.Add(session);
                }
            }
            db.SaveChanges();
            return session;
         
        }
        private async Task<string> GetImageApiResponse(byte[] imageData, string fileName)
        {
            string apiUrl = "http://127.0.0.1:5001/api/process-image";
            using (var client = new HttpClient())
            using (var content = new MultipartFormDataContent())
            {
                // parametre olarak doğrudan byte dizsindeki görseli kullan
                content.Add(new ByteArrayContent(imageData), "image", fileName);
                try
                {
                    HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                    if(response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(result);
                        return apiResponse?.Answer ?? "Görselden metin üretilemedi";
                    }
                    return "API Hatası: " + response.StatusCode;
                }
                catch (Exception ex)
                {
                    return "API Bağlantı Hatası: " + ex.Message;
                }
                    
            
            }

        }
    }

    public class ApiResponse
    {
        public string Answer { get; set; }
    }
}