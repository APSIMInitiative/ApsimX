FROM mono:latest

RUN apt-get update

RUN apt install -y genisoimage dos2unix

ENTRYPOINT["/ApsimX/Setup/osx/BuildMacDist.sh"]