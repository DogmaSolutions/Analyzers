using System;
using System;
using static global::System.DateTime;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA004;

public class MyClassWithMatchesUsingGlobalStaticImport
{
    public DateTime Now1()
    {
        return Now;
    }
}