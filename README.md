# Sugo Backend API

A full ASP.NET Core Web API backend for the Sugo application (live rooms / social task app).

## 🚀 Features

- **User Authentication**: Registration and JWT-based login
- **User Management**: Get user profile information
- **Chat Rooms**: Create and manage chat rooms
- **Wallet**: Per-user wallet with balance and top-up
- **Gifts Store**: List gifts, send gifts to rooms or users, room ranking
- **Matching**: Recommend a room based on activity
- **Moderation**: User/room reports and admin review
- **Real-Time**: SignalR hub for room events and gift notifications
- **Database**: SQL Server (production) with Entity Framework Core
- **API Documentation**: Swagger UI for easy testing
- **Security**: JWT Bearer token authentication
- **CORS**: Enabled for frontend integration

## 📋 Prerequisites

- **.NET 8.0 SDK** or later
- **Visual Studio Code** or **Visual Studio** (optional)
- **Postman** for API testing (optional but recommended)

## 🔧 Installation & Setup

### 1. Restore Dependencies
```bash
dotnet restore
```

### 2. Apply Database Migrations
```bash
dotnet ef database update
```

This will create the SQLite database (`sugo.db`) with the User and Room tables.

### 3. Run the Application (Local)
```bash
dotnet run
```

The API will start on `http://localhost:5000` (see launchSettings.json)

### 4. Production URL

The deployed API (for frontend/mobile use) is available at:

`http://sugobackend.runasp.net`

## 📚 API Endpoints

### Authentication (`/api/auth`)

#### Register User
- **POST** `/api/auth/register`
- **Body**:
  ```json
  {
    "username": "john_doe",
    "email": "john.doe@example.com",
    "password": "SecurePassword123!"
  }
  ```
- **Response**: User ID and success message

#### Login User
- **POST** `/api/auth/login`
- **Body**:
  ```json
  {
    "email": "john.doe@example.com",
    "password": "SecurePassword123!"
  }
  ```
- **Response**: JWT token, user info
  ```json
  {
    "userId": 1,
    "username": "john_doe",
    "email": "john.doe@example.com",
    "token": "eyJhbGciOiJIUzI1NiIs..."
  }
  ```

### Users (`/api/users`) - *Requires Authentication*

#### Get User Profile
- **GET** `/api/users/profile/{id}`
- **Headers**: `Authorization: Bearer {token}`
- **Response**: User profile details

### Rooms (`/api/rooms`) - *Requires Authentication*

#### Create Room
- **POST** `/api/rooms/create`
- **Headers**: `Authorization: Bearer {token}`
- **Body**:
  ```json
  {
    "name": "General Discussion"
  }
  ```
- **Response**: Created room details with ID

#### List All Rooms
- **GET** `/api/rooms/list`
- **Headers**: `Authorization: Bearer {token}`
- **Response**: Array of all available rooms

## 🧪 Testing with Swagger UI

### Local
1. Start the application: `dotnet run`
2. Open browser and navigate to: `http://localhost:5000/swagger`
3. Use the interactive Swagger UI to test endpoints
4. For protected endpoints, click the "Authorize" button and paste your JWT token

### Production
1. Open browser and navigate to: `http://sugobackend.runasp.net/swagger`
2. Use the same flow as local for testing and authorization

## 📤 Testing with Postman

1. Import the provided `SugoBackend.postman_collection.json` into Postman
2. Ensure the collection variable `{{baseUrl}}` is set to the appropriate environment:
   - Default: `http://sugobackend.runasp.net` (production)
   - Local: `http://localhost:5000` if running the API locally
3. Execute the `Login` request to get a JWT token
4. The Postman script will automatically set:
   - `{{token}}`, `{{userId}}`, `{{username}}`, `{{email}}`
5. All protected endpoints are pre-configured with bearer token headers

### Postman Variables
- `{{baseUrl}}`: Base API URL (local or production)
- `{{token}}`: JWT token (set after login)
- `{{userId}}`: User ID (set after registration/login)
- `{{roomId}}`: Room ID (set after creating a room)

## 🗂️ Project Structure

```
SugoBackend/
├── Controllers/
│   ├── AuthController.cs       # Authentication endpoints
│   ├── UsersController.cs      # User management endpoints
│   └── RoomsController.cs      # Room management endpoints
├── Models/
│   ├── User.cs                 # User entity
│   └── Room.cs                 # Room entity
├── DTOs/
│   ├── RegisterDto.cs          # Registration request DTO
│   ├── LoginDto.cs             # Login request DTO
│   ├── RoomDto.cs              # Room response DTO
│   ├── UserProfileDto.cs       # User profile response DTO
│   └── LoginResponseDto.cs     # Login response DTO
├── Data/
│   └── AppDbContext.cs         # Entity Framework DbContext
├── Services/
│   └── TokenService.cs         # JWT token generation service
├── Program.cs                  # Application startup & configuration
├── appsettings.json            # Configuration file
├── appsettings.Development.json # Development configuration
├── SugoBackend.csproj          # Project file
└── SugoBackend.postman_collection.json  # Postman API collection
```

## 🔐 Authentication Flow

1. **Register**: Create a new user account via `/api/auth/register`
2. **Login**: Authenticate with email/password via `/api/auth/login`
3. **Get Token**: Receive JWT token in login response
4. **Use Token**: Include token in `Authorization: Bearer {token}` header for protected endpoints

## 📝 Configuration

Edit `appsettings.json` to customize:

- **Connection String**: Database location
- **JWT Settings**:
  - `Key`: Secret key for signing tokens (⚠️ Change in production!)
  - `Issuer`: Token issuer
  - `Audience`: Token audience
  - `ExpiryInMinutes`: Token expiration time

## ⚠️ Important Notes

- **Security**: The JWT secret key in `appsettings.json` is for development only. Use a secure key in production.
- **Password Hashing**: Uses SHA256 hashing (basic implementation for demo purposes)
- **Database**: SQLite database file (`sugo.db`) is created automatically in the project root
- **CORS**: Currently allows all origins. Configure in `Program.cs` for production

## 🐛 Troubleshooting

### Port Already in Use
If port 5001 is busy, modify the port in `launchSettings.json`:
```json
"profiles": {
  "https": {
    "commandName": "Project",
    "applicationUrl": "https://localhost:YOUR_PORT"
  }
}
```

### Database Issues
Delete `sugo.db` and run migrations again:
```bash
dotnet ef database update
```

### JWT Token Errors
- Ensure the token is passed with `Bearer ` prefix
- Check token expiration in appsettings.json
- Verify JWT secret key is consistent across the project

## 📦 Deliverable Checklist

✅ Complete ASP.NET Core Web API project  
✅ SQLite database with EF Core  
✅ JWT authentication & authorization  
✅ Swagger UI documentation  
✅ CORS enabled  
✅ Clean project structure with regions & comments  
✅ Postman collection included  
✅ Input validation on all endpoints  
✅ Dependency injection properly configured  

## 📞 Support

For issues or questions, refer to the endpoint documentation in Swagger UI or review the controller implementations.

---

**Ready to deploy?** Remember to:
1. Change JWT secret key in `appsettings.json`
2. Update CORS policy for production domains
3. Use HTTPS in production
4. Consider using a production database (SQL Server, PostgreSQL, etc.)
