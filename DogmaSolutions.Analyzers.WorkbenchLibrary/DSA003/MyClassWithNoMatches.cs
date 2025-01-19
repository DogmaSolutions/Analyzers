using System;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA003;

public class MyClassWithNoMatches
{
    public bool IsNullOrWhiteSpace1(string s)
    {
        return string.IsNullOrWhiteSpace(s);
    }

    public bool IsNullOrWhiteSpace2(string s)
    {
        return String.IsNullOrWhiteSpace(s);
    }

    public bool IsNullOrWhiteSpace3(string s)
    {
        return System.String.IsNullOrWhiteSpace(s);
    }

    public bool IsNullOrWhiteSpace4(string s)
    {
        return global::System.String.IsNullOrWhiteSpace(s);
    }
}