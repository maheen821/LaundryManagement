using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaundryManagement.Models
{
    public class Invoice
    {
        public int InvoiceID { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime InvoiceDate { get; set; }
        public List<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}