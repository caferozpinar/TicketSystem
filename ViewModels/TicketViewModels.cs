/*
 * ============================================================
 *  TICKET VIEW MODELS — Veri Taşıma Nesneleri
 * ============================================================
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTALAR:
 *  ✔ "ViewBag, ViewData veya tercihen ViewModel kullanımı (Veri taşıma için)"
 *  ✔ "Data Annotations ile form kontrolü"
 *
 *  ViewModel neden kullanılır?
 *  Model (Ticket) tüm veritabanı alanlarını içerir — bazıları form'da
 *  gösterilmemeli ya da kullanıcı tarafından doldurulmamalıdır
 *  (örn: CustomerId, CreatedAt, Priority otomatik atanır).
 *  ViewModel sadece o ekranın ihtiyaç duyduğu alanları taşır.
 *
 *  Ayrıca her ViewModel'e ait Data Annotation'lar o form için
 *  özelleştirilmiş validasyon kuralları içerir.
 * ============================================================
 */

using System.ComponentModel.DataAnnotations;
using TicketSystem.Models;

namespace TicketSystem.ViewModels
{
    // ================================================================
    //  TICKET CREATE VIEW MODEL
    //  Kullanım: Müşteri yeni talep oluştururken doldurduğu form
    // ================================================================
    // Sadece kullanıcının girmesi gereken alanlar burada — CustomerId,
    // CreatedAt, Status, Priority gibi alanlar controller'da otomatik set edilir.
    public class TicketCreateViewModel
    {
        // [Required] → Boş bırakılamaz (hem sunucu hem client tarafında kontrol edilir)
        // [StringLength] → Min/max karakter sınırı
        [Required(ErrorMessage = "Başlık zorunludur.")]
        [StringLength(100, MinimumLength = 5, ErrorMessage = "Başlık 5-100 karakter arasında olmalıdır.")]
        [Display(Name = "Başlık")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Açıklama zorunludur.")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Açıklama 10-2000 karakter arasında olmalıdır.")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        // Müşteri kategori seçer — bu değer CalculatePriority()'ye girdi olarak verilir
        [Display(Name = "Kategori")]
        public TicketCategory Category { get; set; } = TicketCategory.GenelSoru;

        // Priority bu ViewModel'de YOK — controller otomatik hesaplayıp atar
        // Böylece kullanıcı önceliği manipüle edemez
    }

    // ================================================================
    //  TICKET DETAIL VIEW MODEL
    //  Kullanım: Talep detay sayfası — hem bilgi göster hem yanıt formu
    // ================================================================
    // Tek bir view'da iki farklı iş yapılır:
    //   1) Var olan talebi ve yanıtlarını göster (Ticket nesnesi)
    //   2) Yeni yanıt ekleme formu (NewResponseMessage alanı)
    // ViewModel bu iki veriyi tek pakette view'a taşır.
    public class TicketDetailViewModel
    {
        // Veritabanından çekilen tam talep nesnesi (Customer, Responses dahil)
        public Ticket Ticket { get; set; } = null!;

        // Yeni yanıt yazma formu için — sadece bu alan POST edilir
        [Required(ErrorMessage = "Yanıt boş olamaz.")]
        [StringLength(2000, MinimumLength = 2)]
        [Display(Name = "Yanıtınız")]
        public string NewResponseMessage { get; set; } = string.Empty;

        // Destek ekibine gösterilecek durum dropdown'ı için
        public TicketStatus? NewStatus { get; set; }

        // View'da koşullu içerik için — destek mi müşteri mi?
        // Bu bilgi view'a Model.IsStaff olarak geçer
        public bool IsStaff { get; set; }
    }

    // ================================================================
    //  TICKET LIST VIEW MODEL
    //  Kullanım: Talep listeleme sayfası — filtreli liste + istatistikler
    // ================================================================
    // ViewBag/ViewData yerine ViewModel tercih edildi çünkü:
    //   • Tip güvenliği sağlar (ViewBag dynamic tipte, hatalar runtime'da fark edilir)
    //   • IntelliSense desteği ile geliştirme kolaylaşır
    //   • Test edilebilir — ViewModel nesnesi doğrudan test edilebilir
    public class TicketListViewModel
    {
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();

        // Aktif filtre bilgisi — view'da hangi butonun seçili göründüğünü belirler
        public TicketStatus? StatusFilter { get; set; }

        // "Benim Taleplerim" sekmesi aktif mi? — destek ekibine özel filtre
        public bool MyTicketsFilter { get; set; }

        // Giriş yapan destek görevlisinin üstlendiği talep sayısı (sekme rozeti)
        public int MyAssignedCount { get; set; }

        // PROJE GEREKSİNİMİ: "LINQ ile filtreleme"
        // Bu sayılar controller'da LINQ CountAsync() ile hesaplanır:
        //   OpenCount = await query.CountAsync(t => t.Status == TicketStatus.Acik)
        public int TotalCount { get; set; }
        public int OpenCount { get; set; }
        public int InProgressCount { get; set; }
        public int ResolvedCount { get; set; }
    }
}
