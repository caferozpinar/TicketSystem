/*
 * ============================================================
 *  ACCOUNT CONTROLLER — Kimlik Doğrulama İşlemleri
 * ============================================================
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTALAR:
 *  ✔ "Authentication & Authorization: ASP.NET Core Identity kullanımı"
 *  ✔ "Kullanıcı girişi ve kayıt işlemleri"
 *  ✔ Güvenlik: CSRF koruması ([ValidateAntiForgeryToken])
 *
 *  GÜVENLİK NOTU — Şifre Güvenliği:
 *  Identity'nin CreateAsync() ve PasswordSignInAsync() metodları şifrelerle
 *  doğrudan çalışmaz. Şifreler BCRYPT algoritmasıyla hashlenerek veritabanına
 *  yazılır. Veritabanı ele geçirilse bile düz metin şifreler görülemez.
 *
 *  GÜVENLİK NOTU — CSRF (Cross-Site Request Forgery):
 *  [ValidateAntiForgeryToken] → Her POST isteğinde formla birlikte gelen
 *  gizli token doğrulanır. Başka bir siteden sahte istek gönderilirse
 *  token eşleşmediği için istek reddedilir.
 * ============================================================
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Models;
using TicketSystem.ViewModels;

namespace TicketSystem.Controllers
{
    public class AccountController : Controller
    {
        // ================================================================
        //  DEPENDENCY INJECTION — Servisler constructor üzerinden alınır
        // ================================================================
        // UserManager  → Kullanıcı oluşturma, rol atama, şifre yönetimi
        // SignInManager → Giriş/çıkış, oturum cookie'si yönetimi
        // Program.cs'de AddIdentity() ile kaydedilen servisler buraya inject edilir
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // Zaten giriş yapılmış olsa bile Login sayfası açılır.
            // Test sırasında hesap değiştirmek için aynı pencerede geçiş yapılabilir.
            // POST sırasında mevcut oturum önce kapatılır (aşağıda).
            return View();
        }

        // POST: /Account/Login — Form submit edildiğinde çalışır
        [HttpPost]
        // [ValidateAntiForgeryToken] → View'daki @Html.AntiForgeryToken() ile üretilen
        // gizli token bu attribute tarafından doğrulanır. Token yoksa veya geçersizse
        // istek 400 Bad Request ile reddedilir. CSRF saldırılarını engeller.
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            // ModelState.IsValid → LoginViewModel'deki Data Annotation kurallarını kontrol eder
            // ([Required], [EmailAddress] vb.) — sunucu tarafı validasyon
            if (!ModelState.IsValid)
                return View(model);

            // Başka biri zaten giriş yapmışsa önce oturumu kapat.
            // Böylece aynı tarayıcı penceresinde hesap değiştirmek mümkün olur.
            if (User.Identity?.IsAuthenticated == true)
                await _signInManager.SignOutAsync();

            // PasswordSignInAsync ne yapar?
            //   1. E-postaya karşılık gelen kullanıcıyı bulur
            //   2. Girilen şifreyi BCRYPT ile hashleyip veritabanındaki hash ile karşılaştırır
            //   3. Eşleşirse HTTP response'a oturum cookie'si ekler
            //   4. Sonraki isteklerde bu cookie okunarak kullanıcı tanınır
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,       // true → cookie kalıcı olur (tarayıcı kapansa bile)
                lockoutOnFailure: false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            // Hatalı giriş — kasıtlı olarak "hangi alan yanlış" denmez (güvenlik)
            ModelState.AddModelError(string.Empty, "E-posta veya şifre hatalı.");
            return View(model);
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                EmailConfirmed = true
            };

            // CreateAsync → şifreyi BCRYPT ile hashler ve AspNetUsers tablosuna yazar
            // Veritabanında "PasswordHash" sütununda uzun bir hash değeri görülür
            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // PROJE GEREKSİNİMİ: "Role-based yetkilendirme"
                // Yeni kayıtlı her kullanıcı otomatik olarak "Customer" rolü alır
                // Admin veya Support rolü sadece DbSeeder veya manuel olarak atanabilir
                await _userManager.AddToRoleAsync(user, "Customer");

                // Kayıt başarılı → oturum aç ve ana sayfaya yönlendir
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            // Identity hataları: zayıf şifre, e-posta zaten kayıtlı vb.
            // Bu hatalar Identity'den gelir, manuel kontrol gerekmez
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // POST: /Account/Logout
        // GET ile çıkış yapmak güvensiz → biri kullanıcıya /Account/Logout linki
        // tıklatabilir. POST zorunlu tutularak bu saldırı engellenir.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // SignOutAsync → oturum cookie'sini geçersiz kılar
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
    }
}
