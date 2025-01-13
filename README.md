
# MailCrafter - Application Layer

This repository contains the Application Layer of the MailCrafter system, built with **ASP.NET MVC**. The Application Layer serves as the front-facing interface for interacting with the system, providing API endpoints and user interfaces for users to trigger and manage email campaigns.

## Features
- **UI (User Interface)**: A web interface for users to manage email campaigns.
- **Asynchronous Processing**: Integrates with **RabbitMQ** to queue tasks for background processing by worker nodes.
- **Data Validation**: Ensures that incoming requests, including email details and recipient information, are valid before processing.

## Architecture
- Built with **ASP.NET MVC** for the frontend and the backend.
- Communicates with the **Core Layer** for business logic.
- Sends tasks to **RabbitMQ** for asynchronous processing by worker nodes.
- Retrieves and updates data in the **MongoDB** storage layer.

## Setup and Installation
### Prerequisites:
- **.NET 8 or later**
- **RabbitMQ** instance
- **MongoDB** for storing application data

### Installation Steps:
1. Clone this repository.
   ```bash
   git clone https://github.com/CodieGlot/MailCrafter.MVC.git
   ```
2. Restore dependencies.
   ```bash
   dotnet restore
   ```
3. Set up MongoDB and configure RabbitMQ.
4. Run the application.
   ```bash
   dotnet run
   ```

## Usage
- Navigate to `https://localhost:5001` to access the UI.
- Use API endpoints like `/api/send-email` to trigger email campaigns.

## License
MIT License. See LICENSE file for details.
