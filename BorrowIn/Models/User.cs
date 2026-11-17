using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace BorrowIn.Models
{
    // class khusus role user biasa
    public class User : Role
    {
        public User(string fullName, string userName,
                           string email, string password)
            : base(fullName, userName, email, password) { }

        // constructor
        public User(int Id, string fullName, string userName,
                           string email, string password)
            : base(Id, fullName, userName, email, password) { }

        // mengambil informasi user
        public override string GetRole() => "Member";
        public override string GetInfo() => $"[Member] {fullName} ({email})";
    }
}
