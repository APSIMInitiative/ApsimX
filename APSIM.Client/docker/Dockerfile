# APSIM Next Generation Server container

FROM gcc:latest as build
RUN git clone https://github.com/APSIMInitiative/APSIM.Client /client
RUN cd /client && make libapsimclient.so && make example

FROM debian:bullseye-slim
RUN apt update && apt install -y dumb-init
COPY --from=build /client/libapsimclient.so /usr/lib/
COPY --from=build /client/example /usr/bin/apsimclient-demo

# Use dumb-init as entrypoint, so that the demo client will respond to
# signals such as TERM.
ENTRYPOINT ["/usr/bin/dumb-init", "--", "/usr/bin/apsimclient-demo"]
