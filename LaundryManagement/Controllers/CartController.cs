using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using LaundryManagement.Models;

namespace LaundryManagement.Controllers
{
    public class CartController : Controller
    {
        private string connectionString = "Server=DESKTOP-UNG2TM0;Database=invoice;Integrated Security=True";

        // Checkout Page
        public ActionResult Checkout(string invoiceID, string customerName, decimal totalAmount, string paymentMethod, string paymentStatus)
        {
            ViewBag.InvoiceID = invoiceID;
            ViewBag.CustomerName = customerName;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.PaymentMethod = paymentMethod;
            ViewBag.PaymentStatus = paymentStatus;
            return View();
        }

        // Confirm Payment API
        [HttpPost]
        public JsonResult ConfirmPayment(string InvoiceID, string CustomerName, decimal TotalAmount, string PaymentMethod, string BankTransactionNumber)
        {
            try
            {
                string paymentStatus = PaymentMethod == "Delayed" ? "Pending" : "Completed";

                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "INSERT INTO Payments (InvoiceID, CustomerName, TotalAmount, PaymentMethod, PaymentStatus, BankTransactionNumber, PaymentDate) " +
                                   "VALUES (@InvoiceID, @CustomerName, @TotalAmount, @PaymentMethod, @PaymentStatus, @BankTransactionNumber, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@InvoiceID", InvoiceID);
                        cmd.Parameters.AddWithValue("@CustomerName", CustomerName);
                        cmd.Parameters.AddWithValue("@TotalAmount", TotalAmount);
                        cmd.Parameters.AddWithValue("@PaymentMethod", PaymentMethod);
                        cmd.Parameters.AddWithValue("@PaymentStatus", paymentStatus);
                        cmd.Parameters.AddWithValue("@BankTransactionNumber", (object)BankTransactionNumber ?? DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
    }
}
