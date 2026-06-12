using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketSystem.Data;
using TicketSystem.Models;

namespace TicketSystem.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: / → Ana sayfa / Dashboard
        public async Task<IActionResult> Index()
        {
            // Giriş yapılmamışsa karşılama sayfası göster
            if (User.Identity?.IsAuthenticated != true)
                return View("Landing");

            var userId = _userManager.GetUserId(User);
            bool isStaff = User.IsInRole("Support") || User.IsInRole("Admin");

            if (isStaff)
            {
                // Destek ekibi için: tüm taleplerin özet istatistikleri
                // LINQ ile her durumdaki talep sayısını say
                ViewBag.TotalTickets = await _context.Tickets.CountAsync();
                ViewBag.OpenTickets = await _context.Tickets
                    .CountAsync(t => t.Status == TicketStatus.Acik);
                ViewBag.InProgressTickets = await _context.Tickets
                    .CountAsync(t => t.Status == TicketStatus.Incelemede);
                ViewBag.ResolvedTickets = await _context.Tickets
                    .CountAsync(t => t.Status == TicketStatus.Cozuldu);

                // Son 5 açık talep — LINQ ile tarihe göre sıralı getir
                ViewBag.RecentTickets = await _context.Tickets
                    .Include(t => t.Customer) // İlgili müşteri bilgisini de yükle (JOIN)
                    .Where(t => t.Status == TicketStatus.Acik)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }
            else
            {
                // Müşteri için: kendi taleplerinin özet istatistikleri
                ViewBag.MyTotalTickets = await _context.Tickets
                    .CountAsync(t => t.CustomerId == userId);
                ViewBag.MyOpenTickets = await _context.Tickets
                    .CountAsync(t => t.CustomerId == userId && t.Status == TicketStatus.Acik);
                ViewBag.MyResolvedTickets = await _context.Tickets
                    .CountAsync(t => t.CustomerId == userId && t.Status == TicketStatus.Cozuldu);

                // Müşterinin son 5 talebi
                ViewBag.MyRecentTickets = await _context.Tickets
                    .Where(t => t.CustomerId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .ToListAsync();
            }

            ViewBag.IsStaff = isStaff;
            return View();
        }
    }
}
