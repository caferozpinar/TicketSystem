/*
 * ============================================================
 *  PROGRAM.CS — Uygulamanın giriş noktası
 * ============================================================
 *  Bu dosya iki görevi yerine getirir:
 *  1) Servisleri (DI Container) yapılandırır
 *  2) HTTP istek akışını (Middleware Pipeline) belirler
 *
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTALAR:
 *  ✔ Authentication & Authorization  → AddIdentity + UseAuthentication/UseAuthorization
 *  ✔ EF Core Code-First              → AddDbContext + MigrateAsync (migration otomatik uygulanır)
 *  ✔ Role-based yetkilendirme        → DbSeeder ile Admin/Support/Customer rolleri oluşturulur
 * ============================================================
 */

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Models;

var builder = WebApplication.CreateBuilder(args);

// ================================================================
//  1. VERİTABANI BAĞLANTISI — EF Core Code-First yaklaşımı
// ================================================================
// appsettings.json'dan bağlantı dizesini okur: "Data Source=ticketsystem.db"
// SQLite tercih edildi → kurulum gerektirmez, tek dosyada çalışır
// Production'da sadece bu satır değiştirilerek SQL Server'a geçilebilir:
//   options.UseSqlServer(connectionString)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ================================================================
//  2. ASP.NET CORE IDENTITY — Authentication & Authorization katmanı
// ================================================================
// PROJE GEREKSİNİMİ: "Authentication & Authorization: ASP.NET Core Identity kullanımı"
//
// Identity ne sağlar?
//   • Kullanıcı kayıt/giriş/çıkış
//   • Şifreleri otomatik olarak BCRYPT ile hashler → veritabanında hiç düz metin şifre olmaz
//   • Cookie tabanlı oturum yönetimi
//   • Role-based yetkilendirme altyapısı
//
// ApplicationUser → Identity'nin IdentityUser sınıfını genişlettiğimiz özel kullanıcı sınıfı
// IdentityRole    → Rol tablosunu yöneten varsayılan sınıf
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Şifre kuralları — Data Annotation gibi çalışır ama şifre alanına özgü
    options.Password.RequireDigit = true;           // En az 1 rakam
    options.Password.RequireLowercase = true;       // En az 1 küçük harf
    options.Password.RequireUppercase = true;       // En az 1 büyük harf
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;            // Minimum 6 karakter

    // Aynı e-posta ile iki hesap açılamaz
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()  // Identity tablolarını EF Core ile yönet
.AddDefaultTokenProviders();                       // Şifre sıfırlama token'ları için

// ================================================================
//  3. COOKIE / OTURUM AYARLARI
// ================================================================
// Giriş yapılmadan korunan sayfaya erişilince ne olacağını belirler
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";        // Giriş yapılmamışsa buraya yönlendir
    options.AccessDeniedPath = "/Account/Login"; // Yetkisiz erişimde (403) buraya yönlendir
    options.ExpireTimeSpan = TimeSpan.FromHours(8); // Oturum 8 saat geçerli
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ================================================================
//  4. MIDDLEWARE PIPELINE — İsteklerin işlenme sırası
// ================================================================
// Her HTTP isteği bu sıradaki katmanlardan geçer:
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles(); // wwwroot'tan CSS/JS/resim dosyaları sun
app.UseRouting();     // URL'yi controller/action'a eşle

// ÖNEMLİ: Bu iki middleware'in sırası kritiktir!
// UseAuthentication → "Bu kullanıcı kim?" (cookie'den kimliği okur)
// UseAuthorization  → "Bu kullanıcı bunu yapabilir mi?" ([Authorize] attribute'larını kontrol eder)
// Sıra ters olursa [Authorize] her zaman anonim kullanıcı görür → yetkilendirme çalışmaz
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ================================================================
//  5. VERİTABANI KURULUMU — Uygulama ilk açılışında otomatik çalışır
// ================================================================
// PROJE GEREKSİNİMİ: "EF Core Code-First yaklaşımı ile DbContext ve Migrations kullanımı"
//
// MigrateAsync() → Migrations klasöründeki migration'ları veritabanına uygular
//   • İlk çalıştırmada tüm tabloları oluşturur
//   • Sonraki çalıştırmalarda sadece uygulanmamış migration'ları çalıştırır
//   • "dotnet ef migrations add" komutu ile yeni migration eklenir
//
// DbSeeder → Tablolar oluşturulduktan sonra varsayılan verileri ekler
//   • Admin / Support / Customer rollerini oluşturur
//   • Her rol için birer test kullanıcısı ekler
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await context.Database.MigrateAsync();
    await DbSeeder.SeedAsync(userManager, roleManager);
}

app.Run();
