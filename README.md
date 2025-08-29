# 🚀 Integrated API Logging & Analysis System

## 📝 Introduction

APIs are the backbone of modern software, enabling seamless communication between applications and services. However, as systems grow in complexity, ensuring the reliability, performance, and correctness of APIs becomes increasingly challenging. The **Integrated API Logging & Analysis System** provides a comprehensive solution for this challenge by combining automated testing, real-time logging, and interactive visualization into a single, end-to-end platform.

This system transforms raw API test data into actionable insights, empowering developers, testers and system administrators to monitor, analyze, and improve API performance with efficiency and confidence.

---

## 🔄 Pipeline Overview

The project implements a streamlined end-to-end pipeline:
API Testing (Python Script) → RabbitMQ → Elasticsearch → .NET Application

### 🛠 Pipeline Breakdown

**🐍 API Testing (Python Script)**
- Executes automated API tests and generates detailed logs (✅ success, ⚠️ warnings, ❌ client/server errors).
- Publishes logs to RabbitMQ and consumes them for additional processing or analysis.
  
**🐇 RabbitMQ**
- Acts as a reliable message broker, decoupling API testing from log storage and visualization.
- Two users created using Docker:
  - `api_logger` – publishes logs
  - `api_consumer` – consumes logs
- Ensures no log is lost and supports scalable message handling.

**🔍 Elasticsearch**
- Stores and indexes logs for lightning-fast search, filtering, and analysis.
- Provides the backend for the .NET application to retrieve and display logs efficiently.

**💻 .NET Application**
- Interactive web interface featuring:
  - **🏠 Home Page** – project overview and status.
  - **📄 Logs Page** – explore all logs with options to:
    - Select specific logs and send them via email ✉️.
    - Download a PDF report containing all logs 📑.
  - **🛠 Guide Page** – provides explanations for common errors and actionable troubleshooting steps.

---

## 🛠 Technologies Used

- **🐍 Python** – Automates API testing, publishes/consumes logs to/from RabbitMQ.
- **🐇 RabbitMQ** – Reliable message broker, configured with dedicated users for logging and consuming.
- **🔍 Elasticsearch** – Fast, persistent storage and search engine for structured logs.
- **💻 .NET** – Provides an intuitive web interface for log exploration, emailing, and reporting.
- **🐳 Docker** – Containerization for RabbitMQ and Elasticsearch for rapid deployment and scalability.

---

### ✨ Key Features

- **🔄 End-to-End Automation** – From test execution to visualization, the entire pipeline runs seamlessly.
- **📨 Reliable Messaging** – RabbitMQ ensures every log is delivered and processed without loss.
- **💾 Smart Data Storage** – Elasticsearch allows rapid searching, filtering, and analysis of logs.
- **📊 Interactive Visualization** – The .NET application enables:
  - Viewing logs intuitively by category (✅ success, ⚠️ warning, ❌ error).
  - Selecting logs to email directly from the interface ✉️.
  - Downloading full PDF reports for sharing or documentation 📑.
  - Accessing a guided page explaining common errors and fixes 🛠.
