using System.Text.RegularExpressions;

namespace Client.Pages.Signup;

// BACKEND NOTE: Username must be unique. The client cannot check this (no DB access from the
// browser) — the server MUST verify the username isn't already taken before creating the account,
// and reject the request if it is.
//
// BACKEND NOTE: Email is the permanent account key. All account lookup/identification (login,
// password reset, support, etc.) goes through the email address. Once an account is created, the
// email must NOT be editable — this is a deliberate, permanent design decision, not a missing
// feature to add later.
public static class SignupValidation
{
    public const string ForbiddenPasswordCharactersDisplay = "- _ @ \" ' < > % & + =";
    private const string ForbiddenPasswordCharacters = "-_@\"'<>%&+=";

    public static bool IsValidEmailFormat(string email)
    {
        // Minimal check only, by design: a non-empty string containing '@'.
        // Real format/deliverability verification is intentionally out of scope for now.
        return !string.IsNullOrWhiteSpace(email) && email.Contains('@');
    }

    // Shared with Profile.razor's phone field editor, so both places agree on what counts as
    // a valid phone number.
    public static bool IsValidPhoneFormat(string phone) =>
        Regex.IsMatch(phone.Trim(), @"^[0-9+()\-\s]{7,}$");

    public static IReadOnlyList<(string Description, bool Satisfied)> GetPasswordRequirements(string password)
    {
        return new (string Description, bool Satisfied)[]
        {
            ("At least 8 characters.", password.Length >= 8),
            ("At least one uppercase letter.", password.Any(char.IsUpper)),
            ("At least one lowercase letter.", password.Any(char.IsLower)),
            ("At least one number.", password.Any(char.IsDigit)),
            ("At least one punctuation mark.", password.Any(IsAllowedPunctuation)),
            ($"No forbidden characters: {ForbiddenPasswordCharactersDisplay}", !ContainsForbiddenCharacters(password)),
        };
    }

    public static bool ContainsForbiddenCharacters(string password) =>
        password.Any(c => ForbiddenPasswordCharacters.Contains(c));

    private static bool IsAllowedPunctuation(char c) =>
        (char.IsPunctuation(c) || char.IsSymbol(c)) && !ForbiddenPasswordCharacters.Contains(c);
}
