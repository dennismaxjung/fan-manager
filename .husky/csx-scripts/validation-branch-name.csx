using System.Text.RegularExpressions;

private var pattern01 = @"^(bugfix|feature|impediment)/([0-9]{5,})_([a-z0-9-]+)$";
private var pattern02 = @"^develop$";
private var branchName = Args[0];

if (Regex.IsMatch(branchName, pattern01) || Regex.IsMatch(branchName,pattern02))
  return 0;

Console.WriteLine("Invalid branch name");
Console.WriteLine("e.g: 'bugfix/12345_test-branch'");

return 1;
