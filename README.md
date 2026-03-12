MyFSchool

MyFSchool is a school management system designed for parents, students, teachers, and administrators.
The project centralizes common school operations into one platform, including user management, student records, classes, timetables, attendance, leave requests, clubs, assignments, payments, announcements, and internal messaging.

Overview

The main goal of MyFSchool is to provide a unified system for managing school-related information and communication.

The system supports:

User and role management

Parent, student, and teacher management

Class and academic year management

Timetable and schedule management

Attendance tracking

Student requests and approval workflows

Club registration

Assignments and submissions

Grades and conduct tracking

Tuition fees and payments

Announcements, news, and events

Internal messaging

Technology Stack
Backend

ASP.NET Core Web API

Entity Framework Core

SQL Server

Database

SQL schema and seed data are provided in:

database.sql
Main Modules
1. User and Role Management

This module manages system accounts and permissions.

Tables:

users

roles

user_roles

2. Parent, Student, and Teacher Management

This module stores personal and academic relationships between users in the system.

Tables:

students

parents

teachers

parent_student

3. Class and Academic Structure

This module organizes students by class, school year, semester, and subject.

Tables:

classes

class_students

academic_years

semesters

subjects

4. Student Requests and Approval Workflow

This module handles leave requests and other request types submitted by students or parents.

Tables:

request_types

student_requests

request_approvals

5. Club Management

This module allows students to browse and register for school clubs.

Tables:

clubs

club_registrations

6. Timetable and Scheduling

This module manages class schedules based on day of week, time slot, subject, teacher, semester, and academic year.

Tables:

schedule_slots

timetables

7. Attendance

This module tracks attendance status for students.

Tables:

attendance_records

meal_attendance

8. Grades and Conduct

This module stores academic results, summaries, conduct records, and reward/discipline entries.

Tables:

grade_categories

student_grades

student_grade_summary

student_conduct

student_rewards_discipline

9. Assignments and Submissions

This module supports assignment creation, attachments, submission tracking, and student files.

Tables:

assignments

assignment_attachments

student_assignment_submissions

student_assignment_files

10. Fees and Payments

This module manages fee terms, fee items, service registrations, and payment history.

Tables:

fee_terms

fee_items

student_fee_items

service_registrations

payments

payment_details

11. Menus and Meals

This module supports school meal planning and attendance for meals.

Tables:

menus

menu_items

meal_attendance

12. Announcements, News, and Events

This module manages school-wide communication and updates.

Tables:

announcements

announcement_reads

news_posts

events

13. Internal Messaging

This module allows private communication between users.

Tables:

conversations

conversation_participants

messages

message_attachments

Database Design Notes
Parent-Student Relationship

The system uses the parent_student table as a junction table between parents and students.
This relationship is important for parent-facing features such as:

Viewing child information

Viewing child timetable

Viewing child club registrations

Viewing attendance records

Viewing fee/payment information

Receiving student-related announcements

Timetable Structure

Timetable data is built from the following entities:

timetables

schedule_slots

subjects

teachers

classes

academic_years

semesters

This structure allows the system to generate schedules by class and semester.

Club Registration

Club registration is managed through:

clubs

club_registrations

The registration table should prevent duplicate registrations for the same student in the same club.

Seed Data

The database script includes sample data for testing and development.

Sample Accounts

Example seeded accounts include:

parent01

teacher01

admin01

There are also additional teacher accounts for timetable testing.

Sample Student

HS001

Sample Relationship

A sample parent-student relationship is already created to support parent-side testing.

Sample Timetable

The script includes timetable data for:

Academic year: 2025-2026

Semester: Semester 2

Multiple weekdays

Multiple subjects and teachers

This data can be used to test schedule-related screens and APIs.

Getting Started
1. Clone the repository
git clone <your-repository-url>
cd MyFSchool
2. Create the database

Open SQL Server and execute:

database.sql

This script will create the database schema, relationships, indexes, and sample data.

3. Configure the connection string

Update appsettings.json in the API project with your SQL Server connection string.

Example:

{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MyFSchool;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
4. Restore dependencies
dotnet restore
5. Build the project
dotnet build
6. Run the API
dotnet run

Or with a specific launch profile:

dotnet run --launch-profile https
Suggested Test Accounts

You can use the seeded accounts for testing:

Parent

Username: parent01

Teacher

Username: teacher01

Admin

Username: admin01

Note: if passwords in the seed script are plain-text or demo values, they should only be used for development.
In production, passwords must be hashed and secured properly.

Suggested API Areas

Depending on your current implementation, the project may include APIs such as:

/api/auth

/api/students

/api/parents

/api/schedule

/api/clubs

/api/club-registrations

/api/attendance

/api/assignments

/api/payments

/api/announcements

/api/messages

Actual routes may vary depending on the controller structure.

Development Notes
Entity Framework Core Mapping

Many-to-many or junction tables should be mapped carefully in EF Core, especially tables such as:

parent_student

class_students

conversation_participants

These tables should have proper entity definitions and composite key configuration in DbContext.

DbSet Naming Consistency

Keep naming consistent between entities and DbSet properties.

Example:

Entity: ParentStudent

DbSet: ParentStudents

Recommended Project Structure

A clean architecture for the API project may include:

Controllers

Models

DTOs

Services

Repositories

Data

Security Improvements

For a production-ready version, the project should include:

JWT authentication

Role-based authorization

Password hashing

Refresh token support

Input validation

Global exception handling

Logging

Swagger / OpenAPI documentation

Future Improvements

Possible next steps for the project:

Complete JWT login and authorization

Build parent, student, and teacher mobile/web interfaces

Add full CRUD for clubs and club registrations

Add timetable API by student and parent

Add attendance and leave request APIs

Add announcements and chat features

Add admin dashboard and reports

Purpose

This project is intended for learning, development practice, or as a foundation for a school management application.
