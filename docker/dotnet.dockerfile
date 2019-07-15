FROM mcr.microsoft.com/dotnet/core/sdk:2.2-alpine
WORKDIR /app

RUN apk add bash libc6-compat

ADD https://raw.githubusercontent.com/vishnubob/wait-for-it/master/wait-for-it.sh /bin/wait-for-it
RUN chmod +x /bin/wait-for-it

COPY . ./
RUN dotnet restore Datadog.Trace.sln
