﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PassLockerDatabase;
using PassLocker.Services;
using PassLocker.Services.Protector;
using PassLockerDatabase.Google;

namespace PassLocker.Controllers
{
    [Route("api/[Controller]")]
    public class UserController : ControllerBase
    {
        private PassLockerDbContext db;
        private readonly IProtector protector;
        public UserController(PassLockerDbContext injectContext, IProtector protector)
        {
            this.db = injectContext;
            this.protector = protector;
        }
        
        // GET: api/user
        [HttpGet]
        [ProducesResponseType(404)]
        public IActionResult Get()
        {
            return BadRequest("Inaccessible url");
        }
        

        // GET: api/user/{id}
        [HttpGet("{id:int}")]
        [ProducesResponseType(200, Type = typeof(UserViewDTO))]
        [ProducesResponseType(404)]
        public async Task<ActionResult<UserViewDTO>> GetUser(int id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("User does not exists");
            }
            return UserToDto(user);
        }

        // POST: api/user/create-user
        [HttpPost("create-user")]
        [ProducesResponseType(201, Type = typeof(BasicUserProfile))]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CreateUser([FromBody] BasicUserProfile user)
        {
            if (user == null)
            {
                return BadRequest("Invalid request content. User's info is invalid");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // user = protector.CreateHashedPassword(user);

            var newUser = GoogleUserToDatabaseDto(user);
            
            EntityEntry<User> added = await db.Users.AddAsync(newUser);
            int affected = await db.SaveChangesAsync();
            if (affected == 1)
            {
                return Created(nameof(GetUser), UserToDto(newUser));
            }
            else
            {
                return Problem("Some problem at the server. Cannot create new user.");
            }
        }

        [HttpPut("{id:int}/edit-profile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User user)
        {
            if (user == null || user.UserId != id)
            {
                return BadRequest("Invalid request content. User's info is invalid");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            user = protector.CreateHashedPassword(user);

            User new_user = UserToDatabaseDto(user);

            db.Users.Update(new_user);
            int affected = await db.SaveChangesAsync();
            if (affected == 1)
            {
                return NoContent();
            }
            else {
                return NotFound("User could not be found in database");
            }
        }

        [HttpDelete("{id:int}/delete-profile")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            User user = await db.Users.FindAsync(id);
            if (user == null)
            {
                return BadRequest("User does not exists");
            }
            db.Users.Remove(user);
            int affected = await db.SaveChangesAsync();
            if (affected == 1)
            {
                return NoContent();
            }
            else
            {
                return NotFound("User could not be found in database");
            }
        }

        private static UserViewDTO UserToDto(User user) =>
            new UserViewDTO
            {
                UserId = user.UserId,
                UserEmail = user.UserEmail,
                UserName = user.UserName,
                Confirmed = user.UserConfirmed,
                Name = user.Name,
                Gender = user.Gender,
                MemberSince = user.MemberSince
            };
        
        private static User UserToDatabaseDto(User user) =>
            new User
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserEmail = user.UserEmail,
                UserPasswordSalt = user.UserPasswordSalt,
                UserPasswordHash = user.UserPasswordHash,
                UserSecretAnswerHash = user.UserSecretAnswerHash,
                UserConfirmed = user.UserConfirmed,
                Name = user.Name,
                Location = user.Location,
                Gender = user.Gender,
                MemberSince = user.MemberSince,
                StoredPasswords = user.StoredPasswords
            };

        private static User GoogleUserToDatabaseDto(BasicUserProfile user) =>
            new User
            {
                UserName = user.Username,
                UserEmail = user.Email,
                UserPasswordSalt = "randomPasswordSalt",
                UserPasswordHash = "randomPasswordHash",
                UserSecretAnswerHash = "randomUserSecretAnswerHash",
                UserConfirmed = true,
                Name = user.Name,
                Location = user.Location,
                Gender = user.Gender,
                MemberSince = new DateTime().Date,
                StoredPasswords = new List<UserPasswords>()
            };
    }
}
