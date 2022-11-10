// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices#naming-your-tests", Scope = "namespaceanddescendants", Target = "~N:Orleans.Results.Tests.UnitTests")]
[assembly: SuppressMessage("Style", "IDE0005:Using directive is unnecessary.", Justification = "This project includes shared source files that are also used outside the context of this project's global usings")]
