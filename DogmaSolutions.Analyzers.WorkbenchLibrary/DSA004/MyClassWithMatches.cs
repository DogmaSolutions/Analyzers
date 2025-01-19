using System;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA004;

public class MyClassWithMatches
{
    public DateTime Now1()
    {
        return DateTime.Now;
    }

    public DateTime Now2()
    {
        return DateTime.Now;
    }

    public DateTime Now3()
    {
        return System.DateTime.Now;
    }
}