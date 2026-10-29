using IdentityService.Common;
using IdentityService.Data;
using IdentityService.DTO;
using IdentityService.Entities;
using IdentityService.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Services;

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IdentityDbContext context,
        IPasswordHasher<User> passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var normalizedUsername = request.Username.Trim();

        // 1. Guard against Swagger default dummy values
        if (string.Equals(normalizedUsername, "string", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalizedEmail, "user@example.com", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(request.Password, "string", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppException("Default placeholder values (like 'string' or 'user@example.com') cannot be registered. Please provide real account details.", 400);
        }

        // 2. Validate password complexity
        if (request.Password.Length < 8 ||
            !request.Password.Any(char.IsUpper) ||
            !request.Password.Any(char.IsLower) ||
            !request.Password.Any(char.IsDigit) ||
            !request.Password.Any(ch => !char.IsLetterOrDigit(ch)))
        {
            throw new AppException("Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.", 400);
        }

        // 3. Check for duplicates
        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new AppException("A user with this email address already exists.", 400);
        }

        if (await _context.Users.AnyAsync(u => u.Username == normalizedUsername, cancellationToken))
        {
            throw new AppException("A user with this username already exists.", 400);
        }

        // 4. Role assignment (defaults to Customer, or accepts Admin/Customer)
        var assignedRole = Roles.Customer;
        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var requestedRole = request.Role.Trim();
            if (string.Equals(requestedRole, Roles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                assignedRole = Roles.Admin;
            }
            else if (string.Equals(requestedRole, Roles.Customer, StringComparison.OrdinalIgnoreCase))
            {
                assignedRole = Roles.Customer;
            }
            else
            {
                throw new AppException($"Invalid role '{request.Role}'. Allowed roles are 'Admin' or 'Customer'.", 400);
            }
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = normalizedUsername,
            Email = normalizedEmail,
            Role = assignedRole,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("New user registered successfully: {UserId}, Email: {Email}, Role: {Role}", user.Id, user.Email, user.Role);

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        var response = new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            User = MapToDto(user)
        };

        return ApiResponse<AuthResponse>.Ok(response, "User registered successfully.");
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Login failed: User not found with email {Email}", normalizedEmail);
            throw new AppException("Invalid email or password.", 401);
        }

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: Incorrect password for user {UserId}", user.Id);
            throw new AppException("Invalid email or password.", 401);
        }

        _logger.LogInformation("User logged in successfully: {UserId}, Role: {Role}", user.Id, user.Role);

        var (token, expiresAt) = _jwtTokenGenerator.GenerateToken(user);

        var response = new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAt,
            User = MapToDto(user)
        };

        return ApiResponse<AuthResponse>.Ok(response, "Login successful.");
    }

    public async Task<ApiResponse<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user == null)
        {
            throw new NotFoundException($"User with ID {userId} was not found.");
        }

        return ApiResponse<UserDto>.Ok(MapToDto(user));
    }

    private static UserDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        CreatedAtUtc = user.CreatedAtUtc
    };
}
