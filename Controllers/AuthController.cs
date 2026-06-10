using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PharmeasyAPI.Data;
using PharmeasyAPI.DTOs;
using PharmeasyAPI.Models;
using PharmeasyAPI.Services;

namespace PharmeasyAPI.Controllers;

/// <summary>OTP-based authentication for customers, admins, and doctors.</summary>
[ApiController]
[Route("auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;
    private readonly OtpService _otp;

    public AuthController(AppDbContext db, JwtService jwt, OtpService otp)
    {
        _db = db;
        _jwt = jwt;
        _otp = otp;
    }

    /// <summary>Send a one-time password to the given email address.</summary>
    /// <remarks>Creates a new user record if the email is not registered. OTP expires in 10 minutes.</remarks>
    /// <response code="200">OTP sent successfully.</response>
    [HttpPost("send-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest req)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user is null)
        {   
            user = new User { Email = req.Email, IsNew = true };
            _db.Users.Add(user);
        }

        var otpCode = _otp.GenerateOtp();
        user.OtpHash = _otp.HashOtp(otpCode);
        user.OtpExpiresAt = DateTime.UtcNow.AddMinutes(10);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Fire-and-forget: don't block the HTTP response waiting for SMTP.
        // OtpService already catches and logs any email failures internally.
        _ = _otp.SendOtpEmailAsync(req.Email, otpCode);

        return Ok(new { message = "OTP sent to your email." });
    }

    /// <summary>Verify an OTP and sign in as a Customer.</summary>
    /// <remarks>Returns a JWT token scoped to the Customer role on success.</remarks>
    /// <response code="200">JWT token and user info returned.</response>
    /// <response code="400">Invalid or expired OTP, or email not found.</response>
    [HttpPost("verify-otp/customer")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtpCustomer([FromBody] VerifyOtpRequest req)
        => await VerifyOtpForRole(req, UserRole.Customer);

    /// <summary>Verify an OTP and sign in as an Admin.</summary>
    /// <remarks>Returns a JWT token scoped to the Admin role on success.</remarks>
    /// <response code="200">JWT token and user info returned.</response>
    /// <response code="400">Invalid or expired OTP, or email not found.</response>
    [HttpPost("verify-otp/admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtpAdmin([FromBody] VerifyOtpRequest req)
        => await VerifyOtpForRole(req, UserRole.Admin);

    /// <summary>Verify an OTP and sign in as a Doctor.</summary>
    /// <remarks>Returns a JWT token scoped to the Doctor role on success.</remarks>
    /// <response code="200">JWT token and user info returned.</response>
    /// <response code="400">Invalid or expired OTP, or email not found.</response>
    [HttpPost("verify-otp/doctor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyOtpDoctor([FromBody] VerifyOtpRequest req)
        => await VerifyOtpForRole(req, UserRole.Doctor);

    private async Task<IActionResult> VerifyOtpForRole(VerifyOtpRequest req, UserRole role)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
        if (user is null)
            return BadRequest(new { message = "Email not found." });

        if (user.OtpHash is null || user.OtpExpiresAt is null)
            return BadRequest(new { message = "No OTP requested. Call /send-otp first." });

        if (DateTime.UtcNow > user.OtpExpiresAt)
            return BadRequest(new { message = "OTP has expired." });

        if (!_otp.VerifyOtp(req.Otp, user.OtpHash))
            return BadRequest(new { message = "Invalid OTP." });

        user.OtpHash = null;
        user.OtpExpiresAt = null;
        user.Role = role;
        user.UpdatedAt = DateTime.UtcNow;

        if (role == UserRole.Customer)
        {
            var profileExists = await _db.CustomerProfiles.AnyAsync(cp => cp.UserId == user.Id);
            if (!profileExists)
                _db.CustomerProfiles.Add(new CustomerProfile { UserId = user.Id });
        }

        await _db.SaveChangesAsync();

        var token = _jwt.GenerateToken(user.Id, user.Email, role.ToString());

        return Ok(new AuthResponse
        {
            User = ToDto(user),
            Token = token
        });
    }

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        Name = user.Name,
        Phone = user.Phone,
        Role = user.Role.ToString(),
        IsNew = user.IsNew,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}


