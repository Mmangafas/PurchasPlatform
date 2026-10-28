namespace SmartPricing.Web.Services;

public static class PasswordPolicy
{
    public const int MinLength = 10;

    /// <summary>Returns an error message, or null when the password is acceptable.</summary>
    public static string? Validate(string password)
    {
        if (password.Length < MinLength)
            return $"Τουλάχιστον {MinLength} χαρακτήρες. / At least {MinLength} characters.";
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            return "Απαιτούνται κεφαλαία, πεζά και αριθμοί. / Use upper case, lower case and digits.";
        return null;
    }
}
