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
@"/// <summary>
/// TODO: Update the type parameters AND ESPECIALLY THE RETURN TYPE if the types are CLR primitives.
/// </summary>
[InterceptMethod(
{0})]
public static {1} {2}({3}
    int opCode,
    int mdToken,
    long moduleVersionPtr)
{{
    const string methodName = ""{4}"";
    Type instanceType = {5};
    Type instrumentedType = {6}; // use GetInstrumentedType for base types or GetInstrumentedInterface for interfaces
    {7} instrumentedMethod;

    // Use the MethodBuilder to construct a delegate to the original method call
    try
    {{
        instrumentedMethod =
            MethodBuilder<{7}>
                .Start(moduleVersionPtr, mdToken, opCode, methodName)
                .WithConcreteType(instrumentedType)
                .WithParameters({8})
                .WithNamespaceAndNameFilters({9}) // Needed for the fallback logic if target method name is overloaded
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
            instrumentedType: {10});
        throw;
    }}

    // Open a scope, decorate the span, and call the original method
    using (Scope scope = CreateScopeFrom{2}({11}))
    {{
        try
        {{
            return instrumentedMethod({11});
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
            var rootCommand = new RootCommand("Generates files needed for automatic instrumentation");

            CommandDefinitions.AddMethod.Handler = CommandHandler.Create((InterceptMethodAttribute interceptMethodAttribute, string overrideMethodName) => AddMethod(interceptMethodAttribute, overrideMethodName));
            rootCommand.Add(CommandDefinitions.AddMethod);

            // Parse the incoming args and invoke the handler
            return rootCommand.InvokeAsync(args).Result;
        }

        private static void AddMethod(InterceptMethodAttribute interceptMethodAttribute, string overrideMethodName)
        {
            CorrectGenericSymbols(interceptMethodAttribute);
            Console.WriteLine(GenerateMethod(interceptMethodAttribute, overrideMethodName));
        }

        private static void CorrectGenericSymbols(InterceptMethodAttribute interceptMethodAttribute)
        {
            interceptMethodAttribute.CallerType = interceptMethodAttribute.CallerType?.Replace("^", string.Empty);
            interceptMethodAttribute.TargetType = interceptMethodAttribute.TargetType?.Replace("^", string.Empty);
            interceptMethodAttribute.TargetSignatureTypes = interceptMethodAttribute.TargetSignatureTypes?.Select(s => s.Replace("^", string.Empty)).ToArray();
        }

        private static string GenerateMethod(InterceptMethodAttribute interceptMethodAttribute, string overrideMethodName)
        {
            var stringFormatArgs = new List<object>();

            // Helper variables
            var interpretedVariableTypesOfTargetSignatureTypes = interceptMethodAttribute.TargetSignatureTypes.Select(InterpretSignatureType);

            // {0} = InterceptMethodAttribute
            var interceptMethodContentsSb = new StringBuilder();
            var indentation = "    ";
            var lineBreakAndIndentation = "\r\n" + indentation;
            if (interceptMethodAttribute.TargetAssemblies.Length == 1)
            {
                interceptMethodContentsSb.AppendLine($"{indentation}TargetAssembly = {WrapInQuotes(interceptMethodAttribute.TargetAssemblies.First())},");
            }
            else
            {
                interceptMethodContentsSb.AppendLine($"{indentation}TargetAssemblies = new[] {{ {string.Join(", ", interceptMethodAttribute.TargetAssemblies.Select(WrapInQuotes))} }},");
            }

            interceptMethodContentsSb.AppendLine($"{indentation}TargetType = {WrapInQuotes(interceptMethodAttribute.TargetType)},");
            interceptMethodContentsSb.AppendLine($"{indentation}TargetMethod = {WrapInQuotes(interceptMethodAttribute.TargetMethod)},");
            interceptMethodContentsSb.AppendLine($"{indentation}TargetSignatureTypes = new[] {{ {string.Join(", ", interceptMethodAttribute.TargetSignatureTypes.Select(WrapInQuotes))} }},");
            interceptMethodContentsSb.AppendLine($"{indentation}TargetMinimumVersion = {WrapInQuotes(interceptMethodAttribute.TargetMinimumVersion)},");
            interceptMethodContentsSb.Append($"{indentation}TargetMaximumVersion = {WrapInQuotes(interceptMethodAttribute.TargetMaximumVersion)}");
            var interceptMethodContents = interceptMethodContentsSb.ToString();
            stringFormatArgs.Add(interceptMethodContents);

            // {1} = return type
            var returnTypeString = interpretedVariableTypesOfTargetSignatureTypes.First();
            stringFormatArgs.Add(returnTypeString);

            // {2} = name of autogenerated method, which defaults to TargetMethod
            var methodName = overrideMethodName ?? interceptMethodAttribute.TargetMethod;
            stringFormatArgs.Add(methodName);

            // {3} = comma-separated method parameter list
            var parameterList = new List<string>();
            var signatureTypeLength = interceptMethodAttribute.TargetSignatureTypes.Length;
            if (!interceptMethodAttribute.TargetMethodIsStatic)
            {
                parameterList.Add("instanceObject");
            }

            if (signatureTypeLength > 1)
            {
                for (int i = 1; i < signatureTypeLength; i++)
                {
                    parameterList.Add($"arg{i}");
                }
            }

            var initialObjectType = parameterList.Any() ? "object " : string.Empty;

            var methodSignatureParameters = initialObjectType + string.Join($",{lineBreakAndIndentation}object ", parameterList);
            methodSignatureParameters = string.IsNullOrWhiteSpace(methodSignatureParameters) ? string.Empty : lineBreakAndIndentation + methodSignatureParameters + ",";
            stringFormatArgs.Add(methodSignatureParameters);

            // {4} = TargetMethod
            var targetMethodName = interceptMethodAttribute.TargetMethod;
            stringFormatArgs.Add(targetMethodName);

            // {5} = instanceType assignment
            var instanceTypeExpression = interceptMethodAttribute.TargetMethodIsStatic ? null : "instanceObject.GetType()";
            stringFormatArgs.Add(instanceTypeExpression);

            // {6} = instrumentedType assignment
            var instrumentedTypeExpression = interceptMethodAttribute.TargetMethodIsStatic ? null : $"instanceObject.GetInstrumentedType({WrapInQuotes(interceptMethodAttribute.TargetType)}) ?? instanceObject.GetInstrumentedInterface({WrapInQuotes(interceptMethodAttribute.TargetType)})";
            stringFormatArgs.Add(instrumentedTypeExpression);

            // {7} = instrumentedMethod delegate type
            var typeListForDelegateType = new List<string>();
            if (!interceptMethodAttribute.TargetMethodIsStatic)
            {
                typeListForDelegateType.Add("object");
            }

            var instrumentedMethodDelegateType = $"Func<{string.Join(", ", typeListForDelegateType.Concat(interpretedVariableTypesOfTargetSignatureTypes.Skip(1)).Concat(interpretedVariableTypesOfTargetSignatureTypes.Take(1)))}>";
            stringFormatArgs.Add(instrumentedMethodDelegateType);

            // {8} = WithParameters params
            // default to new object[0];
            var withParametersEnumerable = parameterList.AsEnumerable();
            if (!interceptMethodAttribute.TargetMethodIsStatic)
            {
                withParametersEnumerable = withParametersEnumerable.Skip(1);
            }

            var withParametersArray = string.Join(", ", withParametersEnumerable);
            stringFormatArgs.Add(withParametersArray);

            // {9} = WithNamespaceAndNameFilters params
            var withNamespaceAndNameFiltersArray = string.Join(", ", interceptMethodAttribute.TargetSignatureTypes.Select(s => WrapInQuotes(StripGenerics(s))));
            stringFormatArgs.Add(withNamespaceAndNameFiltersArray);

            // {10} = the TargetType, being passed to the Logging method
            var instrumentedTypeName = WrapInQuotes(interceptMethodAttribute.TargetType);
            stringFormatArgs.Add(instrumentedTypeName);

            // {11} = CreateScope params
            var createScopeParameters = string.Join($", ", parameterList);
            stringFormatArgs.Add(createScopeParameters);

            // Format the template and return the result
            return string.Format(MethodTemplate, stringFormatArgs.ToArray());
        }

        private static string WrapInQuotes(string s) => "\"" + s + "\"";

        private static string StripGenerics(string s) =>
            s.Contains('<') switch
            {
                true => s.Substring(0, s.IndexOf('<')),
                false => s
            };

        private static string InterpretSignatureType(string s) =>
            s switch
            {
                "System.Void" => "void",
                "System.Boolean" => "bool",

                "System.Byte" => "byte",
                "System.SByte" => "sbyte",
                "System.Char" => "char",

                "System.Decimal" => "decimal",
                "System.Double" => "double",
                "System.Single" => "float",

                "System.Int16" => "short",
                "System.UInt16" => "ushort",

                "System.Int32" => "int",
                "System.UInt32" => "uint",

                "System.Int64" => "long",
                "System.UInt64" => "ulong",

                "System.String" => "string",

                _ => "object",
            };
    }
}
