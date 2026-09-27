# 🐾 FurShield – Pet Shelter Management System

FurShield is a modern **Pet Shelter Management System** designed to connect pet owners, animal shelters, and veterinarians in one platform.

The platform helps users discover pets for adoption, manage pet health records, book veterinary appointments, and access important pet-related services. Shelters can manage available pets and coordinate adoption, while veterinarians can manage appointments and pet medical information.

## ✨ Features

### 🐶 Pet Owners

* Register and manage personal profile
* Add and manage multiple pets
* View pet information and health records
* Upload important pet documents
* Book veterinary appointments
* View appointment details
* Manage pet vaccination and medical information
* Manage family/shared pet access

### 🏠 Pet Shelters

* Shelter registration and management
* Add and manage pets
* Manage available pets for adoption
* Maintain pet care information
* Coordinate with potential adopters
* Manage shelter profile

### 👩‍⚕️ Veterinarians

* Veterinarian profile
* Specialization and experience information
* Manage available appointment slots
* View pet medical history
* Add treatments and observations
* Manage veterinary appointments

### 🛠️ Admin

* Manage users
* Manage pet owners
* Manage shelters
* Manage veterinarians
* Manage pets
* Manage appointments
* Manage health records
* Manage platform content

## 📋 Main Modules

* 🏠 Home
* 🐕 Pet Listings
* ❤️ Pet Adoption
* 👤 Pet Owner
* 🏥 Pet Shelter
* 👩‍⚕️ Veterinarian
* 📅 Appointments
* 🩺 Health Records
* 💉 Vaccination Records
* 📄 Pet Documents
* 👨‍👩‍👧 Family / Shared Access
* 💬 Chatbot
* 🔐 Authentication & Authorization
* 🛠️ Admin Dashboard

## 👥 User Roles

### Admin

Manages the overall platform, users, pets, shelters, veterinarians, appointments, and other system data.

### Pet Owner

Can manage their profile and pets, view health information, book appointments, and explore pets available for adoption.

### Pet Shelter

Can register their shelter, manage pets, maintain pet information, and coordinate adoption activities.

### Veterinarian

Can manage their professional profile, appointments, pet medical history, treatments, and observations.

## 🛠️ Technologies Used

* ASP.NET Core MVC
* C#
* Entity Framework Core
* SQL Server
* ASP.NET Core Identity
* HTML5
* CSS3
* Bootstrap 5
* JavaScript
* Font Awesome

## 🗄️ Database

FurShield uses **SQL Server** with **Entity Framework Core** for database management.

The database stores information related to:

* Users
* Pet Owners
* Pets
* Shelters
* Veterinarians
* Appointments
* Health Records
* Vaccination Records
* Adoption
* Family Access
* Other pet-related information

## 🔐 Authentication & Authorization

FurShield uses ASP.NET Core Identity for authentication and role-based authorization.

Different dashboards and features are provided according to the user's role:

* Admin
* Pet Owner
* Pet Shelter
* Veterinarian

## 📁 Project Structure

```text
FurShield
│
├── Areas
│   └── Identity
│
├── Controllers
│
├── Models
│
├── Data
│
├── Views
│
├── wwwroot
│   ├── css
│   ├── js
│   ├── images
│   └── uploads
│
├── Migrations
│
├── appsettings.json
└── Program.cs
```

## 🚀 How to Run the Project

### 1. Clone the Repository

```bash
git clone https://github.com/faryal-2503e1/FurShield-Pet-Shelter.git
```

### 2. Open the Project

Open the project in **Visual Studio**.

### 3. Configure Database

Update the SQL Server connection string in:

```text
appsettings.json
```

### 4. Apply Migrations

Run:

```bash
Update-Database
```

or:

```bash
dotnet ef database update
```

### 5. Run the Application

Run the project from Visual Studio or use:

```bash
dotnet run
```

## 🎯 Project Objective

The main objective of FurShield is to provide a centralized digital platform for managing pet adoption, pet health information, veterinary appointments, and shelter activities.

The system aims to make pet care and adoption management more organized, accessible, and user-friendly.

## 👩‍💻 Developer

**Faryal Nasir**

FurShield – Pet Shelter Management System
