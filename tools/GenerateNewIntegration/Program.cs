using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;
using Datadog.Core.Tools;
using Datadog.Trace.ClrProfiler;

namespace GenerateNewIntegration
{
    public class Program
    {
        private const string MethodTemplate =
@"
[InterceptMethod(
{0})]
public static object {1}({2}
    int opCode,
    int mdToken,
    long moduleVersionPtr)
{{
    const string methodName = nameof({1});
    Type instanceType = {3};
    Type instrumentedType = {4};

    try
    {{
        originalMethodCall =
            MethodBuilder<Func<object, object, CancellationToken, object>>
                .Start(moduleVersionPtr, mdToken, opCode, methodName)
                .WithTargetType(wireProtocolType)
                .WithParameters(connection, cancellationToken)
                .Build();
    }}
    catch (Exception ex)
    {{
        Log.ErrorRetrievingMethod(
            exception: ex,
            opCode: opCode,
            mdToken: mdToken,
            moduleVersionPointer: moduleVersionPtr,
            methodName: methodName,
            instanceType: instanceType?.AssemblyQualifiedName,
            instrumentedType: {5});
        throw;
    }}

    using (var scope = CreateScope({6}))
    {{
        try
        {{
            return originalMethodCall(null);
        }}
        catch (Exception ex)
        {{
            scope?.Span.SetException(ex);
            throw;
        }}
    }}
}}";

        private static StringBuilder sb = new StringBuilder();

