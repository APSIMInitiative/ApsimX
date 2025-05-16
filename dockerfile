# if required install the apt packages for linux builds prior to the below steps with command: apt update -q --silent && apt install -yq libsqlite3-dev
# The models project needs to be restored first using the command: dotnet restore ./Models/Models.csproj
# Secondly, the models project needs to be published using the command: dotnet publish ./Models/Models.csproj -c Release -r linux-x64 --self-contained false -o ./app

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-noble-chiseled AS build

# https://www.gnu.org/software/gettext/manual/html_node/Locale-Environment-Variables.html
ENV \
    LC_ALL=en_AU.UTF-8 \
    LANG=en_AU.UTF-8

COPY ./app /app
USER root
# Add models to path
ENV PATH=$PATH:/app
# This works to run a models dll.
ENTRYPOINT ["Models"] 
