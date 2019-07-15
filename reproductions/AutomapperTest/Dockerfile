#FROM mcr.microsoft.com/dotnet/core/sdk:2.2.301-bionic AS build
FROM mcr.microsoft.com/dotnet/core/sdk:2.1.701-bionic AS build
ARG TRACER_VERSION=1.5.0
RUN mkdir -p /opt/datadog
RUN mkdir -p /var/log/datadog
RUN curl -L https://github.com/DataDog/dd-trace-dotnet/releases/download/v$TRACER_VERSION/datadog-dotnet-apm-$TRACER_VERSION.tar.gz | tar xzf - -C /opt/datadog

WORKDIR /src
COPY AutomapperTest.csproj .
RUN dotnet restore AutomapperTest.csproj

COPY . .
#RUN dotnet publish AutomapperTest.csproj -c Release -f netcoreapp2.2 -o /app
RUN dotnet publish AutomapperTest.csproj -c Release -f netcoreapp2.1 -o /app

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json
ENV DD_TRACE_ENABLED=true
ENV DD_CLR_DISABLE_OPTIMIZATIONS=false

WORKDIR /app
ENTRYPOINT ["dotnet", "/app/AutomapperTest.dll"]
