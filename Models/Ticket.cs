/*
 * ============================================================
 *  TICKET MODEL — EF Core Code-First Ana Varlık Sınıfı
 * ============================================================
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTALAR:
 *  ✔ "EF Core Code-First yaklaşımı"  → Bu C# sınıfı "Tickets" SQL tablosuna dönüşür
 *  ✔ "Data Annotations ile form kontrolü" → [Required], [StringLength] attribute'ları
 *
 *  Data Annotation'lar çift yönlü çalışır:
 *    • Sunucu tarafında: ModelState.IsValid kontrolü ile
 *    • İstemci tarafında: jquery-validation kütüphanesi attribute'ları okuyarak
 *      sayfayı yenilemeye gerek kalmadan hataları gösterir
 * ============================================================
 */

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
    public class Ticket
    {
        // EF Core bu alanı PRIMARY KEY olarak tanır ve otomatik arttırır (AUTOINCREMENT)
        public int Id { get; set; }

        // ================================================================
        //  DATA ANNOTATIONS — Validasyon kuralları
        // ================================================================
        // PROJE GEREKSİNİMİ: "Data Annotations ile form kontrolü (Required, StringLength vb.)"
        //
        // [Required]     → Boş bırakılamaz, null geçilemez
        // [StringLength] → Maksimum ve minimum karakter sayısı
        // [Display]      → View'da label metnini belirler (asp-for ile)
        // Hata mesajları ErrorMessage parametresiyle özelleştirilir

        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Başlık 5-100 karakter arasında olmalıdır.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Açıklama 10-2000 karakter arasında olmalıdır.")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        // ================================================================
        //  ENUM ALANLARI — Sabit değer kümeleriyle temsil edilir
        // ================================================================
        // PROJE GEREKSİNİMİ: "Enum tiplerinin kullanımı"
        //
        // EF Core bu alanları veritabanında int olarak saklar (Acik=0, Incelemede=1...)
        // Ama kod içinde okunabilir isimle kullanılır:
        //   ticket.Status == TicketStatus.Acik
        // → SQL: WHERE Status = 0

        [Display(Name = "Durum")]
        public TicketStatus Status { get; set; } = TicketStatus.Acik;

        // Öncelik kullanıcı tarafından seçilmez — CalculatePriority() metodu atar
        [Display(Name = "Öncelik")]
        public TicketPriority Priority { get; set; } = TicketPriority.Orta;

        [Display(Name = "Kategori")]
        public TicketCategory Category { get; set; } = TicketCategory.GenelSoru;

        // Otomatik atanır — controller'da DateTime.Now ile set edilir
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Yanıt eklendiğinde veya durum değiştiğinde güncellenir
        [Display(Name = "Son Güncelleme")]
        public DateTime? UpdatedAt { get; set; }

        // ================================================================
        //  FOREIGN KEY İLİŞKİLERİ — Tablolar arası bağlantı
        // ================================================================
        // EF Core bu [ForeignKey] attribute'larını okuyarak migration'da
        // FOREIGN KEY constraint'leri oluşturur.
        //
        // Navigation property (Customer, AssignedTo) ile LINQ'te JOIN yazmadan
        // ilişkili veriye erişilir:
        //   ticket.Customer.FullName  → müşterinin adı
        //   ticket.AssignedTo?.FullName → atanan görevlinin adı (null olabilir)

        [Required]
        public string CustomerId { get; set; } = string.Empty;     // FK → AspNetUsers.Id

        [ForeignKey("CustomerId")]
        public ApplicationUser? Customer { get; set; }             // Navigation property

        public string? AssignedToId { get; set; }                  // FK → AspNetUsers.Id (nullable)

        [ForeignKey("AssignedToId")]
        public ApplicationUser? AssignedTo { get; set; }           // Navigation property

        // Bu talebin yanıt koleksiyonu — Include() ile yüklenir
        public ICollection<TicketResponse> Responses { get; set; } = new List<TicketResponse>();
    }
}
