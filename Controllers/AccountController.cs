using chatbot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Security.Cryptography;

namespace chatbot.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Account/Register
        public ActionResult Register() => View();

        //Post: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Register(UserViewModel model)
        {       
            if(ModelState.IsValid)
            {
                if(db.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Bu e-posta adresi zaten kayıtlı.");
                    return View(model);
                }
                // Hashing işlemi için salt oluştur
                byte[] passwordHash, passwordSalt;
                CreatePasswordHash(model.Password, out passwordHash, out passwordSalt);

                // Yeni kullanıcı oluştur
                var user = new User
                {
                    Email = model.Email,
                    UserName = model.UserName,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt
                };

                db.Users.Add(user);
                db.SaveChanges();

                FormsAuthentication.SetAuthCookie(user.Email, false);
                // Kullanıcı kaydı başarılı, yönlendirme yap
                return RedirectToAction("Chat", "Home");

            }

            return View(model);
        }

        // GET: Account/Login
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(UserViewModel model, string returnUrl)
        {

            var user = db.Users.FirstOrDefault(u => u.Email == model.Email);
            if (user != null)
            {
                // veri tabanındaki hash ile kullanıcının girdiği şifre karlışatırılıyor
                if (VerifyPasswordHash(model.Password, user.PasswordHash, user.PasswordSalt))
                {
                    FormsAuthentication.SetAuthCookie(user.Email, false);

                    Session["UserName"] = user.UserName; // Kullanıcı adını oturumda sakla

                    if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/")
                        && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Chat", "Home");
                }
            }
            ViewBag.ErorMessage = "Geçersiz E-posta veya şifre";
            return View(model);
        }

        // GET: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Index", "Home");
        }
        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }
        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using(var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                for (int i = 0; i < computedHash.Length; i++)
                {
                    if (computedHash[i] != passwordHash[i]) return false;
                }
            }
            return true;
        }
    }
    public class  UserViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string UserName { get; set;}
    }
}