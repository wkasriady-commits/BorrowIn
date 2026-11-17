using System;
namespace BorrowIn
{
    // Merepresentasikan data buku yang tersimpan di sistem BorrowIn
    public class Book
    {
        // Informasi dasar buku
        public string BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        // Informasi ketersediaan buku
        public bool IsAvailable { get; set; }
        public int Stock { get; set; }
        // Informasi bibliografi buku
        public string ISBN_13 { get; set; }
        public string ISBN_10 { get; set; }
        public int? AuthorPages { get; set; }
        public string Language { get; set; }
        public string Publisher { get; set; }
        public string Dimensions { get; set; }
        public DateTime PublishDate { get; set; }
        // Informasi tambahan buku
        public string Description { get; set; }
        public string CoverImg { get; set; }
        public string FilePath { get; set; }

        // Mengurangi stok buku saat proses peminjaman
        public bool borrowBooks()
        {
            if (Stock > 0)
            {
                Stock--;
                return true;
            }
            else
            {
                return false;
            }
        }

        // Menandai buku sebagai tidak tersedia
        public void MarkAsBorrowed()
        {
            IsAvailable = false;
        }

        // Menandai buku sebagai tersedia kembali
        public void MarkAsReturned()
        {
            IsAvailable = true;
        }

        // --- POLYMORPHISM ---
        // Mengembalikan tipe buku (di-override oleh child class)
        public virtual string GetBookType()
        {
            return "Book";
        }

        // Mengembalikan informasi ringkas buku (di-override oleh child class)
        public virtual string GetInfo()
        {
            return $"[{GetBookType()}] {Title} oleh {Author}";
        }
    }
}
