using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace LaundryManagement
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
            // Customer Routes
            
            routes.MapRoute(
              name: "InvoiceCreate",
              url: "Invoice/Create",
              defaults: new { controller = "Customer", action = "CustomerList" }
          );

            routes.MapRoute(
              name: "InvoiceAdminPanel",
              url: "InvoiceAdminPanel",
              defaults: new { controller = "Invoice", action = "AdminPanel" }
          );

            routes.MapRoute(
              name: "InvoiceViewInvoice",
              url: "InvoiceViewInvoice",
              defaults: new { controller = "Invoice", action = "ViewInvoice" }
          );
            routes.MapRoute(
              name: "InvoiceReceipt",
              url: "InvoiceReceipt.",
              defaults: new { controller = "Invoice", action = "Receipt" }
          );
            routes.MapRoute(
              name: "InvoiceEditView",
              url: "InvoiceEditView",
              defaults: new { controller = "Invoice", action = "EditView" }
          );
            routes.MapRoute(
             name: "InvoiceEdit",
             url: "InvoiceEdit",
             defaults: new { controller = "Invoice", action = "Edit" }
         );

        }
    }
}
