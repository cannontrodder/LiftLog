using System.Security.Cryptography.X509Certificates;
using FluentValidation;
using LiftLog.Api.Db;
using LiftLog.Api.Models;
using LiftLog.Api.Service;
using LiftLog.Lib.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LiftLog.Api.Controllers;

[ApiController]
public class UserController(
    UserDataContext db,
    PasswordService passwordService,
    IdEncodingService idEncodingService
) : ControllerBase
{
    [Route("[controller]/create")]
    [HttpPost]
    public async Task<IActionResult> CreateUser(
        CreateUserRequest request,
        [FromServices] IValidator<CreateUserRequest> validator
    )
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        if (await db.Users.AnyAsync(x => x.Id == request.Id))
        {
            return BadRequest(new string[] { "User already exists" });
        }

        var password = Guid.NewGuid().ToString();
        var hashedPassword = passwordService.HashPassword(password, out var salt);
        var user = new User
        {
            Id = request.Id,
            HashedPassword = hashedPassword,
            Salt = salt,
            LastAccessed = DateTimeOffset.UtcNow,
            EncryptionIV = [],
            RsaPublicKey = []
        };

        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
        return Ok(new CreateUserResponse(password));
    }

    [Route("[controller]/{id}")]
    [HttpGet]
    public async Task<IActionResult> GetUser(string idOrLookup)
    {
        User? user;
        if (Guid.TryParse(idOrLookup, out var id))
        {
            user = await db.Users.FindAsync(id);
        }
        else
        {
            var userNumber = idEncodingService.DecodeId(idOrLookup);
            user = await db.Users.FirstOrDefaultAsync(x => x.UserNumber == userNumber);
        }
        if (user == null)
        {
            return NotFound();
        }

        user.LastAccessed = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(
            new GetUserResponse(
                Id: user.Id,
                Lookup: idEncodingService.EncodeId(user.UserNumber),
                EncryptedCurrentPlan: user.EncryptedCurrentPlan,
                EncryptedProfilePicture: user.EncryptedProfilePicture,
                EncryptedName: user.EncryptedName,
                EncryptionIV: user.EncryptionIV,
                RsaPublicKey: user.RsaPublicKey
            )
        );
    }

    [Route("[controller]/delete")]
    [HttpPost]
    public async Task<IActionResult> DeleteUser(
        DeleteUserRequest request,
        [FromServices] IValidator<DeleteUserRequest> validator
    )
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var user = await db.Users.FindAsync(request.Id);
        if (user == null)
        {
            return NotFound();
        }

        if (!passwordService.VerifyPassword(request.Password, user.HashedPassword, user.Salt))
        {
            return Unauthorized();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut]
    [Route("[controller]")]
    public async Task<IActionResult> PutUser(
        PutUserDataRequest request,
        [FromServices] IValidator<PutUserDataRequest> validator
    )
    {
        var validationResult = await validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors);
        }

        var user = await db.Users.FindAsync(request.Id);
        if (user == null)
        {
            return NotFound();
        }

        if (!passwordService.VerifyPassword(request.Password, user.HashedPassword, user.Salt))
        {
            return Unauthorized();
        }

        user.EncryptedCurrentPlan = request.EncryptedCurrentPlan;
        user.EncryptedProfilePicture = request.EncryptedProfilePicture;
        user.EncryptedName = request.EncryptedName;
        user.EncryptionIV = request.EncryptionIV;
        user.RsaPublicKey = request.RsaPublicKey;
        await db.SaveChangesAsync();
        return Ok();
    }
}
