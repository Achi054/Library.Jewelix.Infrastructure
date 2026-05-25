using Microsoft.OpenApi;

namespace Jewelix.OpenApi.Tests;

public class BearerSecuritySchemeTransformerTests
{
    [Fact]
    public async Task TransformAsync_Always_AddsBearerKeyToSecuritySchemes()
    {
        var transformer = new BearerSecuritySchemeTransformer();
        var document = new OpenApiDocument { Info = new OpenApiInfo() };

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components.ShouldNotBeNull();
        document.Components.SecuritySchemes.ShouldContainKey("Bearer");
    }

    [Fact]
    public async Task TransformAsync_Always_SetsBearerSchemeTypeToHttp()
    {
        var transformer = new BearerSecuritySchemeTransformer();
        var document = new OpenApiDocument { Info = new OpenApiInfo() };

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components.SecuritySchemes["Bearer"].Type.ShouldBe(SecuritySchemeType.Http);
    }

    [Fact]
    public async Task TransformAsync_Always_SetsSchemeToBearerAndFormatToJwt()
    {
        var transformer = new BearerSecuritySchemeTransformer();
        var document = new OpenApiDocument { Info = new OpenApiInfo() };

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        var scheme = document.Components.SecuritySchemes["Bearer"];
        scheme.Scheme.ShouldBe("bearer");
        scheme.BearerFormat.ShouldBe("JWT");
    }

    [Fact]
    public async Task TransformAsync_CalledTwice_IsIdempotent()
    {
        var transformer = new BearerSecuritySchemeTransformer();
        var document = new OpenApiDocument { Info = new OpenApiInfo() };

        await transformer.TransformAsync(document, null!, CancellationToken.None);
        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components.SecuritySchemes.Count.ShouldBe(1);
    }

    [Fact]
    public async Task TransformAsync_WhenExistingSchemesPresent_PreservesThemAndAddsBearerScheme()
    {
        var transformer = new BearerSecuritySchemeTransformer();
        var document = new OpenApiDocument
        {
            Info = new OpenApiInfo(),
            Components = new OpenApiComponents
            {
                SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["ApiKey"] = new OpenApiSecurityScheme { Type = SecuritySchemeType.ApiKey }
                }
            }
        };

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        document.Components.SecuritySchemes.Count.ShouldBe(2);
        document.Components.SecuritySchemes.ShouldContainKey("ApiKey");
        document.Components.SecuritySchemes.ShouldContainKey("Bearer");
    }
}
