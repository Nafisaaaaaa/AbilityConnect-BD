using BCrypt.Net;
using System.Text.RegularExpressions;

namespace SDP1.Helpers
{
    public static class PasswordHelper
    {
       
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

      
        public static bool VerifyPassword(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

       
        public static bool IsStrongPassword(string password, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrEmpty(password) || password.Length < 8)
            {
                errorMessage = "Password must be at least 8 characters long.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                errorMessage = "Password must contain at least one uppercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                errorMessage = "Password must contain at least one lowercase letter.";
                return false;
            }

            if (!Regex.IsMatch(password, @"\d"))
            {
                errorMessage = "Password must contain at least one number.";
                return false;
            }

            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>_\-+=;'\[\]\\/~`]"))
            {
                errorMessage = "Password must contain at least one special character (!@#$%^&*).";
                return false;
            }

            return true;
        }

     
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;

            
            if (email.Any(char.IsUpper)) return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email && email.Contains('.') && email.Contains('@');
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidFullName(string name, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                errorMessage = "Name is required.";
                return false;
            }

            var words = name.Trim().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (words.Length < 1)
            {
                errorMessage = "Name is required.";
                return false;
            }

            foreach (var word in words)
            {
                if (!Regex.IsMatch(word, @"^[a-zA-Z]+$"))
                {
                    errorMessage = "Name can only contain letters and spaces (no numbers or special characters).";
                    return false;
                }

                if (word.Length < 3)
                {
                    errorMessage = "Each name must be at least 3 letters long.";
                    return false;
                }
            }

            if (name.Replace(" ", "").Length < 3)
            {
                errorMessage = "Name must be at least 3 letters long.";
                return false;
            }

            return true;
        }
    }
}