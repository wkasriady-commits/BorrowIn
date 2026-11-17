using System;

namespace BorrowIn
{
    // Merepresentasikan data pengajuan pengadaan buku
    public class BookProcure
    {
        // Informasi identitas pengadaan
        public int ProcurementID { get; set; }

        // Informasi buku yang diajukan
        public string Title { get; set; }
        public string Author { get; set; }
        public string ISBN { get; set; }
        public int PublishYear { get; set; }

        // Informasi vendor dan penawaran
        public string Vendor { get; set; }
        public int Ammount { get; set; }
        public decimal InitialOffer { get; set; }

        // Informasi tanggal pengadaan
        public DateTime DeliveryDate { get; set; }
        public DateTime RequestDate { get; set; }

        // Constructor kosong digunakan agar objek dapat dibuat
        // lalu diisi melalui property (Object Initializer)
        public BookProcure()
        {
        }
    }
}