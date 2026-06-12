/*
 * ============================================================
 *  TICKET CONTROLLER — Talep Yönetiminin Ana Merkezi
 * ============================================================
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTALAR:
 *  ✔ "Role-based Authorization" → [Authorize(Roles = "...")] attribute'ları
 *  ✔ "LINQ ile filtreleme"      → Where, CountAsync, OrderByDescending
 *  ✔ "Enum kullanımı"           → TicketStatus, TicketPriority, TicketCategory
 *  ✔ "ViewModel kullanımı"      → TicketCreateViewModel, TicketDetailViewModel
 *  ✔ Güvenlik: CSRF             → [ValidateAntiForgeryToken]
 *  ✔ Güvenlik: Yetki kontrolü  → [Authorize] hem class hem method seviyesinde
 *  ✔ İş kuralı: Müşteri kısıtı → Destek yanıtlamadan müşteri yazamaz (çift katman)
 * ============================================================
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Models;
using TicketSystem.ViewModels;

namespace TicketSystem.Controllers
{
    // ================================================================
    //  SINIF SEVİYESİNDE YETKİLENDİRME
    // ================================================================
    // [Authorize] — Bu controller'daki TÜM action'lara giriş yapmadan
    // erişilemez. Giriş yapmamış biri herhangi bir URL'e giderse
    // Program.cs'deki LoginPath'e yönlendirilir (/Account/Login).
    //
    // Sadece view gizlemek YETERSİZDİR. Örneğin "Yeni Talep" butonu
    // sadece Customer'a görünür ama URL bilinerek erişilebilir.
    // [Authorize] sunucu tarafında bunu engeller.
    [Authorize]
    public class TicketController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ================================================================
        //  GET: /Ticket — Talep Listesi
        // ================================================================
        // PROJE GEREKSİNİMİ: "LINQ ile filtreleme (Sadece açık talepleri getir vb.)"
        //
        // Rol bazlı veri erişimi:
        //   • Müşteri → sadece kendi açtığı talepler
        //   • Destek/Admin → tüm talepler
        // Bu ayrım LINQ Where() ile yapılır, iki ayrı sayfa değil tek action.
        // myTickets → true ise sadece giriş yapan destek görevlisinin üstlendiği talepler
        public async Task<IActionResult> Index(TicketStatus? statusFilter, bool myTickets = false)
        {
            var userId = _userManager.GetUserId(User);

            // User.IsInRole() → Identity'nin AspNetUserRoles tablosunu sorgular
            bool isStaff = User.IsInRole("Support") || User.IsInRole("Admin");

            // IQueryable → Sorgu henüz veritabanına gitmedi, sadece TANIMI oluşturuldu
            // Include() → Eager Loading: ilişkili tabloları JOIN ile tek sorguda getirir
            IQueryable<Ticket> query = _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.AssignedTo);

            if (!isStaff)
            {
                // Müşteri sadece kendi taleplerini görür
                query = query.Where(t => t.CustomerId == userId);
            }
            else if (myTickets)
            {
                // ================================================================
                //  "BENİM TALEPLERİM" SEKMESİ — Sadece destek ekibi kullanır
                // ================================================================
                // Claim ettiği (üstlendiği) talepleri listeler.
                // LINQ Where → AssignedToId = giriş yapan kullanıcı
                // SQL: WHERE AssignedToId = '{userId}'
                query = query.Where(t => t.AssignedToId == userId);
            }
            else
            {
                // ================================================================
                //  "TÜM TALEPLER" LİSTESİ — Sadece sahipsiz talepler gösterilir
                // ================================================================
                // Zaten birine atanmış (AssignedToId != null) talepler bu listeden
                // gizlenir. O talepler sadece ilgili kişinin "Benim Taleplerim"
                // sekmesinde görünür.
                //
                // Amaç: Karışıklığı önlemek. Üstlenilmiş talebi başka bir destek
                // görevlisi görüp tekrar üstlenmeye çalışmasın.
                //
                // SQL: WHERE AssignedToId IS NULL
                query = query.Where(t => t.AssignedToId == null);
            }

            // Durum filtresi — URL parametresi: /Ticket?statusFilter=0
            if (statusFilter.HasValue)
            {
                query = query.Where(t => t.Status == statusFilter.Value);
            }

            IQueryable<Ticket> baseQuery = isStaff
                ? _context.Tickets
                : _context.Tickets.Where(t => t.CustomerId == userId);

            var viewModel = new TicketListViewModel
            {
                Tickets         = await query.OrderByDescending(t => t.CreatedAt).ToListAsync(),
                StatusFilter    = statusFilter,
                MyTicketsFilter = myTickets,
                // Sahiplenilen talep sayısı — sekme rozeti için
                MyAssignedCount = isStaff
                    ? await _context.Tickets.CountAsync(t => t.AssignedToId == userId)
                    : 0,
                TotalCount      = await baseQuery.CountAsync(),
                OpenCount       = await baseQuery.CountAsync(t => t.Status == TicketStatus.Acik),
                InProgressCount = await baseQuery.CountAsync(t => t.Status == TicketStatus.Incelemede),
                ResolvedCount   = await baseQuery.CountAsync(t => t.Status == TicketStatus.Cozuldu)
            };

            return View(viewModel);
        }

        // GET: /Ticket/Create — Yeni talep formu
        // Sadece giriş yapmış kullanıcılar erişebilir (class'taki [Authorize])
        public IActionResult Create()
        {
            return View(new TicketCreateViewModel());
        }

        // POST: /Ticket/Create — Formu kaydet
        [HttpPost]
        [ValidateAntiForgeryToken] // CSRF koruması — token geçersizse istek reddedilir
        public async Task<IActionResult> Create(TicketCreateViewModel model)
        {
            // ModelState.IsValid → ViewModel'deki tüm [Required], [StringLength] kurallarını
            // sunucu tarafında kontrol eder. Client-side validation bypass edilse bile çalışır.
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);

            // Otomatik öncelik hesaplama — kullanıcı öncelik seçemiyor
            // Bu sayede müşteri kendi talebini "Kritik" olarak işaretleyemez
            var priority = CalculatePriority(model.Title, model.Description, model.Category);

            // ViewModel → Model dönüşümü
            // Sadece güvenilir alanlar kopyalanır. CustomerId ve CreatedAt
            // sunucu tarafından atanır, formdan gelmez (manipülasyon önlenir)
            var ticket = new Ticket
            {
                Title       = model.Title,
                Description = model.Description,
                Category    = model.Category,
                Priority    = priority,
                Status      = TicketStatus.Acik, // Yeni talep her zaman "Açık" başlar
                CustomerId  = userId!,
                CreatedAt   = DateTime.Now
            };

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync(); // INSERT SQL çalışır

            TempData["Success"] = "Talebiniz başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Ticket/Detail/5 — Talep detay sayfası
        public async Task<IActionResult> Detail(int id)
        {
            var userId = _userManager.GetUserId(User);
            bool isStaff = User.IsInRole("Support") || User.IsInRole("Admin");

            // ThenInclude → İç içe navigation property yükleme
            // Tickets → Responses → User zinciri tek sorguda gelir
            var ticket = await _context.Tickets
                .Include(t => t.Customer)
                .Include(t => t.AssignedTo)
                .Include(t => t.Responses)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            // Yetki kontrolü: Müşteri başkasının talebini görememeli
            // Forbid() → 403 HTTP yanıtı döner
            if (!isStaff && ticket.CustomerId != userId)
                return Forbid();

            var viewModel = new TicketDetailViewModel
            {
                Ticket  = ticket,
                IsStaff = isStaff
            };

            return View(viewModel);
        }

        // POST: /Ticket/AddResponse — Talebe yanıt ekle
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddResponse(int ticketId, string message)
        {
            var userId = _userManager.GetUserId(User);
            bool isStaff = User.IsInRole("Support") || User.IsInRole("Admin");

            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Yanıt boş olamaz.";
                return RedirectToAction(nameof(Detail), new { id = ticketId });
            }

            var ticket = await _context.Tickets
                .Include(t => t.Responses)
                .FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null) return NotFound();

            if (!isStaff && ticket.CustomerId != userId)
                return Forbid();

            // ================================================================
            //  SAHİPLENME ZORUNLULUĞU — Destek ekibi için (Yanıt)
            // ================================================================
            // Destek görevlisi talebi Claim etmemişse yanıt yazamaz.
            // Bu kural view'da form disabled ile gösterilir (Katman 1),
            // ama burada sunucu tarafında da doğrulanır (Katman 2).
            // AssignedToId != userId → bu talep bana ait değil → engelle
            if (isStaff && ticket.AssignedToId != userId)
            {
                TempData["Error"] = "Bu taleple işlem yapabilmek için önce 'Talebi Üstlen' butonuna tıklayın.";
                return RedirectToAction(nameof(Detail), new { id = ticketId });
            }

            // ================================================================
            //  MÜŞTERI YANIT KISITI — Çift Katmanlı Güvenlik
            // ================================================================
            // Katman 1 (View / Detail.cshtml): Destek yanıtı yoksa form disabled
            //   gösterilir, gönder butonu pasiftir.
            //
            // Katman 2 (Bu kontrol): View bypass edilip direkt POST atılsa bile
            //   sunucu tarafında engellenir. Sadece view gizlemek güvenli değildir —
            //   Postman veya benzeri araçlarla form olmadan POST gönderilebilir.
            //   Bu kontrol o tür saldırıları da önler.
            if (!isStaff && !ticket.Responses.Any(r => r.IsStaffReply))
            {
                TempData["Error"] = "Destek ekibi henüz yanıtlamamış. Lütfen bekleyin.";
                return RedirectToAction(nameof(Detail), new { id = ticketId });
            }

            var response = new TicketResponse
            {
                TicketId    = ticketId,
                Message     = message,
                UserId      = userId!,
                IsStaffReply = isStaff, // View'da farklı renkte göstermek için
                CreatedAt   = DateTime.Now
            };

            _context.TicketResponses.Add(response);
            ticket.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Yanıtınız eklendi.";
            return RedirectToAction(nameof(Detail), new { id = ticketId });
        }

        // POST: /Ticket/UpdateStatus — Durum güncelle
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ================================================================
        //  METOD SEVİYESİNDE ROL KONTROLÜ
        // ================================================================
        // PROJE GEREKSİNİMİ: "Role-based (Admin/User) yetkilendirme"
        //
        // [Authorize(Roles = "Support,Admin")] → Bu action'a sadece Support veya
        // Admin rolündeki kullanıcılar erişebilir. Customer rolündeki biri
        // URL'den /Ticket/UpdateStatus'e POST atarsa otomatik olarak 403 alır.
        //
        // Bu kontrol saf sunucu tarafındadır — view'da butonun gizli olması
        // yeterli değildir, bu attribute olmasa URL'den erişilebilir.
        [Authorize(Roles = "Support,Admin")]
        public async Task<IActionResult> UpdateStatus(int ticketId, TicketStatus newStatus)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            // ================================================================
            //  SAHİPLENME ZORUNLULUĞU — Destek ekibi için (Durum)
            // ================================================================
            // Yanıt kısıtıyla aynı mantık — üstlenmeden durum da değiştirilemez.
            // View'da dropdown ve buton disabled gösterilir (görsel),
            // bu kontrol sunucu tarafı güvencesidir (gerçek).
            if (ticket.AssignedToId != userId)
            {
                TempData["Error"] = "Bu taleple işlem yapabilmek için önce 'Talebi Üstlen' butonuna tıklayın.";
                return RedirectToAction(nameof(Detail), new { id = ticketId });
            }

            ticket.Status    = newStatus;
            ticket.UpdatedAt = DateTime.Now;

            // Talep "İncelemede"ye alınıyorsa ve henüz atanmamışsa
            // işlemi yapan destek görevlisini ata
            if (newStatus == TicketStatus.Incelemede && ticket.AssignedToId == null)
            {
                ticket.AssignedToId = _userManager.GetUserId(User);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Talep durumu güncellendi.";
            return RedirectToAction(nameof(Detail), new { id = ticketId });
        }

        // ================================================================
        //  TALEP SAHİPLENME MEKANİZMASI — Sadece Destek Ekibi Görür
        // ================================================================
        // POST: /Ticket/Claim — Talebi sahiplen (üstüne al)
        //
        // Yeni migration gerekmez — Ticket modelindeki mevcut AssignedToId
        // alanı kullanılır. Sadece bu alanı current user'a set eden bir action.
        //
        // İş kuralları:
        //   • Zaten başkasına atanmış talep sahiplenilemez (çakışmayı önler)
        //   • Kendi talebini bırakmak için ayrı Release action'ı var
        //   • Müşteriler bu endpoint'e erişemez → [Authorize(Roles = "Support,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Support,Admin")]
        public async Task<IActionResult> Claim(int ticketId)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets
                .Include(t => t.AssignedTo) // Kimin üstünde olduğunu kontrol için
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null) return NotFound();

            // Başkasına atanmışsa sahiplenme — aynı talebi iki kişi üstlenemez
            if (ticket.AssignedToId != null && ticket.AssignedToId != userId)
            {
                TempData["Error"] = $"Bu talep zaten {ticket.AssignedTo?.FullName} tarafından üstlenilmiş.";
                return RedirectToAction(nameof(Detail), new { id = ticketId });
            }

            // Talebi üstlen: AssignedToId = giriş yapan destek görevlisi
            ticket.AssignedToId = userId;
            ticket.UpdatedAt    = DateTime.Now;

            // Talep henüz "Açık" durumdaysa otomatik "İncelemede"ye al
            if (ticket.Status == TicketStatus.Acik)
                ticket.Status = TicketStatus.Incelemede;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Talep üzerinize alındı. Talep listenizdeki 'Benim Taleplerim' sekmesinde görünecek.";
            return RedirectToAction(nameof(Detail), new { id = ticketId });
        }

        // POST: /Ticket/Release — Sahiplenilmiş talebi bırak
        // Başka bir destek görevlisinin devralabilmesi için atamayı sıfırlar
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Support,Admin")]
        public async Task<IActionResult> Release(int ticketId)
        {
            var userId = _userManager.GetUserId(User);
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket == null) return NotFound();

            // Sadece kendi üstlendiği talebi bırakabilir (başkasınınkini bırakamaz)
            if (ticket.AssignedToId != userId)
            {
                TempData["Error"] = "Yalnızca kendinize atanmış talebi bırakabilirsiniz.";
                return RedirectToAction(nameof(Detail), new { id = ticketId });
            }

            ticket.AssignedToId = null;  // Atamayı sıfırla → talep tekrar sahipsiz olur
            ticket.UpdatedAt    = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Talep bırakıldı, artık başka bir destek görevlisi üstlenebilir.";
            return RedirectToAction(nameof(Detail), new { id = ticketId });
        }

        // POST: /Ticket/Delete/5 — Talep sil
        [HttpPost]
        [ValidateAntiForgeryToken]
        // Sadece Admin silebilir — Support ve Customer bu URL'e POST atarsa 403 alır
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            _context.Tickets.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Talep silindi.";
            return RedirectToAction(nameof(Index));
        }

        // ================================================================
        //  OTOMATİK ÖNCELİK HESAPLAMA
        // ================================================================
        // Kullanıcı önceliği kendisi seçemiyor — bu metod kategori ve
        // metin içeriğine bakarak önceliği otomatik belirler.
        //
        // Mantık:
        //   1. Başlık veya açıklamada kritik kelimeler varsa → Kritik
        //   2. Yoksa kategori bazlı varsayılan → TeknikSorun:Yüksek, diğerleri daha düşük
        private static TicketPriority CalculatePriority(string title, string description, TicketCategory category)
        {
            var kritikKelimeler = new[] { "acil", "kritik", "çalışmıyor", "erişemiyorum", "veri kaybı", "ödeme yapılamıyor" };
            var metin = (title + " " + description).ToLower();

            // LINQ Any() → kelimelerden herhangi biri metinde geçiyor mu?
            if (kritikKelimeler.Any(k => metin.Contains(k)))
                return TicketPriority.Kritik;

            // C# switch expression — switch-case'in kısa yazımı
            return category switch
            {
                TicketCategory.TeknikSorun => TicketPriority.Yuksek,
                TicketCategory.HesapSorunu => TicketPriority.Orta,
                TicketCategory.FaturaOdeme => TicketPriority.Orta,
                TicketCategory.GenelSoru   => TicketPriority.Dusuk,
                _                          => TicketPriority.Dusuk
            };
        }
    }
}
