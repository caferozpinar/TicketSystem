/*
 * ============================================================
 *  APPLICATION DB CONTEXT — EF Core Code-First Merkezi
 * ============================================================
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTA:
 *  ✔ "EF Core Code-First yaklaşımı ile DbContext ve Migrations kullanımı"
 *
 *  Code-First yaklaşımı nedir?
 *  Önce C# sınıfları (Model) yazılır, EF Core bunlardan SQL tabloları üretir.
 *  Veritabanı şeması kod tarafından yönetilir → "dotnet ef migrations add" komutu
 *  ile her model değişikliği migration dosyasına dönüştürülür.
 *
 *  Bu sınıf iki şeyi miras alır:
 *  • IdentityDbContext<ApplicationUser> → AspNetUsers, AspNetRoles, AspNetUserRoles
 *    gibi Identity tablolarını otomatik oluşturur
 *  • DbContext → EF Core'un temel sınıfı, LINQ sorgularını SQL'e çevirir
 * ============================================================
 */

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Models;

namespace TicketSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ================================================================
        //  DBSET'LER — Her DbSet bir veritabanı tablosuna karşılık gelir
        // ================================================================
        // EF Core bu property'leri okuyarak migration'larda tablo oluşturur.
        // LINQ sorgularını bu property'ler üzerinden yazarız:
        //   _context.Tickets.Where(...).ToListAsync()

        public DbSet<Ticket> Tickets { get; set; }

        public DbSet<TicketResponse> TicketResponses { get; set; }

        // ================================================================
        //  MODEL YAPILANDIRMASI — Tablo ilişkileri ve davranışlar
        // ================================================================
        // Data Annotation ile belirtilemeyen karmaşık ilişkiler burada tanımlanır.
        // EF Core bu yapılandırmayı okuyarak migration'da doğru FOREIGN KEY ve
        // ON DELETE kurallarını oluşturur.
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); // Identity tablolarının konfigürasyonunu uygula

            // ---- Ticket → Customer (Talebi açan müşteri) ----
            // HasOne/WithMany → Bir müşteri birden çok talep açabilir (1-N ilişki)
            // DeleteBehavior.Restrict → Müşteri silinse bile talepler korunur,
            //   önce talepler silinmeden müşteri silinemez (veri bütünlüğü)
            builder.Entity<Ticket>()
                .HasOne(t => t.Customer)
                .WithMany(u => u.CreatedTickets)
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Ticket → AssignedTo (Talebi üstlenen destek görevlisi) ----
            // Bu ilişki nullable → talep henüz kimseye atanmamış olabilir
            builder.Entity<Ticket>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTickets)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- TicketResponse → Ticket (Yanıt hangi talebe ait?) ----
            // DeleteBehavior.Cascade → Talep silinince tüm yanıtları da sil
            //   (orphan kayıt kalmasın)
            builder.Entity<TicketResponse>()
                .HasOne(r => r.Ticket)
                .WithMany(t => t.Responses)
                .HasForeignKey(r => r.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // ---- TicketResponse → User (Yanıtı yazan kullanıcı) ----
            builder.Entity<TicketResponse>()
                .HasOne(r => r.User)
                .WithMany(u => u.Responses)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
