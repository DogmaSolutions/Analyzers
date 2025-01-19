using static System.String;

namespace DogmaSolutions.Analyzers.MockLibrary.DSA003;

public class MyClassUsingStaticImport
{
    public bool IsNullOrEmpty1(string s)
    {
        return IsNullOrEmpty(s);
    }
}