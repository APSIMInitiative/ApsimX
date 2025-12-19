# APSIM Next Generation
#
# A container for running APSIM Next Generation.
#
# To build this container, run this from the ApsimX directory (after building apsim):
#    docker build <build args> --target build -t build .
# To run the container:
#    docker run -i --rm -v "$PWD:/apsim" apsiminitiative/apsimng path/to/my/file.apsimx

# Build apsim in an intermediate container
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Builder of this image may specify an commit (commit hash) and version number of the build.
# These are optional, but if set, the specified commit will be built, and stamped with the
# version number.
ARG commit
ARG version

RUN git clone https://github.com/APSIMInitiative/ApsimX /apsim
WORKDIR /apsim

# If commit variable is set, attempt to checkout the specified commit.
RUN echo commit=$commit && git checkout $commit

# If version variable is set, version stamp the build.
RUN echo version=$version && sed -i -e "s/0\.0\.0\.0/$version/g" \
    /apsim/Models/Properties/AssemblyVersion.cs

# Build apsim.
RUN dotnet publish --nologo -c Release -f net8.0 -r linux-x64 --no-self-contained /apsim/Models/Models.csproj


# The actual image is based on dotnet/runtime.
# docker build <build args> --target apsimng -t apsiminitiative/apsimng:latest .
FROM mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim AS apsimng

# Install sqlite3
RUN apt update -q --silent && \
    apt install -yq libsqlite3-dev

# Copy build artifacts from the intermediate container to /apsim
COPY --from=build /apsim/bin/Release/net8.0/linux-x64/publish /opt/apsim/

# Add apsim to path
ENV PATH=$PATH:/opt/apsim

# Set shell to bash (best shell :)
SHELL ["bash", "-c"]

# Entrypoint is Models CLI
ENTRYPOINT ["Models"]


# Repeat the same steps using apsim-r as the base image, in order to build
# apsimng-complete.
# docker build --build-arg version=0.0.0.0 --target apsimng-complete -t apsiminitiative/apsimng-complete:latest .
FROM apsiminitiative/apsimng-r:latest AS apsimng-complete

# Install sqlite3
RUN apt update -q --silent && \
    apt install -yq libsqlite3-dev

# Copy build artifacts from the intermediate container to /apsim
COPY --from=build /apsim/bin/Release/net8.0/linux-x64/publish /opt/apsim/

# Add apsim to path
ENV PATH=$PATH:/opt/apsim

# Set shell to bash (best shell :)
SHELL ["bash", "-c"]

# Entrypoint is Models CLI
ENTRYPOINT ["Models"]


# Build the GUI in another intermediate image
FROM build AS build-apsimng-gui

RUN dotnet publish -c Release -f net8.0 -r linux-x64 --no-self-contained /apsim/ApsimNG/ApsimNG.csproj


# GUI image uses apsimng as base image
# docker build <build args> --target apsimng-gui -t apsiminitiative/apsimng-gui:latest .
FROM apsimng AS apsimng-gui

# Copy build artifacts from the intermediate container to /opt/apsim
COPY --from=build-apsimng-gui /apsim/bin/Release/net8.0/linux-x64/publish/ /opt/apsim/

# Install graphical libraries.
RUN apt update -q --silent &&                                                  \
    apt install -yq gtk-sharp3                                                 \
                    libgtksourceview-4-0

ENTRYPOINT ["ApsimNG"]
