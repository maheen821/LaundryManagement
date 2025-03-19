using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LaundryManagement.Models
{
    public class Payment
    {
        public int PaymentID { get; set; }
        public string InvoiceID { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; } // "Completed" or "Pending"
        public string BankTransactionNumber { get; set; } // Nullable
        public DateTime PaymentDate { get; set; }

    }
}