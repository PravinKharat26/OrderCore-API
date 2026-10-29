using System.ComponentModel.DataAnnotations;

namespace IdentityService.DTO;

public class RegisterRequest
{
<<<<<<< Updated upstream
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
=======
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(30, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 30 characters.")]
    [RegularExpression(@"^(?!string$)(?!admin$)[a-zA-Z0-9_\-]+$", ErrorMessage = "Username cannot be 'string' or 'admin' and must contain only alphanumeric characters, underscores, or hyphens.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$", 
        ErrorMessage = "Password must be at least 8 characters and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.")]
>>>>>>> Stashed changes
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password.")]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
<<<<<<< Updated upstream
    public string? ConfirmPassword { get; set; }

    // Optional: Defaults to "Customer" if not passed. Allowed values: "Admin", "Customer"
=======
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Role to assign upon registration. Allowed values: "Admin", "Customer".
    /// Defaults to "Customer" if omitted.
    /// </summary>
    [RegularExpression("^(Admin|Customer)$", ErrorMessage = "Role must be either 'Admin' or 'Customer'.")]
>>>>>>> Stashed changes
    public string? Role { get; set; }
}

public class LoginRequest
{
<<<<<<< Updated upstream
    [Required]
    [EmailAddress]
=======
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
>>>>>>> Stashed changes
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public UserDto User { get; set; } = default!;
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
