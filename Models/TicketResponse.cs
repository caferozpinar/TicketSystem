using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSystem.Models
{
    // Bir talebe yazılan yanıt/yorum modelini temsil eder
    public class TicketResponse
    {
        public int Id { get; set; }

        // Yanıt metni
        [Required(ErrorMessage = "Yanıt boş olamaz.")]
        [StringLength(2000, MinimumLength = 2, ErrorMessage = "Yanıt 2-2000 karakter arasında olmalıdır.")]
        [Display(Name = "Yanıt")]
        public string Message { get; set; } = string.Empty;

        // Yanıtın yazıldığı tarih
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Destek ekibinden mi müşteriden mi geldi? (görsel ayırt etmek için)
        public bool IsStaffReply { get; set; } = false;

        // ---- Foreign Key İlişkileri ----

        // Bu yanıt hangi talebe ait? (Foreign Key)
        public int TicketId { get; set; }

        [ForeignKey("TicketId")]
        public Ticket? Ticket { get; set; }

        // Bu yanıtı kim yazdı? (Foreign Key)
        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
