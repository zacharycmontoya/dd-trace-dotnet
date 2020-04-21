# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

# copy csproj and restore as distinct layer
COPY *.csproj .
RUN dotnet restore

# copy everything else and build app
COPY . .
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app ./
ENV DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true DOTNET_CLI_TELEMETRY_OPTOUT=1

# Install DataDog APM
ARG TRACER_VERSION=1.16.1
RUN curl -LO https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION}/datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb
RUN dpkg -i ./datadog-dotnet-apm_${TRACER_VERSION}_amd64.deb
#RUN curl -L https://github.com/DataDog/dd-trace-dotnet/releases/download/v${TRACER_VERSION/datadog-dotnet-apm-${TRACER_VERSION.tar.gz | tar xzf - -C /opt/datadog

COPY --from=build /app ./

# Setup DataDog APM
ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER={846F5F1C-F9AE-4B07-969E-05C26BC060D8}
ENV CORECLR_PROFILER_PATH=/opt/datadog/Datadog.Trace.ClrProfiler.Native.so
ENV DD_INTEGRATIONS=/opt/datadog/integrations.json
ENV DD_DOTNET_TRACER_HOME=/opt/datadog
ENV DD_LOGS_INJECTION=true

RUN mkdir -p /var/log/datadog/dotnet
#RUN ln -sf /dev/stdout /var/log/datadog/dotnet/dotnet-profiler.log
RUN ln -sf /dev/stdout /var/log/datadog/dotnet/dotnet-tracer-dotnet-20200421.log

ENTRYPOINT ["dotnet", "AspNetCore31.NoTraces.dll"]