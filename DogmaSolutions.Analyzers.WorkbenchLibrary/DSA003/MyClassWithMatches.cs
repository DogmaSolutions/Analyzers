using System;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA003;

public class MyClassWithMatches
{
    public bool IsNullOrEmpty1(string s)
    {
        return string.IsNullOrEmpty(s);
    }

    public bool IsNullOrEmpty2(string s)
    {
        return String.IsNullOrEmpty(s);
    }

    public bool IsNullOrEmpty3(string s)
    {
        return System.String.IsNullOrEmpty(s);
    }

    public bool IsNullOrEmpty4(string s)
    {
        return global::System.String.IsNullOrEmpty(s);
    }
}