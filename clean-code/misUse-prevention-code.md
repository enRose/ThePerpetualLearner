```
public static class RegexPattern
{
    public static string NumbersOnly = @"^[0-9]+$";


    // xss
    public static string DisalowAngleBrackets = @"^[^\<\>]*$";


    // XPath, sql injection - only used in url query string parameters
    public static string DisallowSingleQuote = @"^[^\']*$";

    public static string DisallowDoubleQuote = @"^[^""]*$";


    // OS command injection - only used in url query string parameters
    public static string DisallowSemicolon = @"^[^;]*$";
}
```
