namespace AncientServer.Services
{
    public static class PasswordService
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
    }
}