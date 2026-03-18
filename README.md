# ECommerce API

**eCommerce API** is a robust E-Commerce backend designed to handle high-concurrency scenarios efficiently. It allows users to sign up, manage products, and create orders with full transaction integrity. The system supports automatic expiration of pending orders and in-memory caching for fast data retrieval.

---

## Architecture

The project is implemented using **Domain-Driven Design (DDD) with Clean Architecture**. The main layers are:

- **Domain**: Core business logic and entities (Products, Orders, Users).  
- **Application**: Services, use-cases, DTOs, and repository interfaces.  
- **Infrastructure**: Database context, EF Core repositories, caching, logging, and external integrations.  
- **API**: RESTful controllers exposing endpoints.

### Concurrency Strategy

We use **pessimistic locking** for inventory management to ensure stock consistency under high-concurrency scenarios, such as flash sales. This approach locks product records during order creation so that only one transaction can modify stock at a time, preventing overselling and minimizing failed operations. Locks are acquired in a consistent order to prevent deadlocks.

---

## Features

- **User Management**: Create, read, update, and delete users. Authentication via Bearer token.  
- **Product Management**: CRUD operations, including bulk CSV import/export.  
- **Order Management**: Create orders with multiple items, atomic stock deduction, and automatic handling of expired pending orders.  
- **Background Processing**: Expired pending orders are automatically cancelled using `IHostedService`.  
- **Caching**: In-memory caching for frequently accessed product data.  
- **API Documentation**: Fully documented using Swagger.

---

## Domain Model Overview

- **Users**: Represent customers interacting with the system.  
- **Products**: Items available for purchase, including stock quantity, price, and description.  
- **Orders**: Contains multiple products with statuses: `Pending`, `Processing`, `Completed`, `Cancelled`, `Failed`.  
- **OrderItems**: Join table between Orders and Products. Tracks quantity and unit price.

---

## API Endpoints

All endpoints are documented in **Scalar** at:
https://localhost:7275/scalar


Key endpoints include:

- `/users` – User CRUD  
- `/products` – Product CRUD and CSV import/export  
- `/orders` – Order CRUD with stock validation and background expiration  

---

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)  
- SQL Server  
- Git  

---

## Setup & Run

1. Clone the repository:
```bash
git clone https://github.com/Yaman-Khatib/e-commerce.git
cd MonsterShop-API
```
2. Configure the connection string in appsettings.json.
3. Run EF Core migrations:
```bash
dotnet ef migrations add "InitialMigration"
dotnet ef database update
```
4. Open Scalar to test endpoints:
```
https://localhost:5001/scalar
```
---
## Future Improvements

- Integrate Redis distributed caching to save server memory instead of fully depending on in memory cashing.

- Add user roles to distinguish between Admin and Customer.

- Implement more advanced background jobs with Hangfire or Quartz.NET.

- Enhance logging and observability for production environments.

---
## Notes

- Pessimistic locking ensures stock consistency and prevents overselling during high-concurrency orders.

- Expired pending orders automatically restore stock quantities.

- Transactions are atomic: either the entire order is successfully created with stock deducted, or nothing changes.
