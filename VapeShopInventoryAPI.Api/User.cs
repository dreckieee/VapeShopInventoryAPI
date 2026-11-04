namespace VapeShopInventoryAPI.Api;

public class User
{
    private const int MaxUsernameLength = 30;
    
    public int Id { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }

    public User (string username, string passwordHash, UserRole role)
    {
        GuardUser(username, passwordHash, role);
        Username = username.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
    }
    private static void GuardUser(string username, string passwordHash, UserRole role)
    {
        if (username == null)
        {
            throw new ArgumentNullException(nameof(username), "Username cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be empty", nameof(username));
        }
        if (username.Trim().Length > MaxUsernameLength)
        {
            throw new ArgumentException($"Username cannot be longer than {MaxUsernameLength} characters.", nameof(username));
        }
        if (passwordHash == null)
        {
            throw new ArgumentNullException(nameof(passwordHash), "Password Hash cannot be null.");
        }
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password Hash cannot be empty", nameof(passwordHash));
        }
        if (!Enum.IsDefined(role))
        {
            string userRoles = string.Join(", ", Enum.GetNames<UserRole>());
            throw new ArgumentOutOfRangeException(nameof(role), $"Role provided for user is incorrect. Choose between: {userRoles}");
        }
    }
}