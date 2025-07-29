# Blazor Quiz App

A modern, interactive quiz application built with Blazor Server and ASP.NET Core Identity.

## 🚀 Features

### Core Functionality
- **Interactive Quiz System**: Take timed quizzes with immediate feedback
- **Question Management**: Admin panel for creating, editing, and managing quiz questions
- **User Authentication**: Secure login/registration with ASP.NET Core Identity
- **Role-Based Authorization**: Admin role for question management

### New Improvements
- **Global Error Handling**: Comprehensive error boundaries and user-friendly error messages
- **Notification System**: Toast notifications for user feedback and actions
- **Quiz Timer**: Configurable countdown timer with visual progress indicators
- **Leaderboard**: Track and display top quiz scores
- **Question Randomization**: Randomize questions for each quiz attempt
- **Loading States**: Smooth loading indicators throughout the application
- **Responsive Design**: Modern, mobile-friendly UI with Bootstrap 5
- **Health Checks**: Application monitoring endpoints
- **Structured Logging**: Comprehensive logging with Serilog
- **Strongly-Typed Configuration**: Type-safe application settings

## 🛠️ Technology Stack

- **.NET 9.0**: Latest .NET framework
- **Blazor Server**: Interactive web UI framework
- **ASP.NET Core Identity**: Authentication and authorization
- **Entity Framework Core**: Data access and migrations
- **SQL Server**: Database (LocalDB for development)
- **Bootstrap 5**: Responsive CSS framework
- **Bootstrap Icons**: Icon library
- **Serilog**: Structured logging

## 📋 Prerequisites

- .NET 9.0 SDK
- SQL Server LocalDB (included with Visual Studio)
- Visual Studio 2022 or VS Code

## 🚀 Getting Started

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd BlazorQuizApp
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run database migrations**
   ```bash
   dotnet ef database update
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access the application**
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - Register a new account or use the default admin account
   - Default admin email: `zakitworld@gmail.com` (configured in `appsettings.json`)

## 🔧 Configuration

### App Settings (`appsettings.json`)
```json
{
  "AppSettings": {
    "AdminUser": {
      "Email": "zakitworld@gmail.com"
    },
    "Quiz": {
      "DefaultTimeLimit": 300,
      "MaxQuestionsPerQuiz": 10,
      "RandomizeQuestions": true
    }
  }
}
```

### Health Check Endpoints
- `/health` - Overall application health
- `/health/ready` - Readiness check
- `/health/live` - Liveness check

## 📁 Project Structure

```
BlazorQuizApp/
├── Components/           # Blazor components
│   ├── Pages/           # Page components
│   ├── Layout/          # Layout components
│   └── Account/         # Authentication components
├── Data/                # Data access layer
├── Models/              # Data models
├── Services/            # Business logic services
├── Configuration/       # Configuration classes
└── wwwroot/            # Static assets
```

## 🔐 Security Features

- **Role-Based Authorization**: Admin-only access to question management
- **Input Validation**: Client and server-side validation
- **Secure Configuration**: Environment-based settings
- **Error Handling**: No sensitive data exposure in logs

## 🎨 User Experience

- **Responsive Design**: Works on all device sizes
- **Loading Indicators**: Visual feedback during operations
- **Toast Notifications**: User-friendly feedback messages
- **Error Boundaries**: Graceful error handling
- **Accessibility**: ARIA attributes and semantic HTML

## 📊 Monitoring

- **Health Checks**: Application status monitoring
- **Structured Logging**: Comprehensive application logs
- **Error Tracking**: Detailed error logging and reporting

## 🧪 Testing

The application includes:
- Input validation testing
- Error handling scenarios
- User flow testing
- Responsive design testing

## 🔄 Future Enhancements

- [ ] Unit and integration tests
- [ ] User quiz history
- [ ] Advanced analytics
- [ ] Quiz categories
- [ ] Export/import functionality
- [ ] Real-time multiplayer quizzes

## 📝 License

This project is licensed under the MIT License.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📞 Support

For support and questions, please open an issue in the repository. 