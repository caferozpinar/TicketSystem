using Microsoft.AspNetCore.Identity;

namespace TicketSystem.Models
{
    // Identity'nin varsayılan kullanıcı sınıfını genişletiyoruz
    // Böylece her kullanıcıya ad-soyad ekleyebiliyoruz
    public class ApplicationUser : IdentityUser
    {
        // Kullanıcının tam adı (Required → boş geçilemez)
        public string FullName { get; set; } = string.Empty;

        // Kullanıcının sisteme katıldığı tarih
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Bu kullanıcının açtığı talepler (navigation property)
        public ICollection<Ticket> CreatedTickets { get; set; } = new List<Ticket>();

        // Bu kullanıcıya atanan talepler (Destek ekibi için)
        public ICollection<Ticket> AssignedTickets { get; set; } = new List<Ticket>();

        // Bu kullanıcının verdiği yanıtlar
        public ICollection<TicketResponse> Responses { get; set; } = new List<TicketResponse>();
    }
}
