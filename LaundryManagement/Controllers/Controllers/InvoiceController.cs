using LaundryManagement.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Web.Mvc;
using ZXing;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace LaundryManagement.Controllers
{
    public class InvoiceController : Controller
    {
        private string connectionString = "Server=DESKTOP-UNG2TM0;Database=invoice;Integrated Security=True";

        // ✅ Customer Invoice Creation
        public ActionResult Create()
        {
            ViewBag.Items = new List<SelectListItem>
            {
                new SelectListItem { Text = "Pant", Value = "Pant|300" },
                new SelectListItem { Text = "Shirt", Value = "Shirt|250" },
                new SelectListItem { Text = "Suit", Value = "Suit|500" },
                new SelectListItem { Text = "Towel", Value = "Towel|100" }
            };
            return View();
        }

        [HttpPost]
        public JsonResult GenerateInvoice(Invoice invoice)
        {
            if (invoice == null || invoice.Items == null || invoice.Items.Count == 0)
                return Json(new { success = false, message = "Please add at least one item!" });

            int invoiceID;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string query = "INSERT INTO Invoice (CustomerName, CustomerEmail, InvoiceDate, TotalAmount) OUTPUT INSERTED.InvoiceID VALUES (@CustomerName, @CustomerEmail, @InvoiceDate, @TotalAmount)";
                        SqlCommand cmd = new SqlCommand(query, conn, transaction);
                        cmd.Parameters.AddWithValue("@CustomerName", invoice.CustomerName);
                        cmd.Parameters.AddWithValue("@CustomerEmail", invoice.CustomerEmail);
                        cmd.Parameters.AddWithValue("@InvoiceDate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);

                        invoiceID = (int)cmd.ExecuteScalar();

                        foreach (var item in invoice.Items)
                        {
                            string itemQuery = "INSERT INTO InvoiceItems (InvoiceID, ItemName, Quantity, UnitPrice) VALUES (@InvoiceID, @ItemName, @Quantity, @UnitPrice)";
                            SqlCommand itemCmd = new SqlCommand(itemQuery, conn, transaction);
                            itemCmd.Parameters.AddWithValue("@InvoiceID", invoiceID);
                            itemCmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                            itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                            itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                            itemCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Json(new { success = false, message = "Error: " + ex.Message });
                    }
                }
            }

            string barcodePath = GenerateBarcode(invoiceID.ToString());
            return Json(new
            {
                success = true,
                invoiceID,
                customerName = invoice.CustomerName,
                customerEmail = invoice.CustomerEmail,
                totalAmount = invoice.TotalAmount,
                barcodePath,
                items = invoice.Items
            });
        }

        private string GenerateBarcode(string invoiceID)
        {
            try
            {
                BarcodeWriter writer = new BarcodeWriter { Format = BarcodeFormat.CODE_128 };
                using (Bitmap bitmap = writer.Write(invoiceID))
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    string base64String = Convert.ToBase64String(ms.ToArray());
                    return "data:image/png;base64," + base64String;
                }
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        // ✅ Display Invoice Items (Fix for Error)
        public ActionResult ViewInvoiceItems(int? invoiceID)
        {
            if (invoiceID == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest, "Invoice ID is required.");
            }

            List<InvoiceItem> items = new List<InvoiceItem>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM InvoiceItems WHERE InvoiceID = @ItemID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceID", invoiceID.Value);
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    items.Add(new InvoiceItem
                    {
 
                        InvoiceItemID = (int)reader["InvoiceItemID"],
                        InvoiceID = (int)reader["InvoiceID"],
                        ItemName = reader["ItemName"].ToString(),
                        Quantity = (int)reader["Quantity"],
                        UnitPrice = (decimal)reader["UnitPrice"]
                    });
                }
            }
            return View(items);
        }

        // ✅ Admin Panel View (List Invoices + Search)
        public ActionResult AdminPanel(string search)
        {
            List<Invoice> invoices = new List<Invoice>();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Invoice WHERE CustomerName LIKE @Search OR CustomerEmail LIKE @Search";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@Search", "%" + search + "%");
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    invoices.Add(new Invoice
                    {
                        InvoiceID = (int)reader["InvoiceID"],
                        CustomerName = reader["CustomerName"].ToString(),
                        CustomerEmail = reader["CustomerEmail"].ToString(),
                        InvoiceDate = (DateTime)reader["InvoiceDate"],
                        TotalAmount = (decimal)reader["TotalAmount"]
                    });
                }
            }
            return View(invoices);
        }

        // ✅ Edit Invoice
        public ActionResult Edit(int id)
        {
            Invoice invoice = null;
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM Invoice WHERE InvoiceID = @InvoiceID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceID", id);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    invoice = new Invoice
                    {
                        InvoiceID = (int)reader["InvoiceID"],
                        CustomerName = reader["CustomerName"].ToString(),
                        CustomerEmail = reader["CustomerEmail"].ToString(),
                        InvoiceDate = (DateTime)reader["InvoiceDate"],
                        TotalAmount = (decimal)reader["TotalAmount"]
                    };
                }
            }
            return View(invoice);
        }

        [HttpPost]
        public ActionResult Edit(Invoice invoice)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "UPDATE Invoice SET CustomerName = @CustomerName, CustomerEmail = @CustomerEmail, TotalAmount = @TotalAmount WHERE InvoiceID = @InvoiceID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerName", invoice.CustomerName);
                cmd.Parameters.AddWithValue("@CustomerEmail", invoice.CustomerEmail);
                cmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                cmd.Parameters.AddWithValue("@InvoiceID", invoice.InvoiceID);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("AdminPanel");
        }

        // ✅ Delete Invoice
        public ActionResult Delete(int id)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = "DELETE FROM Invoice WHERE InvoiceID = @InvoiceID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceID", id);
                cmd.ExecuteNonQuery();
            }
            return RedirectToAction("AdminPanel");
        }
    }
}
