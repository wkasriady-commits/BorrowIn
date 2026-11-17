using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BorrowIn.Models
{
    public class BookCategory : Book
    {
        public override string GetBookType() => "Category Book";
        public override string GetInfo() => $"[{Category}] {Title} oleh {Author}";
    }
}
