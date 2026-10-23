using System;
using System.Collections.Generic;
using System.Text;


namespace Model
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public bool IsActive { get; set;  }

        // נשלף מטבלת Roles באמצעות JOIN
        public string RoleName { get; set; } = string.Empty;

        public User() { }

        public User(string username, string email, string passwordHash)
        {
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
        }

        public User(
            int id,
            string username,
            string email,
            string passwordHash,
            int roleId,
            string roleName, bool isActive) : base(id)
        {
            Username = username;
            Email = email;
            PasswordHash = passwordHash;
            RoleId = roleId;
            RoleName = roleName;
            IsActive = isActive;
        }

        public override string ToString() => $"{Username} ({RoleName})";
    }
}
