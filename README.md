![.NET 8](https://img.shields.io/badge/.NET%208.0-purple?style=flat&logo=dotnet)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-MVC-blue?style=flat)
![OpenAI](https://img.shields.io/badge/AI-GPT--4o%20Vision-green?style=flat&logo=openai)
![Stripe](https://img.shields.io/badge/Payments-Stripe-635bff?style=flat&logo=stripe)
![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL-336791?style=flat&logo=postgresql)
![Docker](https://img.shields.io/badge/Docker-Containerization-2496ED?style=flat&logo=docker)
![NUnit](https://img.shields.io/badge/Testing-NUnit%20%7C%20Moq-success?style=flat)

# 🛒 Market.AI - Next-Gen E-commerce Platform

> **A smart C2C/B2C e-commerce platform that solves the problem of tedious listing creation using Generative AI.**
> The system automatically generates deep technical specifications, descriptions, and pricing based on product photos. It features a robust monolithic architecture, a secure internal wallet (Escrow model) integrated with Stripe, and an advanced administration panel.

---

## 🌐 Live Demo
The application is available at: **[market.kacperkotecki.me](https://market.kacperkotecki.me)**

---

## 🚀 Enterprise-Level Architecture & Features

### 🧠 1. AI Module (Generative Deep Specs)
The core of the app is the integration with a multimodal LLM (**GPT-4o Vision**) via OpenRouter API.
* **Image Analysis:** Photos are converted to Base64. AI recognizes the product and generates detailed technical specifications (Deep Specs) in JSON format.
* **Structured Outputs & Fallbacks:** The model's response is strictly enforced as JSON and safely deserialized into strongly-typed C# DTOs, eliminating text parsing errors and handling AI hallucinations.

### 💳 2. Hybrid Payment System (Internal Ledger & Escrow)
Transaction safety is built around an Escrow model, ensuring financial consistency.
* **Stripe Webhooks:** Payment status is updated asynchronously via Webhooks, preventing client-side manipulation.
* **Atomic Transactions:** The system maintains a funds register (`WalletBalance`). Withdrawals and balance updates are executed as atomic database transactions using the **Unit of Work** pattern to prevent "double spending".

### 🛡️ 3. Security & Aspect-Oriented Programming (AOP)
* **Custom Action Filters:** Instead of cluttering controllers with imperative `if` statements, custom authorization attributes (e.g., `[SellerFilter]`) intercept requests to ensure users have provided mandatory data (like an IBAN) before accessing specific actions.
* **Soft Delete:** Blocking users or banning auctions changes their state (`IsBlocked`, `IsBanned`) rather than physically deleting records, preserving historical financial and order integrity.

### ⚡ 4. Performance & Media Pipeline
* **On-the-fly Image Processing:** Uploaded photos are intercepted by **ImageSharp**, scaled to Full HD, and converted to the modern **WebP** format, reducing bandwidth usage by ~60-70%.

---

## 📺 System Preview

### 1. AI-First Listing ("Snap & Sell")
The user uploads photos, and the **GPT-4o (Vision)** model analyzes the product (recognizing the model, damage, and specs) and fills out the form in a fraction of a second.

<img width="1920" height="1440" alt="418shots_so" src="https://github.com/user-attachments/assets/42f98e0a-758b-4f9a-9f0a-b9f9a3a9b5c0" />
<img width="1920" height="1440" alt="806shots_so" src="https://github.com/user-attachments/assets/3aac6678-debd-408d-9c5f-ef5b127982e6" />

### 2. Wallet and Auctions
A financial dashboard with transaction history and an auction list with filtering.

<img width="1920" height="1440" alt="407shots_so" src="https://github.com/user-attachments/assets/24798596-6d48-45bd-ac01-75c0efe4d008" />
<img width="1920" height="1440" alt="878shots_so" src="https://github.com/user-attachments/assets/e24846a4-5eae-412f-8746-12e6105b368e" />

### 3. Admin Panel (Moderation) & Checkout
User management, content blocking, and a seamless checkout flow with real-time Stripe validation.

<img width="1920" height="1440" alt="822shots_so" src="https://github.com/user-attachments/assets/2f5ae7cf-9185-4a3c-97ef-783e1b76db4c" />
<img width="1920" height="1440" alt="560shots_so" src="https://github.com/user-attachments/assets/17095d8f-cb88-49cf-82e2-9ad5d3b73ce4" />

---

## 🛠️ Tech Stack

**Backend & Architecture:**
* **.NET 8.0** (ASP.NET Core MVC)
* **Clean Architecture Principles** (Repository Pattern, Unit of Work, Dependency Injection)
* **Entity Framework Core 8** (Code-First, PostgreSQL)
* **Identity** (Extended ASP.NET Core Identity for B2B/B2C profiles)

**Frontend:**
* Razor Views (`.cshtml`), Bootstrap 5 (Dark Mode)
* Vanilla JavaScript (ES6+), Fetch API, Swiper.js

**Testing:**
* **NUnit** (Test Framework)
* **Moq** (Mocking dependencies like `IUnitOfWork`)
* **FluentAssertions** (Readable test assertions)

**Integrations & Infrastructure:**
* **OpenRouter API** (GPT-4o Vision)
* **Stripe API & Webhooks**
* **Docker & Docker Compose**
* **ImageSharp**

---

## 🧪 Testing
The project includes a comprehensive suite of Unit Tests located in the `Market.Tests` project. It focuses on testing core business logic, such as the `ProfileService` and `OrderService`, ensuring that financial operations (like withdrawing funds or calculating pending balances) behave correctly under various scenarios.

To run the tests:
```bash
dotnet test Market.Tests/Market.Tests.csproj
```

---

## ⚙️ Getting Started (Local Development)

### Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for PostgreSQL database)
* Stripe Account (for API Keys)
* OpenRouter Account (for AI API Keys)

### Installation Steps

1. **Clone the repository:**
   ```bash
   git clone https://github.com/KacperKotecki/Market.git
   cd Market
   ```

2. **Set up the Database (Docker):**
   Run the provided `docker-compose.yml` to spin up the PostgreSQL instance:
   ```bash
   docker-compose up -d
   ```

3. **Configure Environment Variables:**
   Update the `appsettings.json` or use .NET User Secrets to configure your API keys and Database connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Port=5432;Database=MarketDb;Username=postgres;Password=YourPassword"
   },
   "Stripe": {
     "SecretKey": "sk_test_...",
     "PublishableKey": "pk_test_...",
     "WebhookSecret": "whsec_..."
   },
   "OpenRouter": {
     "ApiKey": "sk-or-v1-...",
     "Model": "openai/gpt-4o"
   }
   ```

4. **Apply Migrations & Run:**
   The application is configured to automatically apply pending migrations and seed default Admin roles on startup.
   ```bash
   cd Market.Web
   dotnet run
   ```

---

## 📬 Contact

**Kacper Kotecki** 📧 kacperkotecki@protonmail.com  
🔗 [LinkedIn Profile](https://www.linkedin.com/in/kacper-kotecki-349829295/)
