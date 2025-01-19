using static global::System.String;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA003;

public class MyClassWithMatchesUsingGlobalStaticImport
{
    public bool IsNullOrEmpty1(string s)
    {
        return IsNullOrEmpty(s);
    }
}