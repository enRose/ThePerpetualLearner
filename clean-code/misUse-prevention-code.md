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


// regex patterns organised not in conciseness but clarity.
// quite often when we are presented with a regex like disallow single quote
// we don't neccessary know what attack it is corelated to.
public static class FieldValidation
{
    public static string NumbersOnly = @"^[0-9]+$";
}

public static class XSS
{
    public static string DisalowAngleBrackets = @"^[^\<\>]*$";
}

public static class XPathAndSqlInjection
{
    public static string DisallowSingleQuote = @"^[^\']*$";

    public static string DisallowDoubleQuote = @"^[^""]*$";
}

public static class OSCommandInjection
{
    public static string DisallowSemicolon = @"^[^;]*$";
}

// If we know what regex should be applied to what attack,
// quite often we don't know where in the request/response flow
// should we apply to.
// We address this by specifying the location of application in policy,
public static class SecuirtyPolicy
{
    public static string[] Url()
    {
        return new string[]
        {
            XPathAndSqlInjection.DisallowDoubleQuote,
            XPathAndSqlInjection.DisallowSingleQuote,
            OSCommandInjection.DisallowSemicolon
        };
    }
}

public static class RegexRunner
{
    public static bool Check(string input, params string[] patterns)
    {
        return patterns.ToList().All(pattern => Regex.Match(input, pattern).Success);
    }
}
