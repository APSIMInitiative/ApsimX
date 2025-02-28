# if required install the apt packages for linux builds prior to the below steps with command: apt update -q --silent && apt install -yq libsqlite3-dev
# The models project needs to be restored first using the command: dotnet restore ./Models/Models.csproj
# Secondly, the models project needs to be published using the command: dotnet publish ./Models/Models.csproj -c Release -r linux-x64 --self-contained false -o ./app

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0-noble-chiseled AS build

# ARG commit
# ARG version

# https://www.gnu.org/software/gettext/manual/html_node/Locale-Environment-Variables.html
ENV \
    LC_ALL=en_AU.UTF-8 \
    LANG=en_AU.UTF-8
WORKDIR /app
COPY --link ./app .
USER $APP_UID
# This works to run a models dll.
ENTRYPOINT ["dotnet","Models.dll"] 


# # Regular apsimng
# FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS apsimng
# RUN apk update && apk add sqlite-dev
# WORKDIR /app
# COPY --from=build /app .
# # ENV PATH=$PATH:/app
# ENTRYPOINT [ "dotnet", "Models.dll" ]

# # Repeat the same steps using apsim-r as the base image, in order to build
# # apsimng-complete.
# # docker build --build-arg version=0.0.0.0 --target apsimng-complete -t apsiminitiative/apsimng-complete:latest .
# FROM apsiminitiative/apsimng-r:latest AS apsimng-complete

# # Install sqlite3
# RUN apt update -q --silent && \
#     apt install -yq libsqlite3-dev

# # Copy build artifacts from the intermediate container to /apsim
# COPY --from=build /app /opt/apsim/

# # Add apsim to path
# ENV PATH=$PATH:/opt/apsim

# # Set shell to bash (best shell :)
# SHELL ["bash", "-c"]

# # Entrypoint is Models CLI
# ENTRYPOINT ["Models"]


# # Build the GUI in another intermediate image
# FROM build AS build-apsimng-gui

# RUN dotnet publish -c Release -f net8.0 -r linux-x64 --no-self-contained /apsim/ApsimNG/ApsimNG.csproj


# # GUI image uses apsimng as base image
# # docker build <build args> --target apsimng-gui -t apsiminitiative/apsimng-gui:latest .
# FROM apsimng AS apsimng-gui

# # Copy build artifacts from the intermediate container to /opt/apsim
# COPY --from=build-apsimng-gui /apsim/bin/Release/net8.0/linux-x64/publish/ /opt/apsim/

# # Install graphical libraries.
# RUN apt update -q --silent &&                                                  \
#     apt install -yq gtk-sharp3                                                 \
#                     libgtksourceview-4-0

# ENTRYPOINT ["ApsimNG"]

