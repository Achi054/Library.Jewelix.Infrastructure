namespace Jewelix.OpenApi.Tests;

public class JewelixOpenApiOptionsTests
{
    [Fact]
    public void SectionName_IsOpenApi()
    {
        JewelixOpenApiOptions.SectionName.ShouldBe("OpenApi");
    }

    [Fact]
    public void DefaultOptions_ContainsSingleDocument()
    {
        var options = new JewelixOpenApiOptions();
        options.Documents.ShouldHaveSingleItem();
    }

    [Fact]
    public void DefaultDocument_Name_IsV1()
    {
        var doc = new JewelixOpenApiDocument();
        doc.Name.ShouldBe("v1");
    }

    [Fact]
    public void DefaultDocument_Title_IsApi()
    {
        var doc = new JewelixOpenApiDocument();
        doc.Title.ShouldBe("API");
    }

    [Fact]
    public void DefaultDocument_Version_Is1Point0()
    {
        var doc = new JewelixOpenApiDocument();
        doc.Version.ShouldBe("1.0");
    }

    [Fact]
    public void DefaultDocument_Description_IsNull()
    {
        var doc = new JewelixOpenApiDocument();
        doc.Description.ShouldBeNull();
    }

    [Fact]
    public void DefaultDocument_EnableBearerAuth_IsFalse()
    {
        var doc = new JewelixOpenApiDocument();
        doc.EnableBearerAuth.ShouldBeFalse();
    }

    [Fact]
    public void DefaultDocument_ScalarRoutePrefix_IsScalar()
    {
        var doc = new JewelixOpenApiDocument();
        doc.ScalarRoutePrefix.ShouldBe("scalar");
    }
}
