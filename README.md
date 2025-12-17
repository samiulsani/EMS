# 🎓 EMS - Smart Education Management System

**EMS (Education Management System)** is a modern, full-featured web application designed to streamline academic and administrative processes for universities and colleges. Built with **ASP.NET Core MVC (.NET 8)**, it features distinct panels for Admins, Teachers, and Students, incorporating advanced functionalities like **AI-integrated assignment grading**, **smart student promotion**, and **anonymous teacher evaluations**.

---

## 🚀 Key Features

### 👨‍💼 Admin Panel

- **User Management:** Create, manage, and delete Student and Teacher accounts securely.
- **Smart Promotion System:** Promote students to the next semester in bulk with built-in logic that automatically detects "All Clear" vs. "Failed" students based on exam results.
- **Academic History:** View comprehensive academic records (results and attendance) for students across all previous semesters.
- **Teacher Evaluation Reports:** Access anonymous feedback and ratings given by students to monitor teaching performance.
- **Curriculum Management:** Manage Departments, Semesters, Courses, and assign teachers to specific courses.

### 👨‍🏫 Teacher Panel

- **My Class Schedule:** Personalized weekly class routine with time and room details.
- **Digital Attendance:** Take and track student attendance for assigned courses.
- **Exam & Result Management:** Create exams and input marks. The system automatically calculates grades.
- **Assignment Management:** Create assignments and review student submissions.
- **Enrolled Students:** View the list of students enrolled in specific courses.

### 👨‍🎓 Student Panel

- **Interactive Dashboard:** At-a-glance view of enrolled courses, upcoming classes, and notices.
- **Academic Performance:** View semester-wise exam results and attendance reports (with percentage calculation).
- **Assignment Submission:** Submit assignments online (supports PDF/Text) and receive grades/feedback.
- **Teacher Evaluation:** Provide anonymous ratings (1-5 stars) and textual feedback for course instructors.
- **AI Assistant:** Integrated **Google Gemini AI** to assist with studies and assignment queries.

---

## 🛠️ Technologies Used

- **Framework:** ASP.NET Core MVC (.NET 8)
- **Language:** C#
- **Database:** Microsoft SQL Server (Entity Framework Core)
- **Authentication:** ASP.NET Core Identity
- **Front-end:** HTML5, CSS3, Bootstrap 5, JavaScript (jQuery)
- **AI Integration:** Google Gemini API (for AI-assisted grading and student support)
- **Libraries:**
  - `UglyToad.PdfPig` (For PDF text extraction)
  - `FontAwesome` (Icons)
  - `Chart.js` (For dashboard analytics)

---

## ⚙️ Setup & Installation

Follow these steps to set up the project locally on your machine.

### 1. Prerequisites

- **Visual Studio 2022** (with "ASP.NET and web development" workload installed).
- **.NET 8.0 SDK**.
- **Microsoft SQL Server** (LocalDB or full instance).

### 2. Clone the Repository

```bash
git clone [https://github.com/samiulsani/ems.git](https://github.com/samiulsani/ems.git)
```

### 3. Configure Database

Open the project solution (EMS.sln) in Visual Studio.

Open appsettings.json.

Update the DefaultConnection string if necessary to match your local SQL Server instance:

```bash
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=EMSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

### 4. Apply Migrations

Open the Package Manager Console (View > Other Windows > Package Manager Console) and run.

```bash
Update-Database
```
