using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaundryManagement.Models
{
    public class InvoiceItem
    {
        public int InvoiceItemID { get; set; }
        public int InvoiceID { get; set; }
        public string ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string SelectedServices { get; set; } // ✅ Multiple Services ke liye List<string>
    }
}
