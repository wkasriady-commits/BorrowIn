using System.Collections.Generic;

namespace BorrowIn
{
    // Menyimpan data peminjaman sementara di memori aplikasi
    internal class BorrowRepository
    {
        // List global untuk menampung seluruh data peminjaman
        public static List<Borrower> Records = new List<Borrower>();
    }
}