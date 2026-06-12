/*
 * ============================================================
 *  ENUMS — Sabit Değer Kümeleri
 * ============================================================
 *  PROJE GEREKSİNİMİ KARŞILANAN NOKTA:
 *  ✔ "Enum tiplerinin kullanımı"
 *
 *  Enum'lar magic number yerine anlamlı isim kullanmayı sağlar.
 *  Veritabanında int olarak saklanır (Acik=0, Incelemede=1 ...)
 *  ama kod içinde okunabilir isimle kullanılır.
 *
 *  Kullanım örnekleri:
 *    • LINQ filtreleme: .Where(t => t.Status == TicketStatus.Acik)
 *    • Controller'da durum güncelleme: ticket.Status = TicketStatus.Cozuldu
 *    • View'da dropdown: Html.GetEnumSelectList<TicketStatus>()
 * ============================================================
 */

namespace TicketSystem.Models
{
    // ================================================================
    //  TALEBİN DURUMU — Destek sürecindeki aşamaları temsil eder
    // ================================================================
    // LINQ sorgularında filtre olarak kullanılır:
    //   _context.Tickets.Where(t => t.Status == TicketStatus.Acik)
    //   → SQL'de: WHERE Status = 0
    public enum TicketStatus
    {
        Acik = 0,       // Müşteri tarafından yeni açıldı, henüz incelenmedi
        Incelemede = 1, // Destek ekibi talebi üstlendi, çalışıyor
        Cozuldu = 2,    // Destek ekibi sorunu çözdü
        Kapandi = 3     // Kapatıldı — bu durumdaki taleplere yanıt eklenemez
    }

    // ================================================================
    //  TALEBİN ÖNCELİĞİ — Otomatik atanır, kullanıcı seçemez
    // ================================================================
    // CalculatePriority() metodu (TicketController) bu değerleri döndürür.
    // Kategori + anahtar kelime analizine göre belirlenir.
    public enum TicketPriority
    {
        Dusuk = 0,   // Genel sorular — acele değil
        Orta = 1,    // Hesap/fatura sorunları — normal öncelik
        Yuksek = 2,  // Teknik sorunlar — hızlı yanıt gerekir
        Kritik = 3   // "acil", "çalışmıyor" gibi kelimeler içerir — derhal çözülmeli
    }

    // ================================================================
    //  TALEBİN KATEGORİSİ — Otomatik öncelik hesaplamada kullanılır
    // ================================================================
    // Müşteri talebi açarken bu kategorilerden birini seçer.
    // CalculatePriority() bu değere göre varsayılan önceliği belirler.
    public enum TicketCategory
    {
        GenelSoru = 0,    // → Otomatik öncelik: Düşük
        TeknikSorun = 1,  // → Otomatik öncelik: Yüksek
        HesapSorunu = 2,  // → Otomatik öncelik: Orta
        FaturaOdeme = 3,  // → Otomatik öncelik: Orta
        Diger = 4         // → Otomatik öncelik: Düşük
    }
}
