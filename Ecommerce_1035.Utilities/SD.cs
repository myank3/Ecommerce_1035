using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce_1035.Utilities
{
    public class SD
    {
        public const string Role_Individual = "Individual user";
        public const string Role_Company = "Company user";
        public const string Role_Admin = "Admin user";
        public const string Role_Employee = "Employee user";

        // session
        public const string Ss_CartSessionCount = "Cart Count Session";

        public static double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity < 50)
                return price;
            else if (quantity < 100)
                return price50; return price100;
        }
        public const string OrderStatusPending = "Pending";
        public const string OrderStatusApproved = "Approved";
        public const string OrderStatusInProgress = "Processing";
        public const string OrderStatusShipped = "Shipped";
        public const string OrderStatusCancelled = "Cancelled";
        public const string OrderStatusRefunded = "Refunded";

        // Payment Status
        public const string PaymentStatusPending = "Pending";
        public const string PaymentStatusApproved = "Approved";
        public const string PaymentStatusDelayPayment = "PaymentStatusDelay";
        public const string PaymentStatusRejected = "Rejected";
    }
}
