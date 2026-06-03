# MentoraX - Cloud-Based Tutoring Platform

## 📚 Overview

MentoraX is a cloud-based tutoring platform designed to connect students with verified tutors in a secure, scalable, and efficient learning environment. The platform addresses common challenges in online tutoring systems such as trust, scalability, tutor verification, and session management.

Built using a microservices architecture and deployed on Microsoft Azure, MentoraX provides a modern solution for online education with cloud-native technologies.

---
Live Demo
https://mentorax-webapp-e5g6aectascma9hj.southeastasia-01.azurewebsites.net/
## 🎯 Problem Statement

Many existing tutoring platforms face several issues:

- Difficulty verifying tutor credibility
- Limited scalability during high user traffic
- Poor user experience due to slow performance
- Lack of personalized and structured tutor discovery
- Manual and inefficient verification processes

MentoraX aims to solve these challenges through cloud computing, secure authentication, and scalable system design.

---

## 🚀 Features

### 👨‍🎓 Student Features
- User Registration & Login
- Search Tutors by Subject & Expertise
- Book Tutoring Sessions
- View Tutor Profiles and Portfolios

### 👨‍🏫 Tutor Features
- Create and Manage Tutor Profiles
- Showcase Qualifications and Certifications
- Display Teaching Experience and Achievements
- Manage Availability

### 👨‍💼 Admin Features
- Approve or Reject Tutor Profiles
- Manage Platform Users
- Monitor System Activity

### 🔒 Security Features
- JWT Authentication
- Role-Based Access Control (RBAC)
- Secure API Communication (HTTPS)
- Protected User Data

---

## 🏗️ System Architecture

MentoraX follows a Microservices Architecture:

```text
Client (Web Frontend)
        │
        ▼
   API Gateway
        │
 ┌──────┼──────┐
 ▼      ▼      ▼
User  Tutor  Booking
Service Service Service
        │
        ▼
 Azure SQL Database
```

### Core Services

- User Management Service
- Tutor Management Service
- Booking Management Service
- API Gateway Service

---

## 🛠️ Technology Stack

### Frontend
- HTML
- CSS
- JavaScript
- React (Optional Future Enhancement)

### Backend
- ASP.NET Core Web API

### Database
- Azure SQL Database

### Cloud Platform
- Microsoft Azure

### API Communication
- RESTful APIs

### Containerization
- Docker

---

## ☁️ Cloud Computing Concepts

- Cloud Hosting & Deployment
- Database as a Service (DBaaS)
- Microservices Architecture
- API Gateway
- Scalability & Load Handling
- Containerization

---

## 🔐 Security Implementation

- JWT-Based Authentication
- Role-Based Authorization
- Secure API Endpoints
- HTTPS Communication
- User Data Protection

---

## 📂 Project Structure

```text
MentoraX/
│
├── API-Gateway/
│
├── Services/
│   ├── UserService/
│   ├── TutorService/
│   └── BookingService/
│
├── Frontend/
│
├── Database/
│
├── Docker/
│
├── Documentation/
│
└── README.md
```

---

## ⚙️ Installation

### Prerequisites

- .NET 8 SDK
- SQL Server / Azure SQL Database
- Docker Desktop
- Visual Studio 2022
- Azure Account

### Clone Repository

```bash
git clone https://github.com/your-username/MentoraX.git
cd MentoraX
```

### Run Backend Services

```bash
dotnet restore
dotnet build
dotnet run
```

### Run Frontend

```bash
npm install
npm start
```

### Docker Deployment

```bash
docker-compose up --build
```

---

## 📊 Future Enhancements

- AI-Powered Tutor Recommendations
- Personalized Learning Paths
- LLM-Based Learning Assistant
- Video Calling Integration
- Payment Gateway Integration
- Mobile Application
- Advanced Analytics Dashboard

---

## 📈 Project Scope

The project focuses on developing a functional cloud-based tutoring system capable of:

- Managing students and tutors
- Tutor verification workflow
- Session booking management
- Secure cloud deployment
- Scalable microservice implementation

Advanced features such as payment processing, blockchain integration, and mobile applications are planned for future versions.

---

## 🧪 Testing

The platform will undergo:

- Unit Testing
- API Testing
- Integration Testing
- Security Testing
- Performance Testing

---

## 📄 License

This project is developed as part of the Cloud Computing course at Bahria University Karachi Campus for academic purposes.

---

## 📞 Contact

For project-related queries, please contact the development team through GitHub Issues.

---

### ⭐ If you find this project useful, don't forget to star the repository!
