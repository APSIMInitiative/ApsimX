# if required install the apt packages for linux builds prior to the below steps with command: apt update -q --silent && apt install -yq libsqlite3-dev
# The models project needs to be restored first using the command: dotnet restore ./Models/Models.csproj
# Secondly, the models project needs to be published using the command: dotnet publish ./Models/Models.csproj -c Release -r linux-x64 --self-contained false -o ./app

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS build

COPY entrypoint.sh /entrypoint.sh
COPY ./app /app
COPY ./Prototypes /validation_files/Prototypes
COPY ./Examples /validation_files/Examples
COPY ./Tests/UnderReview /validation_files/Tests/UnderReview
COPY ./Tests/Validation /validation_files/Tests/Validation
COPY ./Tests/Simulation /validation_files/Tests/Simulation

USER root
RUN apt update -q --silent && \
    apt install -yq libsqlite3-dev
# Add models to path
ENV PATH=$PATH:/app
# This works to run a models dll.
RUN ["chmod", "+x", "/entrypoint.sh"]
ENTRYPOINT ["/entrypoint.sh"] 
