using System;

namespace BorrowIn
{
    // Merepresentasikan data transaksi peminjaman buku
    public class Borrow
    {
        // Identitas transaksi peminjaman
        public string BorrowId { get; set; }

        // Relasi ke user dan buku
        public string UserId { get; set; }
        public string BookId { get; set; }
        public Book BorrowedBook { get; set; }

        // Informasi periode peminjaman
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }

        // Bernilai null jika buku belum dikembalikan
        public DateTime? ReturnDate { get; set; }

        // Informasi pengambilan dan status peminjaman
        public string PickupCode { get; set; }
        public string Status { get; set; }

        // Constructor kosong untuk kebutuhan object initialization
        public Borrow() { }

        // Constructor untuk membuat objek peminjaman dengan data lengkap
        public Borrow(
            string borrowId,
            string userId,
            string bookId,
            DateTime borrowDate,
            DateTime dueDate,
            string pickupCode,
            string status)
        {
            BorrowId = borrowId;
            UserId = userId;
            BookId = bookId;
            BorrowDate = borrowDate;
            DueDate = dueDate;
            PickupCode = pickupCode;
            Status = status;
        }
    }
}