namespace Client.Extensions;

public static class StringExtensions
{
    extension(string str)
    {
        public static bool IsNullOrWhiteSpaces(params string?[] strings) => strings.Any(string.IsNullOrWhiteSpace);
    }
}