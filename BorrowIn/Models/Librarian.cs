using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace BorrowIn.Models
{
    // class khusus role librarian
    public class Librarian : Role // inheritance class role
    {
        public Librarian(string fullName, string userName,
                         string email, string password)
            : base(fullName, userName, email, password) { }

        // constructor 
        public Librarian(int Id, string fullName, string userName,
                         string email, string password)
            : base(Id, fullName, userName, email, password) { }

        // mengambil informasi librarian
        public override string GetRole() => "Librarian";
        public override string GetInfo() => $"[Librarian] {fullName} ({email})";
    }
}
