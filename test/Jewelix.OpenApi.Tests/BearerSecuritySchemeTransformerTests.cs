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

        // Capture the non-null result so the compiler knows Components is non-null below.
        var components = document.Components.ShouldNotBeNull();
        components.SecuritySchemes.ShouldContainKey("Bearer");
    }

    [Fact]
    public async Task TransformAsync_Always_SetsBearerSchemeTypeToHttp()
    {
        var transformer = new BearerSecuritySchemeTransformer();
        var document = new OpenApiDocument { Info = new OpenApiInfo() };

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        var scheme = document.Components.ShouldNotBeNull().SecuritySchemes["Bearer"];
        scheme.Type.ShouldBe(SecuritySchemeType.Http);
    }

    [Fact]
    public async Task TransformAsync_Always_SetsSchemeToBearerAndFormatToJwt()
    {
        var transformer = new BearerSecuritySchemeTransformer();
        var document = new OpenApiDocument { Info = new OpenApiInfo() };

        await transformer.TransformAsync(document, null!, CancellationToken.None);

        var scheme = document.Components.ShouldNotBeNull().SecuritySchemes["Bearer"];
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

        document.Components.ShouldNotBeNull().SecuritySchemes.ShouldNotBeNull().Count.ShouldBe(1);
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

        var schemes = document.Components.ShouldNotBeNull().SecuritySchemes.ShouldNotBeNull();
        schemes.Count.ShouldBe(2);
        schemes.ShouldContainKey("ApiKey");
        schemes.ShouldContainKey("Bearer");
    }
}
