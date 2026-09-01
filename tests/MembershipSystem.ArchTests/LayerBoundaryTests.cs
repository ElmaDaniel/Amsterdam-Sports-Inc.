using NetArchTest.Rules;

namespace MembershipSystem.ArchTests;

/// <summary>
/// Enforces the dependency rules from docs/specs/membership-system.md
/// ("Architecture boundary guardrails"). A failing test here means a
/// layer boundary was violated — fix the reference, don't loosen the
/// rule.
/// </summary>
public class LayerBoundaryTests
{
    private const string DomainNamespace = "MembershipSystem.Domain";
    private const string UseCasesNamespace = "MembershipSystem.UseCases";
    private const string AdaptersNamespace = "MembershipSystem.Adapters";
    private const string ApiNamespace = "MembershipSystem.Api";

    [Fact]
    public void Domain_Should_Not_DependOn_UseCases()
    {
        var result = Types.InAssembly(typeof(Domain.Member).Assembly)
            .Should()
            .NotHaveDependencyOn(UseCasesNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Domain must not depend on UseCases. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_Should_Not_DependOn_Adapters()
    {
        var result = Types.InAssembly(typeof(Domain.Member).Assembly)
            .Should()
            .NotHaveDependencyOn(AdaptersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Domain must not depend on Adapters. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_Should_Not_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Domain.Member).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Domain must not depend on Api. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Domain_Should_Not_DependOn_Frameworks()
    {
        var result = Types.InAssembly(typeof(Domain.Member).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Domain must not depend on EF Core or ASP.NET Core. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void UseCases_Should_Not_DependOn_Adapters()
    {
        var result = Types.InAssembly(typeof(UseCases.MemberUseCases).Assembly)
            .Should()
            .NotHaveDependencyOn(AdaptersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "UseCases must not depend on Adapters (ports are declared inward, " +
            "implemented outward, never referenced directly). Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void UseCases_Should_Not_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(UseCases.MemberUseCases).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "UseCases must not depend on Api. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void UseCases_Should_Not_DependOn_Frameworks()
    {
        var result = Types.InAssembly(typeof(UseCases.MemberUseCases).Assembly)
            .Should()
            .NotHaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore")
            .GetResult();

        Assert.True(result.IsSuccessful,
            "UseCases must not depend on EF Core or ASP.NET Core request/response types. " +
            "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Adapters_Should_Not_DependOn_Api()
    {
        var result = Types.InAssembly(typeof(Adapters.EfMemberRepository).Assembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Adapters must not depend on Api. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }

    [Fact]
    public void Api_Should_Not_DependOn_Adapters_Concrete_Types_Outside_Composition_Root()
    {
        // The composition root (Program.cs / DI registration) is the one
        // place allowed to reference concrete adapters, to wire them
        // behind the ports UseCases declares. Everything else in Api
        // (controllers, etc.) must go through UseCases only.
        var result = Types.InAssembly(typeof(Program).Assembly)
            .That()
            .DoNotHaveName("Program")
            .Should()
            .NotHaveDependencyOn(AdaptersNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful,
            "Only the composition root (Program.cs) may reference Adapters directly; " +
            "controllers/endpoints must depend on UseCases only. Violating types: " +
            string.Join(", ", result.FailingTypeNames ?? []));
    }
}
