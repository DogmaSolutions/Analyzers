using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Guards the consistency of the resource file: every entry of Resources.resx has a typed accessor in the generated
/// Resources.Designer.cs (and vice versa), and no message is empty. A rule whose title, message or description is
/// missing would otherwise fail only at run time, in the IDE of the user.
/// </summary>
[TestClass]
public class ResourcesTests
{
    private static IReadOnlyDictionary<string, string> ResxEntries =>
        Resources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)
            .Cast<DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => (string)entry.Value);

    private static IReadOnlyDictionary<string, PropertyInfo> Accessors =>
        typeof(Resources).GetProperties(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
            .Where(property => property.PropertyType == typeof(string))
            .ToDictionary(property => property.Name);

    [TestMethod]
    public void Every_resx_entry_has_an_accessor()
    {
        var missing = ResxEntries.Keys.Except(Accessors.Keys).OrderBy(name => name).ToList();
        Assert.AreEqual(0, missing.Count, "Entries without accessor in Resources.Designer.cs: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void Every_accessor_has_a_resx_entry()
    {
        var orphans = Accessors.Keys.Except(ResxEntries.Keys).OrderBy(name => name).ToList();
        Assert.AreEqual(0, orphans.Count, "Accessors without entry in Resources.resx: " + string.Join(", ", orphans));
    }

    [TestMethod]
    public void Every_accessor_returns_its_entry_and_it_is_not_blank()
    {
        var blank = new List<string>();
        var different = new List<string>();

        foreach (var accessor in Accessors.Values.OrderBy(property => property.Name))
        {
            var value = (string)accessor.GetValue(null);
            if (string.IsNullOrWhiteSpace(value))
                blank.Add(accessor.Name);
            else if (ResxEntries.TryGetValue(accessor.Name, out var expected) && expected != value)
                different.Add(accessor.Name);
        }

        Assert.AreEqual(0, blank.Count, "Blank resources: " + string.Join(", ", blank));
        Assert.AreEqual(0, different.Count, "Accessors not returning their entry: " + string.Join(", ", different));
    }
}
