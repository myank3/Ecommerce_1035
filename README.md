# 📚 Bookshelf (Ecommerce_1035)

A full-featured e-commerce web application built with **ASP.NET Core MVC**, **Entity Framework Core**, and **MS SQL Server**, styled with a classic editorial aesthetic.

🔗 **Live Demo:** [https://bookshelff.runasp.net](https://bookshelff.runasp.net)

---

## ✨ Features

- **Storefront & Catalog:** Browse curated books with title, author, category, cover type, and tiered volume pricing.
- **Shopping Cart & Checkout:** Add to cart, adjust quantities, summary preview, and integrated payment gateway processing via PayPal.
- **Identity & Role-Based Access Control (RBAC):**
  - Custom `ApplicationUser` using ASP.NET Core Identity.
  - Multi-role architecture: Customer, Admin, and Employee roles.
  - OAuth external authentication support (Google, LinkedIn).
  - Two-Factor Authentication (SMS 2FA via Twilio & TextBee).
- **Admin Management Portal:**
  - Full CRUD operations for Products, Categories, and Cover Types.
  - Order management lifecycle (Pending, Approved, Processing, Shipped, Cancelled).
  - User management and permission assignments.
- **Automated Notifications:**
  - Order and confirmation emails powered by Brevo (Sendinblue).
  - SMS updates via Twilio and TextBee.

---

## 🛠️ Tech
