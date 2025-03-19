
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
                new SelectListItem { Text = "Pant", Value = "Pant|150" },
                  new SelectListItem { Text = "Coat", Value = "Coat|150" },
    new SelectListItem { Text = "Scarf", Value = "Scarf|150" },

          new SelectListItem { Text = "Hoodie", Value = "Hoodie|150" },
    new SelectListItem { Text = "Tie", Value = "Tie|150" },
      new SelectListItem { Text = "Cap", Value = "Cap|150" },
    new SelectListItem { Text = "Dupatta", Value = "Dupatta|150" },
      new SelectListItem { Text = "Bedsheet", Value = "Bedshow|150" },
    new SelectListItem { Text = "Pillow Cover", Value = "Pillow Cover|150" },
                new SelectListItem { Text = "Shirt", Value = "Shirt|250" },
                new SelectListItem { Text = "Suit", Value = "Suit|500" },
                new SelectListItem { Text = "Sweater", Value = "Sweater|500" },
          new SelectListItem { Text = "Towel", Value = "Towel|200" }
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

            return Json(new { success = true, invoiceID });
        }

        // ✅ Generate Barcode for Invoice
        public ActionResult GenerateBarcode(int id)
        {
            try
            {
                BarcodeWriter writer = new BarcodeWriter { Format = BarcodeFormat.CODE_128 };
                using (Bitmap bitmap = writer.Write(id.ToString()))
                using (MemoryStream ms = new MemoryStream())
                {
                    bitmap.Save(ms, ImageFormat.Png);
                    return File(ms.ToArray(), "image/png");
                }
            }
            catch
            {
                return HttpNotFound();
            }
        }

        // ✅ Invoice Receipt View
        public ActionResult Receipt(int id)
        {
            Invoice invoice = null;
            List<InvoiceItem> items = new List<InvoiceItem>();
            string paymentMethod = "N/A";
            string paymentStatus = "Pending";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // ✅ Fetch Invoice Details
                string query = "SELECT * FROM Invoice WHERE InvoiceID = @InvoiceID";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceID", id);
                SqlDataReader reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    invoice = new Invoice
                    {
                        InvoiceID = Convert.ToInt32(reader["InvoiceID"]),
                        CustomerName = reader["CustomerName"].ToString(),
                        CustomerEmail = reader["CustomerEmail"].ToString(),
                        InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                    };
                }
                reader.Close();

                // ✅ Fetch Payment Details (JOIN Payment Table)
                query = "SELECT PaymentMethod, PaymentStatus FROM Payments WHERE InvoiceID = @InvoiceID";
                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceID", id);
                reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    paymentMethod = reader["PaymentMethod"] != DBNull.Value ? reader["PaymentMethod"].ToString() : "N/A";
                    paymentStatus = reader["PaymentStatus"] != DBNull.Value ? reader["PaymentStatus"].ToString() : "Pending";
                }
                reader.Close();

                // ✅ Fetch Invoice Items
                query = "SELECT * FROM InvoiceItems WHERE InvoiceID = @InvoiceID";
                cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@InvoiceID", id);
                reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    items.Add(new InvoiceItem
                    {
                        InvoiceID = Convert.ToInt32(reader["InvoiceID"]),
                        ItemName = reader["ItemName"].ToString(),
                        Quantity = Convert.ToInt32(reader["Quantity"]),
                        UnitPrice = Convert.ToDecimal(reader["UnitPrice"])
                    });
                }
            }

            if (invoice == null)
                return HttpNotFound("Invoice Not Found!");

            // ✅ Assign Data to ViewBag
            ViewBag.InvoiceID = invoice.InvoiceID;
            ViewBag.CustomerName = invoice.CustomerName;
            ViewBag.CustomerEmail = invoice.CustomerEmail;
            ViewBag.TotalAmount = invoice.TotalAmount;
            ViewBag.PaymentMethod = paymentMethod;  // ✅ Now fetched from Payment Table
            ViewBag.PaymentStatus = paymentStatus;  // ✅ Now fetched from Payment Table
            ViewBag.Items = items;

            return View(invoice);
        }


        // ✅ Admin Panel (List Invoices + Search)
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
                        InvoiceID = Convert.ToInt32(reader["InvoiceID"]),
                        CustomerName = reader["CustomerName"].ToString(),
                        CustomerEmail = reader["CustomerEmail"].ToString(),
                        InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"]),
                        TotalAmount = Convert.ToDecimal(reader["TotalAmount"])
                    });
                }
            }
            return View(invoices);
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
        [HttpPost]
        public ActionResult Edit(Invoice invoice)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // **Step 1: Invoice Table ko Update Karna**
                string query = @"UPDATE Invoice 
                         SET CustomerName = @CustomerName, TotalAmount = @TotalAmount 
                         WHERE CustomerEmail = @CustomerEmail";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@CustomerName", invoice.CustomerName);
                cmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                cmd.Parameters.AddWithValue("@CustomerEmail", invoice.CustomerEmail);
                cmd.ExecuteNonQuery();

                // **Step 2: Invoice Items Table ko Update Karna**
                foreach (var item in invoice.Items)
                {
                    string itemQuery = @"UPDATE InvoiceItems 
                                 SET Quantity = @Quantity, UnitPrice = @UnitPrice 
                                 WHERE InvoiceID = (SELECT InvoiceID FROM Invoice WHERE CustomerEmail = @CustomerEmail) 
                                 AND ItemName = @ItemName";

                    SqlCommand itemCmd = new SqlCommand(itemQuery, conn);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                    itemCmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                    itemCmd.Parameters.AddWithValue("@CustomerEmail", invoice.CustomerEmail);
                    itemCmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("AdminPanel"); // Edit ke baad Admin Panel pe redirect hoga
        }

        public ActionResult ViewInvoice()
        {
            List<Invoice> invoices = new List<Invoice>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT i.InvoiceID, i.CustomerName, i.CustomerEmail, i.InvoiceDate, i.TotalAmount, 
                                ii.ItemName, ii.Quantity, ii.UnitPrice
                         FROM Invoice i
                         LEFT JOIN InvoiceItems ii ON i.InvoiceID = ii.InvoiceID
                         ORDER BY i.InvoiceID DESC";

                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                Dictionary<int, Invoice> invoiceDict = new Dictionary<int, Invoice>();

                while (reader.Read())
                {
                    int invoiceID = Convert.ToInt32(reader["InvoiceID"]);

                    if (!invoiceDict.ContainsKey(invoiceID))
                    {
                        invoiceDict[invoiceID] = new Invoice
                        {
                            InvoiceID = invoiceID,
                            CustomerName = reader["CustomerName"].ToString(),
                            CustomerEmail = reader["CustomerEmail"].ToString(),
                            InvoiceDate = Convert.ToDateTime(reader["InvoiceDate"]),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            Items = new List<InvoiceItem>()
                        };
                    }

                    if (reader["ItemName"] != DBNull.Value)
                    {
                        invoiceDict[invoiceID].Items.Add(new InvoiceItem
                        {
                            ItemName = reader["ItemName"].ToString(),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"])
                        });
                    }
                }

                invoices = new List<Invoice>(invoiceDict.Values);
            }

            return View(invoices);
        }
        // ✅ Edit Invoice (GET)
        public ActionResult EditView(int id)
        {
            Invoice invoice = null;
            List<InvoiceItem> invoiceItems = new List<InvoiceItem>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Fetch Invoice Details
                string invoiceQuery = "SELECT * FROM Invoice WHERE InvoiceID = @InvoiceID";
                SqlCommand invoiceCmd = new SqlCommand(invoiceQuery, conn);
                invoiceCmd.Parameters.AddWithValue("@InvoiceID", id);
                SqlDataReader invoiceReader = invoiceCmd.ExecuteReader();

                if (invoiceReader.Read())
                {
                    invoice = new Invoice
                    {
                        InvoiceID = Convert.ToInt32(invoiceReader["InvoiceID"]),
                        CustomerName = invoiceReader["CustomerName"].ToString(),
                        CustomerEmail = invoiceReader["CustomerEmail"].ToString(),
                        InvoiceDate = Convert.ToDateTime(invoiceReader["InvoiceDate"]),
                        TotalAmount = Convert.ToDecimal(invoiceReader["TotalAmount"])
                    };
                }
                invoiceReader.Close();

                // Fetch Invoice Items
                string itemsQuery = "SELECT * FROM InvoiceItems WHERE InvoiceID = @InvoiceID";
                SqlCommand itemsCmd = new SqlCommand(itemsQuery, conn);
                itemsCmd.Parameters.AddWithValue("@InvoiceID", id);
                SqlDataReader itemsReader = itemsCmd.ExecuteReader();

                while (itemsReader.Read())
                {
                    invoiceItems.Add(new InvoiceItem
                    {

                        ItemName = itemsReader["ItemName"].ToString(),
                        Quantity = Convert.ToInt32(itemsReader["Quantity"]),
                        UnitPrice = Convert.ToDecimal(itemsReader["UnitPrice"])
                    });
                }
            }

            if (invoice == null)
                return HttpNotFound("Invoice Not Found!");

            invoice.Items = invoiceItems;
            return View(invoice);
        }

        // ✅ Edit Invoice (POST)
        [HttpPost]
        public ActionResult EditView(Invoice invoice)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Update Invoice Details
                string updateInvoiceQuery = @"UPDATE Invoice 
                                      SET CustomerName = @CustomerName, 
                                          CustomerEmail = @CustomerEmail, 
                                          TotalAmount = @TotalAmount 
                                      WHERE InvoiceID = @InvoiceID";

                SqlCommand cmd = new SqlCommand(updateInvoiceQuery, conn);
                cmd.Parameters.AddWithValue("@InvoiceID", invoice.InvoiceID);
                cmd.Parameters.AddWithValue("@CustomerName", invoice.CustomerName);
                cmd.Parameters.AddWithValue("@CustomerEmail", invoice.CustomerEmail);
                cmd.Parameters.AddWithValue("@TotalAmount", invoice.TotalAmount);
                cmd.ExecuteNonQuery();

                // Update Invoice Items
                foreach (var item in invoice.Items)
                {
                    string updateItemQuery = @"UPDATE InvoiceItems 
                                       SET ItemName = @ItemName, 
                                           Quantity = @Quantity, 
                                           UnitPrice = @UnitPrice 
                                       WHERE ItemID = @ItemID";

                    SqlCommand itemCmd = new SqlCommand(updateItemQuery, conn);
                    itemCmd.Parameters.AddWithValue("@ItemName", item.ItemName);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                    itemCmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("ViewInvoice");
        }

    }
}
