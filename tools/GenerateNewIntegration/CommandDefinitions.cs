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
                    "Override the generated method name. If undefined, the generated method name will match the name of the target method.")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    new string[] { "--caller-assembly", "-ca" },
                    "The name of the assembly where calls to the target method are searched.")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    new string[] { "--caller-type", "-ct" },
                    "The namespace-qualified name of the type where calls to the target method are searched.")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    new string[] { "--caller-method", "-cm" },
                    "The name of the method where calls to the target method are searched.")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    new string[] { "--target-assembly", "-ta" },
                    "The name of the assembly that contains the target method to be intercepted. Required if TargetAssemblies is not set.")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    new string[] { "--target-assemblies", "-tas" },
                    "The name of the assemblies that contain the target method to be intercepted. Required if TargetAssembly is not set.")
                {
                    Argument = new Argument<string[]>(),
                    Required = true
                },
                new Option(
                    new string[] { "--target-type", "-tt" },
                    "The namespace-qualified name of the type that contains the target method to be intercepted.")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
                new Option(
                    new string[] { "--target-method", "-tm" },
                    "The name of the target method to be intercepted.")
                {
                    Argument = new Argument<string>(),
                    Required = true
                },
                new Option(
                    new string[] { "--target-method-is-static", "-tstatic" },
                    "The name of the target method to be intercepted.")
                {
                    Argument = new Argument<bool>(),
                    Required = true
                },
                new Option(
                    new string[] { "--target-minimum-version", "-tminv" },
                    "The minimum version of the containing assembly.")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    new string[] { "--target-maximum-version", "-tmaxv" },
                    "The maximum version of the containing assembly.")
                {
                    Argument = new Argument<string>(),
                },
                new Option(
                    new string[] { "--target-signature-types", "-tsig" },
                    "The signature of the target method to be intercepted. Follows format: new[] { return_type, param_1_type, param_2_type, ..., param_n_type }")
                {
                    Argument = new Argument<string[]>(),
                    Required = true
                }
            };
    }
}
