using System;
using static System.DateTime;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA004;

public class MyClassWithMatchesUsingStaticImport
{
    public DateTime Now1()
    {
        return Now;
    }
}