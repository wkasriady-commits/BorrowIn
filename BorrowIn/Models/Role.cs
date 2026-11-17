namespace BorrowIn
{
    // Merepresentasikan data pengguna dalam sistem BorrowIn
    public class Role
    {
        // Identitas user
        public int Id { get; set; }
        // Informasi akun user
        public string fullName { get; set; }
        public string userName { get; set; }
        public string email { get; set; }
        public string password { get; set; }

        // Constructor untuk membuat akun baru saat proses registrasi
        public Role(string fullName, string userName,
                    string email, string password)
        {
            this.fullName = fullName;
            this.userName = userName;
            this.email = email;
            this.password = password;
        }

        // Constructor untuk membuat objek User dari data database
        public Role(int Id, string fullName, string userName,
                    string email, string password)
        {
            this.Id = Id;
            this.fullName = fullName;
            this.userName = userName;
            this.email = email;
            this.password = password;
        }

        // polymorphism
        // mengembalikan role user (di-override oleh child class)
        public virtual string GetRole()
        {
            return "Role";
        }

        // mengembalikan informasi ringkas user (di-override oleh child class)
        public virtual string GetInfo()
        {
            return $"[{GetRole()}] {fullName} ({email})";
        }
    }
}
