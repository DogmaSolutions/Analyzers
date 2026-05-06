using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA024ParameterMatchTests
{
    private static readonly string[] ExactNames = { "path" };

    private static readonly string[] PrefixSuffixNames =
    {
        "filePath", "fileName", "directoryPath", "directoryName",
        "folderPath", "folderName", "fileFullPath", "directoryFullPath",
        "fileFullName", "directoryFullName", "xmlFile", "xmlFilePath",
        "xmlFileName", "jsonFile", "jsonFileName", "jsonFilePath",
    };

    [TestMethod]
    public void ExactMatch_Path() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("path", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void ExactMatch_CaseInsensitive_PATH() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("PATH", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void ExactMatch_CaseInsensitive_Path() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("Path", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void PrefixMatch_filePath() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("filePathOverride", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void SuffixMatch_outputFilePath() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("outputFilePath", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void PrefixMatch_fileName() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("fileNameBase", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void SuffixMatch_sourceFileName() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("sourceFileName", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void PrefixMatch_directoryPath() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("directoryPathPrefix", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void SuffixMatch_targetDirectoryPath() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("targetDirectoryPath", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void PrefixMatch_folderPath() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("folderPath", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void SuffixMatch_outputFolderName() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("outputFolderName", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void PrefixMatch_xmlFile() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("xmlFileSource", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void SuffixMatch_configXmlFile() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("configXmlFile", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void PrefixMatch_jsonFilePath() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("jsonFilePathOverride", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void SuffixMatch_outputJsonFilePath() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("outputJsonFilePath", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void CaseInsensitive_PrefixSuffix_FILEPATH() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("FILEPATH", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void CaseInsensitive_PrefixSuffix_OutputFILEPATH() =>
        Assert.IsTrue(DSA024Analyzer.IsMatchingParameter("OutputFILEPATH", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void NoMatch_data() =>
        Assert.IsFalse(DSA024Analyzer.IsMatchingParameter("data", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void NoMatch_content() =>
        Assert.IsFalse(DSA024Analyzer.IsMatchingParameter("content", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void NoMatch_message() =>
        Assert.IsFalse(DSA024Analyzer.IsMatchingParameter("message", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void NoMatch_url() =>
        Assert.IsFalse(DSA024Analyzer.IsMatchingParameter("url", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void NoMatch_bufferSize() =>
        Assert.IsFalse(DSA024Analyzer.IsMatchingParameter("bufferSize", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void NoMatch_pathLikeButNotExact_mypath() =>
        Assert.IsFalse(DSA024Analyzer.IsMatchingParameter("mypath", ExactNames, PrefixSuffixNames));

    [TestMethod]
    public void NoMatch_pathLikeButNotExact_pathname() =>
        Assert.IsFalse(DSA024Analyzer.IsMatchingParameter("pathname", ExactNames, PrefixSuffixNames));
}
