using System;

namespace BorrowIn
{
    // Merepresentasikan data peminjaman yang akan disimpan ke database
    public class Borrower
    {
        // ID unik transaksi peminjaman
        public string BorrowId { get; set; }

        // ID user yang melakukan peminjaman
        public int UserID { get; set; }

        // ID buku yang dipinjam
        public string BookId { get; set; }

        // Tanggal peminjaman dilakukan
        public DateTime BorrowDate { get; set; }

        // Batas waktu pengembalian buku
        public DateTime DeadlineDate { get; set; }

        // Kode yang digunakan untuk pengambilan buku
        public string PickupCode { get; set; }

        // Status peminjaman (active, overdue, returned)
        public string Status { get; set; }
    }
}