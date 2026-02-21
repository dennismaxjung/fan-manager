/// <summary>
/// A simple regex commit linter example
/// https://www.conventionalcommits.org/en/v1.0.0/
/// </summary>

using System.Text.RegularExpressions;

private var pattern = @"^(?=.{1,100}$)(?:build|feat|ci|chore|docs|fix|perf|refactor|revert|style|test)(?:\(.+\))*(?::).{4,}(?:#\d+)*(?<![\.\s])$";
private var msg = File.ReadAllLines(Args[0])[0];

if (Regex.IsMatch(msg, pattern))
  return 0;

Console.WriteLine("Invalid commit message");
Console.WriteLine("e.g: 'feat(scope): subject' or 'feat: subject'");
Console.WriteLine("more info: https://www.conventionalcommits.org/en/v1.0.0/");

if (msg.Length > 100)
{
  Console.WriteLine($"Commit message is to long! \r\n\t allowed: 100\r\n\t length: {msg.Length}");
}

return 1;
