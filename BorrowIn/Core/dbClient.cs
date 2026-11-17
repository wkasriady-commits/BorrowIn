using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BorrowIn
{
    // Class ini menangani semua interaksi langsung dengan database SQL Server
    internal class dbClient
    {
        // String koneksi ke database SQL Express lokal
        private readonly string cs = "Data Source=.\\SQLEXPRESS;" +
             "Initial Catalog=BorrowIn;" +
             "Integrated Security=true;" +
             "TrustServerCertificate=True;";

        // =========================================================================
        // 1. User Management
        // Method method yang ada dibawah ini berfokus untuk mengelola data pengguna
        // =========================================================================

        // 1. Add Users
        // Berfungsi untuk menambahkan user baru ke tabel 'users' saat proses Sign Up
        public void addUser(Role user)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "INSERT INTO users (fullName, userName, email, password) " +
                    "VALUES (@fullName, @userName, @email, @password)";

                SqlCommand sqlCommand = new SqlCommand(query, conn);
                sqlCommand.Parameters.AddWithValue("@fullName", user.fullName);
                sqlCommand.Parameters.AddWithValue("@userName", user.userName);
                sqlCommand.Parameters.AddWithValue("@email", user.email);
                sqlCommand.Parameters.AddWithValue("@password", user.password);

                conn.Open();
                sqlCommand.ExecuteNonQuery(); // Menjalankan perintah insert
            }
        }

        // 2. Is Email Exists
        // Memeriksa apakah sebuah email sudah terdaftar di database untuk validasi unik
        public bool isEmailExists(string email)
        {
            // Membersihkan pool koneksi untuk memastikan data paling fresh
            SqlConnection.ClearAllPools();

            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT COUNT(*) FROM users WHERE email = @email";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);

                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar()); // Mengambil jumlah record yang cocok
                return count > 0; // Mengembalikan true jika ada duplikat
            }
        }

        // 3. Update Password
        // Mengupdate password user berdasarkan email (Fitur Forgot Password)
        public bool updatePassword(string email, string newPassword)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "UPDATE Users SET password = @password WHERE Email = @email";
                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@password", newPassword);
                cmd.Parameters.AddWithValue("@email", email);

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                return rowsAffected > 0;
            }
        }

        // 4.  Delete User
        // Menghapus akun user secara permanen dari database
        public bool deleteUser(string email)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "DELETE FROM users WHERE email = @email";
                SqlCommand cmd = new SqlCommand(query, conn);

                cmd.Parameters.AddWithValue("@email", email);

                conn.Open();
                int rowsAffected = cmd.ExecuteNonQuery();

                return rowsAffected > 0;
            }
        }

        // =========================================================================
        // 2. Authentication
        // Method yang mengatur login dan session user
        // =========================================================================

        // 1. Get User Login
        // Memvalidasi kredensial login dengan pengecekan Case-Sensitive pada password
        public bool loginUser(string email, string password)
        {
            updateOverdueStatuses(); // Pastikan status terupdate saat user login
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // Menggunakan COLLATE agar password 'ABC' berbeda dengan 'abc' (lebih aman)
                string query = "SELECT COUNT(*) FROM users WHERE email = @email AND password COLLATE SQL_Latin1_General_CP1_CS_AS = @pass";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@pass", password);
                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        // Method Pembantu untuk mengaktifkan sistem denda otomatis (Overdue)
        // Mengubah status 'active' menjadi 'overdue' jika melewati jatuh tempo (due_date)
        public void updateOverdueStatuses()
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // Query ini mengupdate semua record yang sudah telat tapi statusnya masih 'active'
                string query = "UPDATE Borrow SET status = 'overdue' WHERE due_date < CAST(GETDATE() AS DATE) AND status = 'active'";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 2. Get User Login
        // Mengambil data user saat login untuk disimpan ke dalam Session global
        public Role getUserLogin(string email, string password)
        {
            Role user = null;
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // Pengecekan Case-Sensitive password saat pengambilan data objek User
                string query = "SELECT * FROM users WHERE email=@email AND password COLLATE SQL_Latin1_General_CP1_CS_AS = @password";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@password", password);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    user = new Role(
                        Convert.ToInt32(reader["id"]),
                        reader["fullName"].ToString(),
                        reader["userName"].ToString(),
                        reader["email"].ToString(),
                        reader["password"].ToString()
                    );
                }
            }
            return user;
        }

        // =========================================================================
        // 3. Book Management
        // Method yang mengelola dan mengatur informasi buku
        // =========================================================================

        // 1. Get Recommended Books
        // Mendapatkan daftar buku rekomendasi untuk Banner Dinamis (5 buku terbaru)
        public List<Book> getRecommendedBooks()
        {
            List<Book> books = new List<Book>();
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // Mengambil 5 buku terbaru yang tersedia untuk ditampilkan bergantian
                string query = "SELECT TOP 5 * FROM Book WHERE is_available = 1 AND stock > 0 ORDER BY book_id DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    books.Add(new Book
                    {
                        BookId = reader["book_id"].ToString(),
                        Title = reader["title"].ToString(),
                        Author = reader["author"].ToString(),
                        Description = reader["deskripsi"].ToString(),
                        CoverImg = reader["cover_img"].ToString(),
                        FilePath = reader["file_path"].ToString()
                    });
                }
            }
            return books;
        }

        // 2. Get Latest Book
        // Mendapatkan 1 buku terbaru yang ditambahkan untuk Hero Banner
        public Book getLatestBook()
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // Ambil buku terbaru yang stoknya tersedia (Urutkan berdasarkan panjang ID lalu ID itu sendiri)
                string query = "SELECT TOP 1 * FROM Book WHERE is_available = 1 AND stock > 0 ORDER BY LEN(book_id) DESC, book_id DESC";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new Book
                    {
                        BookId = reader["book_id"].ToString(),
                        Title = reader["title"].ToString(),
                        Author = reader["author"].ToString(),
                        Description = reader["deskripsi"].ToString(),
                        CoverImg = reader["cover_img"].ToString(),
                        FilePath = reader["file_path"].ToString()
                    };
                }
                return null;
            }
        }
         
        // 3. Get Total Books
        // Mengambil statistik total buku untuk dashboard
        public int GetTotalBooks()
        {
            int total = 0;
            string query = "SELECT COUNT(*) FROM Book";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                total = (int)cmd.ExecuteScalar();
                conn.Close();
                return total;
            }
        }

        // 4. Generate Next Book ID
        // Menghasilkan ID Buku otomatis dengan format B### (Contoh: B001, B002)
        public string generateNextBookId()
        {
            string nextId = "B001";
            // Perbaikan query: Urutkan berdasarkan panjang ID terlebih dahulu agar B1000 > B999
            string query = "SELECT TOP 1 book_id FROM Book ORDER BY LEN(book_id) DESC, book_id DESC";

            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    string lastId = result.ToString().Trim();
                    // Mengambil angka setelah huruf 'B' dan menambahkannya dengan 1
                    if (lastId.Length >= 2 && lastId.StartsWith("B"))
                    {
                        if (int.TryParse(lastId.Substring(1), out int lastNum))
                        {
                            nextId = "B" + (lastNum + 1).ToString("D3"); // Format 3 digit angka (001)
                        }
                    }
                }
            }
            return nextId;
        }
         
        // 5. Is Book Duplicate
        // Memvalidasi agar tidak ada buku dengan judul atau ISBN yang sama di database
        // 5. Is Book Duplicate
        // Memvalidasi agar tidak ada buku dengan judul atau ISBN yang sama di database
        public bool isBookDuplicate(string title, string isbn13, string isbn10, string excludeId = null)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                List<string> conditions = new List<string>();
                
                if (!string.IsNullOrWhiteSpace(title)) conditions.Add("title = @title");
                if (!string.IsNullOrWhiteSpace(isbn13)) conditions.Add("isbn_13 = @isbn13");
                if (!string.IsNullOrWhiteSpace(isbn10)) conditions.Add("isbn_10 = @isbn10");

                if (conditions.Count == 0) return false;

                string query = "SELECT COUNT(*) FROM Book WHERE (" + string.Join(" OR ", conditions) + ")";

                if (!string.IsNullOrEmpty(excludeId))
                {
                    query += " AND book_id != @excludeId";
                }

                SqlCommand cmd = new SqlCommand(query, conn);
                if (!string.IsNullOrWhiteSpace(title)) cmd.Parameters.AddWithValue("@title", title.Trim());
                if (!string.IsNullOrWhiteSpace(isbn13)) cmd.Parameters.AddWithValue("@isbn13", isbn13.Trim());
                if (!string.IsNullOrWhiteSpace(isbn10)) cmd.Parameters.AddWithValue("@isbn10", isbn10.Trim());

                if (!string.IsNullOrEmpty(excludeId))
                {
                    cmd.Parameters.AddWithValue("@excludeId", excludeId.Trim());
                }

                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }

        // 6. Add Book
        // Menambah data buku baru ke tabel Book (Librarian/Admin only)
        public bool addBook(Book book)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"INSERT INTO Book (book_id, title, author, isbn_13, isbn_10, author_pages, language, publisher, dimensions, publish, deskripsi, cover_img, file_path, stock, is_available)
                                 VALUES (@id, @title, @author, @isbn13, @isbn10, @pages, @lang, @pub, @dim, @date, @desc, @img, @path, @stock, @avail)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", book.BookId);
                cmd.Parameters.AddWithValue("@title", book.Title);
                cmd.Parameters.AddWithValue("@author", book.Author);
                cmd.Parameters.AddWithValue("@isbn13", book.ISBN_13 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@isbn10", book.ISBN_10 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pages", book.AuthorPages ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@lang", book.Language ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pub", book.Publisher ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@dim", book.Dimensions ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@date", book.PublishDate);
                cmd.Parameters.AddWithValue("@desc", book.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@img", book.CoverImg ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@path", book.FilePath ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@stock", book.Stock);
                cmd.Parameters.AddWithValue("@avail", book.IsAvailable);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 7. Update Book
        // Memperbarui informasi buku yang sudah ada berdasarkan BookId
        public bool updateBook(Book book)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"UPDATE Book SET title=@title, author=@author, isbn_13=@isbn13, isbn_10=@isbn10, author_pages=@pages, 
                                 language=@lang, publisher=@pub, dimensions=@dim, publish=@date, deskripsi=@desc, cover_img=@img, file_path=@path, stock=@stock, is_available=@avail
                                 WHERE book_id=@id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", book.BookId);
                cmd.Parameters.AddWithValue("@title", book.Title);
                cmd.Parameters.AddWithValue("@author", book.Author);
                cmd.Parameters.AddWithValue("@isbn13", book.ISBN_13 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@isbn10", book.ISBN_10 ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pages", book.AuthorPages ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@lang", book.Language ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@pub", book.Publisher ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@dim", book.Dimensions ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@date", book.PublishDate);
                cmd.Parameters.AddWithValue("@desc", book.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@img", book.CoverImg ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@path", book.FilePath ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@stock", book.Stock);
                cmd.Parameters.AddWithValue("@avail", book.IsAvailable);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 8. Delete Book
        // Menghapus buku dari database secara permanen
        public bool deleteBook(string bookId)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "DELETE FROM Book WHERE book_id = @id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", bookId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 9. Get Books
        // Mengambil daftar objek Book sederhana (untuk ComboBox)
        public List<Book> getBooks()
        {
            List<Book> books = new List<Book>();
            string query = "SELECT * FROM Book";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Book book = new Book
                    {
                        BookId = reader["book_id"].ToString(),
                        Title = reader["title"].ToString(),
                        Stock = Convert.ToInt32(reader["stock"])
                    };
                    books.Add(book);
                }
            }
            return books;
        }

        // 10. Get All Books
        // Mengambil seluruh data buku untuk kebutuhan DataTable di Book Manager
        public DataTable getAllBooks()
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT * FROM Book";
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // =========================================================================
        // 4. Borrow Management
        // Method yang mengatur peminjaman dan pengembalian buku
        // =========================================================================

        // 1. Get History By User
        // Mengambil seluruh riwayat peminjaman untuk user tertentu (Join 3 Tabel)
        public DataTable getHistoryByUser(string email)
        {
            updateOverdueStatuses(); // Sinkronisasi status keterlambatan sebelum ambil data
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // Join tabel Borrow, Book, dan users untuk mendapatkan detail judul buku dan nama peminjam
                string query = @"SELECT br.borrow_id, b.book_id, b.title, br.borrow_date, br.due_date, br.return_date, br.status,
                                 b.author, b.isbn_13, b.isbn_10, b.author_pages, b.language, b.publisher, b.dimensions, b.publish, b.deskripsi, b.cover_img, b.file_path
                         FROM Borrow br 
                         JOIN Book b ON br.book_id = b.book_id 
                         JOIN users u ON br.user_id = u.id 
                         WHERE u.email = @email";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt); // Mengisi data ke dalam DataTable untuk ditampilkan di DataGridView
                return dt;
            }
        }

        // 2. Get Pending Returns
        // Mengambil daftar peminjaman yang masih aktif atau terlambat (Status: active/overdue)
        public DataTable getPendingReturns()
        {
            updateOverdueStatuses(); // Pastikan status terbaru (active/overdue) terupdate di DB
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"SELECT br.borrow_id, br.user_id, u.userName, br.book_id, b.title, 
                                 br.borrow_date, br.due_date, br.status, b.author, b.file_path
                         FROM Borrow br 
                         JOIN Book b ON br.book_id = b.book_id 
                         JOIN users u ON br.user_id = u.id 
                         WHERE br.status = 'active' OR br.status = 'overdue'";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                dt.TableName = "PendingReturns";
                adapter.Fill(dt);
                return dt;
            }
        }

        // 3. Get Due Soon
        // Mendapatkan daftar peminjaman yang akan jatuh tempo dalam <= 3 hari
        public DataTable getDueSoon(string email)
        {
            updateOverdueStatuses(); // Perbarui status sebelum memproses peringatan jatuh tempo
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // DATEDIFF menghitung selisih hari antara sekarang dan jatuh tempo
                string query = @"SELECT b.title as 'Book Title', br.due_date as 'Due Date'
                                 FROM Borrow br
                                 JOIN Book b ON br.book_id = b.book_id
                                 JOIN users u ON br.user_id = u.id
                                 WHERE u.email = @email 
                                 AND br.status = 'active' 
                                 AND DATEDIFF(day, GETDATE(), br.due_date) <= 3
                                 AND DATEDIFF(day, GETDATE(), br.due_date) >= 0
                                 ORDER BY br.due_date ASC";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // 4. Update Borrow Status
        // Mengupdate status peminjaman menjadi 'returned' dan MENAMBAH stok buku kembali
        public bool updateBorrowStatus(string borrowId, string status)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                // Menggunakan Transaction agar kedua update (status & stok) berhasil semua atau gagal semua
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Update status peminjaman di tabel Borrow
                    string updateBorrowQuery = "UPDATE Borrow SET status = @status, return_date = @return_date WHERE borrow_id = @borrow_id";
                    SqlCommand cmdBorrow = new SqlCommand(updateBorrowQuery, conn, transaction);
                    cmdBorrow.Parameters.AddWithValue("@status", status);
                    cmdBorrow.Parameters.AddWithValue("@return_date", DateTime.Now);
                    cmdBorrow.Parameters.AddWithValue("@borrow_id", borrowId);
                    cmdBorrow.ExecuteNonQuery();

                    // 2. Jika dikembalikan, otomatis menambah stok di tabel Book
                    if (status == "returned")
                    {
                        string updateStockQuery = "UPDATE Book SET stock = stock + 1 WHERE book_id = (SELECT book_id FROM Borrow WHERE borrow_id = @borrow_id)";
                        SqlCommand cmdStock = new SqlCommand(updateStockQuery, conn, transaction);
                        cmdStock.Parameters.AddWithValue("@borrow_id", borrowId);
                        cmdStock.ExecuteNonQuery();
                    }

                    transaction.Commit(); // Simpan perubahan secara permanen
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback(); // Batalkan jika terjadi error di tengah jalan
                    return false;
                }
            }
        }

        // 5. Get All Borrow History
        // Mengambil semua data dari tabel peminjaman secara mentah
        public DataTable getAllBorrowHistory()
        {
            updateOverdueStatuses(); // Perbarui status sebelum cetak laporan (Crystal Report/PDF/CSV)
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // Query lengkap untuk keperluan Crystal Report (Timeline peminjaman)
                string query = @"SELECT br.borrow_id, u.userName, b.title, 
                                 br.borrow_date, br.due_date, br.return_date, br.status
                         FROM Borrow br 
                         JOIN Book b ON br.book_id = b.book_id 
                         JOIN users u ON br.user_id = u.id 
                         ORDER BY br.borrow_date DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                dt.TableName = "Command";
                adapter.Fill(dt);
                return dt;
            }
        }

        // 6. Generate Next Borrow ID
        // Menghasilkan ID Peminjaman otomatis dengan format BR### (Contoh: BR001)
        public string generateNextBorrowId()
        {
            string nextId = "BR001";
            // Perbaikan query: Urutkan berdasarkan panjang ID terlebih dahulu agar BR1000 > BR999
            string query = "SELECT TOP 1 borrow_id FROM Borrow ORDER BY LEN(borrow_id) DESC, borrow_id DESC";

            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result != null)
                {
                    string lastId = result.ToString();
                    int lastNum = int.Parse(lastId.Substring(2));
                    nextId = "BR" + (lastNum + 1).ToString("D3");
                }
            }
            return nextId;
        }

        // 6. Get Active Borrow Count
        // Menghitung jumlah peminjaman yang masih aktif atau overdue untuk user tertentu
        public int getActiveBorrowCount(string userId)
        {
            // Menerima ID user
            using (SqlConnection conn = new SqlConnection(cs))
            {
                // menjalankan query 
                string query = "SELECT COUNT(*) FROM Borrow WHERE user_id = @userId AND (status = 'active' OR status = 'overdue')";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                conn.Open();
                return (int)cmd.ExecuteScalar();
            }
        }

        // 7. Add Borrow
        // Menambah data peminjaman baru dan MENGURANGI stok buku secara otomatis (Transaction)
        public void addBorrow(Borrower borrower)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Simpan data record peminjaman
                    string insertQuery = @"INSERT INTO Borrow
                        (borrow_id, user_id, book_id, borrow_date, due_date, return_date, pickup_code, status)
                        VALUES
                        (@borrow_id, @user_id, @book_id, @borrow_date, @due_date, NULL, @pickup_code, @status)";

                    SqlCommand cmdInsert = new SqlCommand(insertQuery, conn, transaction);
                    cmdInsert.Parameters.AddWithValue("@borrow_id", borrower.BorrowId);
                    cmdInsert.Parameters.AddWithValue("@user_id", borrower.UserID);
                    cmdInsert.Parameters.AddWithValue("@book_id", borrower.BookId);
                    cmdInsert.Parameters.AddWithValue("@borrow_date", borrower.BorrowDate);
                    cmdInsert.Parameters.AddWithValue("@due_date", borrower.DeadlineDate);
                    cmdInsert.Parameters.AddWithValue("@pickup_code", borrower.PickupCode);
                    cmdInsert.Parameters.AddWithValue("@status", borrower.Status);
                    cmdInsert.ExecuteNonQuery();

                    // 2. Kurangi stok buku karena sedang dipinjam
                    string updateStockQuery = "UPDATE Book SET stock = stock - 1 WHERE book_id = @book_id";
                    SqlCommand cmdUpdate = new SqlCommand(updateStockQuery, conn, transaction);
                    cmdUpdate.Parameters.AddWithValue("@book_id", borrower.BookId);
                    cmdUpdate.ExecuteNonQuery();

                    transaction.Commit(); // Berhasil semua
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Gagal, batalkan semua perubahan
                    throw ex;
                }
            }
        }

        // =========================================================================
        // 5. Procurement Management
        // Method yang mengatur pengadaan buku baru
        // =========================================================================

        // 1. Add Procurement
        // Menambahkan data pengadaan buku baru (Request dari Staff ke Admin)
        public void addProcurement(BookProcure procurement)
        {
            string query = @"INSERT INTO Procurement
                        (Title, Author, ISBN, PublishYear, Vendor, Ammount, InitialOffer, DeliveryDate, RequestDate, Status)
                        VALUES
                        (@title, @author, @isbn, @publish_year, @vendor, @ammount, @initial_offer, @delivery_date, @request_date, 'Pending')";

            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand call = new SqlCommand(query, conn);
                call.Parameters.AddWithValue("@title", procurement.Title);
                call.Parameters.AddWithValue("@author", procurement.Author);
                call.Parameters.AddWithValue("@isbn", procurement.ISBN);
                call.Parameters.AddWithValue("@publish_year", procurement.PublishYear);
                call.Parameters.AddWithValue("@vendor", procurement.Vendor);
                call.Parameters.AddWithValue("@ammount", procurement.Ammount);
                call.Parameters.AddWithValue("@initial_offer", procurement.InitialOffer);
                call.Parameters.AddWithValue("@delivery_date", procurement.DeliveryDate);
                call.Parameters.AddWithValue("@request_date", procurement.RequestDate);

                conn.Open();
                call.ExecuteNonQuery();
            }
        }

        // 2. Get All Procurement
        // Mengambil seluruh data pengadaan buku untuk dashboard Admin
        public DataTable getAllProcurements()
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "SELECT * FROM Procurement ORDER BY RequestDate DESC";
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // 3. Delete Procurement
        // Menghapus data pengadaan buku dari database
        public void deleteProcurement(int procurementId)
        {
            string query = "DELETE FROM Procurement WHERE ProcurementID = @procurementId";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@procurementId", procurementId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 4. Update Procurement Status
        // Memperbarui data detail pengadaan (Update manual di Grid)
        public void updateProcurementStatus(int procurementId, string title, string author, int amount, decimal offer, int publishYear, string vendor)
        {
            string query = @"UPDATE Procurement 
                         SET Title = @title, 
                         Author = @author, 
                         Ammount = @amount, 
                         InitialOffer = @offer, 
                         PublishYear = @publishYear, 
                         Vendor = @vendor 
                     WHERE ProcurementID = @procurementId";

            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@author", author);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@offer", offer);
                cmd.Parameters.AddWithValue("@publishYear", publishYear);
                cmd.Parameters.AddWithValue("@vendor", vendor);
                cmd.Parameters.AddWithValue("@procurementId", procurementId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // 5. Approve Procurement
        // Menyetujui pengadaan buku (Otomatis menambah stok atau membuat buku baru di database)
        public bool approveProcurement(int procurementId)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                conn.Open();
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Ambil detail data pengadaan
                    string getQuery = "SELECT * FROM Procurement WHERE ProcurementID = @id";
                    SqlCommand cmdGet = new SqlCommand(getQuery, conn, transaction);
                    cmdGet.Parameters.AddWithValue("@id", procurementId);

                    string title = "", author = "", isbn = "";
                    int amount = 0, year = 0;

                    using (SqlDataReader reader = cmdGet.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            title = reader["Title"].ToString();
                            author = reader["Author"].ToString();
                            isbn = reader["ISBN"].ToString();
                            amount = Convert.ToInt32(reader["Ammount"]);
                            year = Convert.ToInt32(reader["PublishYear"]);
                        }
                    }

                    // 2. Cek apakah buku sudah ada di sistem (berdasarkan ISBN atau Judul)
                    string checkQuery = "SELECT book_id, stock FROM Book WHERE isbn_13 = @isbn OR isbn_10 = @isbn OR title = @title";
                    SqlCommand cmdCheck = new SqlCommand(checkQuery, conn, transaction);
                    cmdCheck.Parameters.AddWithValue("@isbn", isbn);
                    cmdCheck.Parameters.AddWithValue("@title", title);

                    object result = cmdCheck.ExecuteScalar();
                    if (result != null)
                    {
                        // Jika buku sudah ada, cukup tambahkan stoknya saja
                        string bookId = result.ToString();
                        string updateQuery = "UPDATE Book SET stock = stock + @amt, is_available = 1 WHERE book_id = @id";
                        SqlCommand cmdUpd = new SqlCommand(updateQuery, conn, transaction);
                        cmdUpd.Parameters.AddWithValue("@amt", amount);
                        cmdUpd.Parameters.AddWithValue("@id", bookId);
                        cmdUpd.ExecuteNonQuery();
                    }
                    else
                    {
                        // Jika buku baru, buat record buku baru otomatis
                        string nextId = "B001";
                        // Perbaikan query: Urutkan berdasarkan panjang ID terlebih dahulu agar B1000 > B999
                        string idQuery = "SELECT TOP 1 book_id FROM Book ORDER BY LEN(book_id) DESC, book_id DESC";
                        SqlCommand cmdId = new SqlCommand(idQuery, conn, transaction);
                        object lastIdObj = cmdId.ExecuteScalar();
                        if (lastIdObj != null)
                        {
                            string lastId = lastIdObj.ToString().Trim();
                            if (lastId.StartsWith("B") && int.TryParse(lastId.Substring(1), out int lastNum))
                                nextId = "B" + (lastNum + 1).ToString("D3");
                        }

                        string insQuery = @"INSERT INTO Book (book_id, title, author, isbn_13, publish, stock, is_available, deskripsi)
                                            VALUES (@id, @t, @a, @isbn, @date, @s, 1, 'Imported from Procurement')";
                        SqlCommand cmdIns = new SqlCommand(insQuery, conn, transaction);
                        cmdIns.Parameters.AddWithValue("@id", nextId);
                        cmdIns.Parameters.AddWithValue("@t", title);
                        cmdIns.Parameters.AddWithValue("@a", author);
                        cmdIns.Parameters.AddWithValue("@isbn", isbn);
                        cmdIns.Parameters.AddWithValue("@date", new DateTime(year, 1, 1));
                        cmdIns.Parameters.AddWithValue("@s", amount);
                        cmdIns.ExecuteNonQuery();
                    }

                    // 3. Update status pengadaan menjadi 'Approved'
                    string statusQuery = "UPDATE Procurement SET Status = 'Approved' WHERE ProcurementID = @id";
                    SqlCommand cmdStatus = new SqlCommand(statusQuery, conn, transaction);
                    cmdStatus.Parameters.AddWithValue("@id", procurementId);
                    cmdStatus.ExecuteNonQuery();

                    transaction.Commit(); // Simpan perubahan secara permanen
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback(); // Batalkan jika terjadi error di tengah jalan
                    return false;
                }
            }
        }

        // 6. Reject Procurement
        // Menolak pengadaan buku
        public bool rejectProcurement(int procurementId)
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = "UPDATE Procurement SET Status = 'Rejected' WHERE ProcurementID = @id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", procurementId);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // 7. Get Procurement Vendor Summary
        // Menjumlahkan total jumlah buku per vendor untuk statistik Histogram di dashboard Admin
        public DataTable getProcurementVendorSummary()
        {
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"SELECT Vendor, SUM(Ammount) as TotalAmount 
                                 FROM Procurement 
                                 GROUP BY Vendor";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }

        // =========================================================================
        // 6. Dashboard & reports
        // Method yang mengatur tampilan dashboard dan laporan sistem
        // =========================================================================

        // 1. Get Total Users
        // Menjumlahkan total jumlah user terdaftar
        public int GetTotalUsers()
        {
            int total = 0;
            string query = "SELECT COUNT(*) FROM users";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                total = (int)cmd.ExecuteScalar();
                conn.Close();
                return total;
            }
        }

        // 2. Get Total Borrowed Books
        // Menjumlahkan total buku yang sedang dipinjam (active/overdue)
        public int GetTotalBorrowedBooks()
        {
            updateOverdueStatuses(); // Sinkronisasi status keterlambatan untuk statistik dashboard
            int total = 0;
            string query = "SELECT COUNT(*) FROM Borrow WHERE status = 'active' OR status = 'overdue'";
            using (SqlConnection conn = new SqlConnection(cs))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();
                total = (int)cmd.ExecuteScalar();
                conn.Close();
                return total;
            }
        }

        // 3. Get User Stats
        // Mengambil statistik peminjaman untuk user tertentu
        public DataTable getUserStats(string email)
        {
            updateOverdueStatuses(); // Pastikan data dashboard user akurat (active vs overdue)
            using (SqlConnection conn = new SqlConnection(cs))
            {
                string query = @"
                    SELECT 
                        ISNULL(SUM(CASE WHEN status = 'active' THEN 1 ELSE 0 END), 0) as Active,
                        ISNULL(SUM(CASE WHEN status = 'overdue' THEN 1 ELSE 0 END), 0) as Overdue,
                        ISNULL(SUM(CASE WHEN status = 'returned' THEN 1 ELSE 0 END), 0) as TotalRead
                    FROM Borrow br
                    JOIN users u ON br.user_id = u.id
                    WHERE u.email = @email";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@email", email);
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);
                return dt;
            }
        }
    }
}
