# PixelMart-Order Processor
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square&logo=dotnet)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791?style=flat-square&logo=postgresql)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-7.x-FF6600?style=flat-square&logo=rabbitmq)
![Docker](https://img.shields.io/badge/Docker-Ready-2496ED?style=flat-square&logo=docker)

This ASP.NET Core system delivers a scalable, event-driven e-commerce order processor. It showcases key production patterns: a decoupled microservices architecture, reliable asynchronous message processing with RabbitMQ, and robust distributed transaction management utilizing PostgreSQL.

## 🏗️ Architecture
```
┌─────────────┐         ┌──────────────┐         ┌──────────────────┐
│  Client/UI  │ ──────> │  OrderAPI    │ ──────> │   RabbitMQ       │
│             │ <────── │ (Publisher)  │         │  Message Broker  │
└─────────────┘         └──────────────┘         └──────────────────┘
                                                           │
                              ┌────────────────────────────┼────────────────────┐
                              │                            │                    │
                              ▼                            ▼                    ▼
                    ┌──────────────────┐      ┌──────────────────┐   ┌──────────────────┐
                    │ Payment Worker   │      │ Inventory Worker │   │  Email Worker    │
                    │  (Consumer)      │      │   (Consumer)     │   │  (Consumer)      │
                    └──────────────────┘      └──────────────────┘   └──────────────────┘
                              │                            │                    │
                              └────────────────────────────┼────────────────────┘
                                                           ▼
                                                 ┌──────────────────┐
                                                 │   PostgreSQL     │
                                                 │    Database      │
                                                 └──────────────────┘
```

## ✨ Features

### Core Capabilities
- ✅ **Asynchronous Order Processing** - Non-blocking order placement with immediate API response
- ✅ **Event-Driven Architecture** - Loose coupling between services using message queues
- ✅ **Distributed Transaction Management** - Multi-step order workflow with status tracking
- ✅ **Fault Tolerance** - Message acknowledgment, retry logic, and dead letter queue support
- ✅ **Data Persistence** - Full ACID compliance with PostgreSQL
- ✅ **Audit Trail** - Complete order history with timestamps and status transitions

### Technical Features
- 🔐 **Secure Configuration Management** - User Secrets for development, environment variables for production
- 📊 **Real-time Status Tracking** - Monitor order progress across multiple processing stages
- 🔄 **Automatic Retries** - Failed messages are automatically requeued for processing
- 📝 **Comprehensive Logging** - Structured logging with correlation IDs
- 🎯 **Input Validation** - FluentValidation for robust request validation
- 🐳 **Docker Support** - Full containerization with Docker Compose
- 📚 **API Documentation** - Interactive Swagger/OpenAPI documentation

### Installation

1. **Clone the repository**
```bash
   git clone https://github.com/yourusername/PixelMartOrderProcessor.git
   cd PixelMartOrderProcessor
```

2. **Start infrastructure services**
```bash
   docker-compose up -d
```
   This starts PostgreSQL and RabbitMQ containers.

3. **Configure User Secrets** (for each project)
```bash
   # OrderApi
   cd OrderApi
   dotnet user-secrets init
   dotnet user-secrets set "DB_PASSWORD" "your_postgres_password"
   
   # PaymentWorker
   cd ../PaymentWorker
   dotnet user-secrets init
   dotnet user-secrets set "DB_PASSWORD" "your_postgres_password"
   
   # InventoryWorker
   cd ../InventoryWorker
   dotnet user-secrets init
   dotnet user-secrets set "DB_PASSWORD" "your_postgres_password"
   
   # EmailWorker
   cd ../EmailWorker
   dotnet user-secrets init
   dotnet user-secrets set "DB_PASSWORD" "your_postgres_password"
```

4. **Apply database migrations**
```bash
   cd OrderApi
   dotnet ef database update
```

5. **Run the application**

   Open 4 separate terminal windows:
```bash
   # Terminal 1 - API
   cd OrderApi (PixelMartOrderProcessor)
   dotnet run
   
   # Terminal 2 - Payment Worker
   cd PaymentWorker
   dotnet run
   
   # Terminal 3 - Inventory Worker
   cd InventoryWorker
   dotnet run
   
   # Terminal 4 - Email Worker
   cd EmailWorker
   dotnet run
```

6. **Access the application**
   - API: `https://localhost:5001`
   - Swagger UI: `https://localhost:5001/swagger`
   - RabbitMQ Management: `http://localhost:15672` (guest/guest)

## 🤝 Contributing

Contributions are welcome! Please follow these guidelines:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request