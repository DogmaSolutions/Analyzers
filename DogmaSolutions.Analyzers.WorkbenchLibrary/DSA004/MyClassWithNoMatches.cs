using System;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA004;

public class MyClassWithNoMatches
{
    public long Id { get; set; }

    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }

    public DateTime Now()
    {
        return DateTime.UtcNow;
    }

    public DateTime UtcNow3()
    {
        return System.DateTime.UtcNow;
    }
}