        public static int Main(string[] args)
        {
            // Create a root command with some options
            var rootCommand = new RootCommand("Generates files needed for automatic instrumentation")
            {
                new Option(
                    "--int-option",
                    "An option whose argument is parsed as an int")
                {
                    Argument = new Argument<int>(defaultValue: () => 42)
                },
                new Option(
                    "--bool-option",
                    "An option whose argument is parsed as a bool")
                {
                    Argument = new Argument<bool>()
                },
                new Option(
                    "--file-option",
                    "An option whose argument is parsed as a FileInfo")
                {
                    Argument = new Argument<FileInfo>()
                }
            };

            var addFileSubcommand = new Command("add-file")
            {
                new Argument<string>("filepath")
            };
            addFileSubcommand.Handler = CommandHandler.Create<string>((filepath) => AddFile(filepath));
            rootCommand.Add(addFileSubcommand);

            var addMethodSubcommand = new Command("add-method")
            {
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
                    "The name of the containing assembly")
                {
                    Argument = new Argument<string>(),
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
            addMethodSubcommand.Handler = CommandHandler.Create((InterceptMethodAttribute interceptMethodAttribute) => AddMethod(interceptMethodAttribute));
            rootCommand.Add(addMethodSubcommand);

            /*
            rootCommand.Handler = CommandHandler.Create<int, bool, FileInfo>((intOption, boolOption, fileOption) =>
            {
                Console.WriteLine($"The value for --int-option is: {intOption}");
                Console.WriteLine($"The value for --bool-option is: {boolOption}");
                Console.WriteLine($"The value for --file-option is: {fileOption?.FullName ?? "null"}");
            });
            */

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        private static void AddFile(string filepath)
        {
            Console.WriteLine("Thanks for the filepath argument, it is: " + filepath.ToString());

            if (!Path.IsPathFullyQualified(filepath))
            {
                var integrationsFolder = Path.Combine(EnvironmentTools.GetSolutionDirectory(), "src", "Datadog.Trace.ClrProfiler.Managed", "Integrations");
                filepath = Path.Combine(integrationsFolder, filepath);
            }

            Console.WriteLine("Final filepath argument: " + filepath.ToString());
        }

        private static void AddMethod(InterceptMethodAttribute interceptMethodAttribute)
        {
            Console.WriteLine("TargetSignatureTypes before correction: " + string.Join(',', interceptMethodAttribute.TargetSignatureTypes));
            Console.WriteLine();
            CorrectGenericSymbols(interceptMethodAttribute);
            Console.WriteLine("TargetSignatureTypes after correction: " + string.Join(',', interceptMethodAttribute.TargetSignatureTypes));

            Console.WriteLine(GenerateMethod(interceptMethodAttribute));
        }

        private static void CorrectGenericSymbols(InterceptMethodAttribute interceptMethodAttribute)
        {
            interceptMethodAttribute.CallerType = interceptMethodAttribute.CallerType?.Replace("^", string.Empty);
            interceptMethodAttribute.TargetType = interceptMethodAttribute.TargetType?.Replace("^", string.Empty);
            interceptMethodAttribute.TargetSignatureTypes = interceptMethodAttribute.TargetSignatureTypes?.Select(s => s.Replace("^", string.Empty)).ToArray();
        }

        private static string GenerateMethod(InterceptMethodAttribute interceptMethodAttribute)
        {
            var interceptMethodContentsSb = new StringBuilder();
            var indentation = "    ";
            var lineBreakAndIndentation = "\r\n" + indentation;
            if (interceptMethodAttribute.TargetAssemblies.Length == 1)
            {
                interceptMethodContentsSb.AppendLine($"{indentation}TargetAssembly = {WrapInQuotes(interceptMethodAttribute.TargetAssemblies.First())},");
            }
            else
            {
                // interceptMethodContentsSb.AppendLine($"{indentation}TargetAssembly = {WrapInQuotes(interceptMethodAttribute.TargetAssembly)},");
            }

            interceptMethodContentsSb.AppendLine($"{indentation}TargetType = {WrapInQuotes(interceptMethodAttribute.TargetType)},");
            interceptMethodContentsSb.AppendLine($"{indentation}TargetSignatureTypes = new[] {{ {string.Join(", ", interceptMethodAttribute.TargetSignatureTypes.Select(WrapInQuotes))} }},");
            interceptMethodContentsSb.AppendLine($"{indentation}TargetMinimumVersion = {WrapInQuotes(interceptMethodAttribute.TargetMinimumVersion)},");
            interceptMethodContentsSb.Append($"{indentation}TargetMaximumVersion = {WrapInQuotes(interceptMethodAttribute.TargetMaximumVersion)}");

            var parameterList = new List<string>();
            if (interceptMethodAttribute.TargetSignatureTypes.Length > 1)
            {
                if (!interceptMethodAttribute.TargetMethodIsStatic)
                {
                    parameterList.Add("instanceObject");
                }

                for (int i = 1; i < interceptMethodAttribute.TargetSignatureTypes.Length; i++)
                {
                    parameterList.Add($"arg{i}");
                }
            }

            var initialObjectType = parameterList.Any() ? "object " : string.Empty;

            var interceptMethodContents = interceptMethodContentsSb.ToString();
            var methodSignatureParameters = initialObjectType + string.Join($",{lineBreakAndIndentation}object ", parameterList);
            methodSignatureParameters = string.IsNullOrWhiteSpace(methodSignatureParameters) ? string.Empty : lineBreakAndIndentation + methodSignatureParameters + ",";
            var instanceTypeExpression = interceptMethodAttribute.TargetMethodIsStatic ? null : "instanceObject.GetType()";
            var instrumentedTypeExpression = interceptMethodAttribute.TargetMethodIsStatic ? null : $"instanceObject.GetInstrumentedType({WrapInQuotes(interceptMethodAttribute.TargetType)})";
            var instrumentedTypeName = WrapInQuotes(interceptMethodAttribute.TargetType);
            var createScopeParameters = string.Join($", ", parameterList);

            return string.Format(
                MethodTemplate,
                interceptMethodContents,
                interceptMethodAttribute.TargetMethod,
                methodSignatureParameters,
                instanceTypeExpression,
                instrumentedTypeExpression,
                instrumentedTypeName,
                createScopeParameters);
        }

        private static string WrapInQuotes(string s)
        {
            return "\"" + s + "\"";
        }

        /*
                public static int Main(string[] args)
                {
                    // File name
                    var filePath = GetFilePath();

                    // Class name
                    var className = GetClassName(filePath);

                    // Integration name
                    var integrationName = GetIntegrationName(className);

                    // Generate InterceptMethod
                    bool continueLoop = true;
                    while (continueLoop)
                    {
                        GetMethod();
                        bool invalidContinueResponse = true;
                        while (invalidContinueResponse)
                        {
                            Console.Write("Would you like to generate another method [Y|n]: ");
                            var response = Console.ReadLine();
                            if (string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
                            {
                                continueLoop = true;
                                invalidContinueResponse = false;
                            }
                            else if (string.Equals(response, "n", StringComparison.OrdinalIgnoreCase))
                            {
                                continueLoop = false;
                                invalidContinueResponse = false;
                            }
                        }
                    }

                    // Example: Store some stuff
                    sb.Append("Sample text");

                    // Finalize
                    if (string.IsNullOrEmpty(filePath))
                    {
                        Console.WriteLine(sb.ToString());
                    }
                    else
                    {
                        var fullPath = Path.GetFullPath(filePath);
                        var directory = Path.GetDirectoryName(fullPath);

                        Directory.CreateDirectory(directory);
                        File.WriteAllText(filePath, sb.ToString());
                    }

                    return 0;
                }

                private static string GetFilePath()
                {
                    Console.WriteLine("Please enter the output file name.");
                    Console.WriteLine("Note: If empty, the text will be sent to standard out.");
                    Console.WriteLine("Note: A relative path will default to a path within the Integrations folder.");
                    Console.Write("Filename: ");
                    var filePath = Console.ReadLine();

                    if (string.IsNullOrEmpty(filePath))
                    {
                        filePath = string.Empty;
                    }
                    else if (!Path.IsPathFullyQualified(filePath))
                    {
                        var integrationsFolder = Path.Combine(EnvironmentTools.GetSolutionDirectory(), "src", "Datadog.Trace.ClrProfiler.Managed", "Integrations");
                        filePath = Path.Combine(integrationsFolder, filePath);
                    }

                    return filePath;
                }

                private static string GetClassName(string filePath)
                {
                    if (string.IsNullOrEmpty(filePath))
                    {
                        Console.Write("Please enter the class name. Classname: ");
                        return Console.ReadLine();
                    }
                    else
                    {
                        var className = Path.GetFileNameWithoutExtension(filePath);
                        Console.WriteLine($"Classname is being set to {className}");
                        return className;
                    }
                }

                private static string GetIntegrationName(string className)
                {
                    if (className.EndsWith("Integration"))
                    {
                        var suggestion = className.Substring(0, className.Length - 11);
                        Console.Write($"Press enter to accept IntegrationName=\"{suggestion}\", or enter a new IntegrationName: ");
                        var response = Console.ReadLine();
                        return string.IsNullOrEmpty(response) ? suggestion : response;
                    }
                    else
                    {
                        Console.Write("Please enter the integration name. IntegrationName: ");
                        return Console.ReadLine();
                    }
                }

                private static string GetMethod()
                {
                    return null;
                }
        */
    }
}
