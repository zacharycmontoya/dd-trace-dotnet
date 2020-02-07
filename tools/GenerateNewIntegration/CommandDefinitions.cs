using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Text;

namespace GenerateNewIntegration
{
    public static class CommandDefinitions
    {
        internal static Command AddMethod { get; } =
            new Command("add-method")
            {
                new Option(
                    "--override-method-name",
                    "Override the generated method name. If undefined, the generated method name will match the name of the target method")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    "--caller-assembly",
                    "The name of the assembly where calls to the target method are searched")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    "--caller-type",
                    "The namespace-qualified name of the type where calls to the target method are searched")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    "--caller-method",
                    "The name of the method where calls to the target method are searched")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    "--target-assembly",
                    "The name of the assembly that contains the target method to be intercepted. Required if TargetAssemblies is not set")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    "--target-assemblies",
                    "The name of the assemblies that contain the target method to be intercepted. Required if TargetAssembly is not set")
                {
                    Argument = new Argument<string[]>(),
                    Required = true
                },
                new Option(
                    "--target-type",
                    "The namespace-qualified name of the type that contains the target method to be intercepted")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
                new Option(
                    "--target-method",
                    "The name of the target method to be intercepted")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
                new Option(
                    "--target-method-is-static",
                    "The name of the target method to be intercepted")
                {
                    Argument = new Argument<bool>(),
                    Required = true
                },
                new Option(
                    "--target-minimum-version",
                    "The minimum version of the containing assembly")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    "--target-maximum-version",
                    "The maximum version of the containing assembly")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    "--target-signature-types",
                    "The signature of the target method to be intercepted. Follows format: new[] { return_type, param_1_type, param_2_type, ..., param_n_type }")
                {
                    Argument = new Argument<string[]>(),
                    Required = true
                }
            };
    }
}
