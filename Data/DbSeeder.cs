/*
 * ============================================================
 *  DB SEEDER — Uygulama İlk Açılışında Varsayılan Veri Oluşturur
 * ============================================================
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTA:
 *  ✔ "Role-based (Admin/User) yetkilendirme — En az iki farklı rol tanımlanmalıdır"
 *
 *  Bu sistemde 3 rol tanımlandı:
 *    • Admin    → Tüm yetkilere sahip, talep silebilir
 *    • Support  → Destek ekibi, talepleri yönetir ve yanıtlar
 *    • Customer → Müşteri, talep açar ve takip eder
 *
 *  Program.cs'de MigrateAsync() sonrasında çağrılır.
 *  Her çalıştırmada "yoksa oluştur" mantığıyla çalışır → duplicate oluşmaz.
 * ============================================================
 */

using Microsoft.AspNetCore.Identity;
using TicketSystem.Models;

namespace TicketSystem.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // ================================================================
            //  1. ROL OLUŞTURMA
            // ================================================================
            // PROJE GEREKSİNİMİ: "En az iki farklı rol veya kullanıcı tipi tanımlanmalıdır"
            //
            // Bu roller veritabanındaki "AspNetRoles" tablosuna yazılır.
            // Controller'lardaki [Authorize(Roles = "...")] attribute'u bu rollere göre çalışır.
            string[] roles = { "Admin", "Support", "Customer" };
            foreach (var role in roles)
            {
                // RoleExistsAsync → rol zaten varsa tekrar oluşturma (idempotent)
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ================================================================
            //  2. ADMIN KULLANICI
            // ================================================================
            // Admin: tüm talepleri görür, silebilir, durum değiştirebilir
            var adminEmail = "admin@ticketsystem.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Sistem Yöneticisi",
                    EmailConfirmed = true
                };
                // CreateAsync → şifreyi otomatik BCRYPT ile hashleyerek veritabanına yazar
                // Veritabanına bakıldığında "Admin@123" değil, hash değeri görülür
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                {
                    // Kullanıcıyı Admin rolüne ata → "AspNetUserRoles" tablosuna yazılır
                    await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            // ================================================================
            //  3. DESTEK GÖREVLİSİ 1
            // ================================================================
            var supportEmail = "destek@ticketsystem.com";
            if (await userManager.FindByEmailAsync(supportEmail) == null)
            {
                var support = new ApplicationUser
                {
                    UserName = supportEmail,
                    Email = supportEmail,
                    FullName = "Ahmet Destek",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(support, "Support@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(support, "Support");
            }

            // ================================================================
            //  4. DESTEK GÖREVLİSİ 2
            // ================================================================
            var support2Email = "destek2@ticketsystem.com";
            if (await userManager.FindByEmailAsync(support2Email) == null)
            {
                var support2 = new ApplicationUser
                {
                    UserName = support2Email,
                    Email = support2Email,
                    FullName = "Mehmet Destek",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(support2, "Support@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(support2, "Support");
            }

            // ================================================================
            //  5. MÜŞTERİ 1
            // ================================================================
            var customer1Email = "musteri1@test.com";
            if (await userManager.FindByEmailAsync(customer1Email) == null)
            {
                var customer1 = new ApplicationUser
                {
                    UserName = customer1Email,
                    Email = customer1Email,
                    FullName = "Ali Müşteri",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(customer1, "Customer@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(customer1, "Customer");
            }

            // ================================================================
            //  6. MÜŞTERİ 2
            // ================================================================
            var customer2Email = "musteri2@test.com";
            if (await userManager.FindByEmailAsync(customer2Email) == null)
            {
                var customer2 = new ApplicationUser
                {
                    UserName = customer2Email,
                    Email = customer2Email,
                    FullName = "Ayşe Müşteri",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(customer2, "Customer@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(customer2, "Customer");
            }
        }
    }
}
