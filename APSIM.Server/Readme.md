Remote control apsim

Several methods of controlling apsim 

V1 - initially developed to talk with [APSIM.Client](https://github.com/APSIMInitiative/APSIM.Client), a homebrew C application, it uses a custom protocol to talk over sockets connedcting native (C) and managed (.NET) clients

ZMQ+Msgpack - uses [0MQ](https://zeromq.org/) and [msgpack](https://github.com/msgpack/msgpack-cli) to talk with multilanguage clients in two communication paradigms: a "one shot" protocol that starts a simulation, collecting output at termination; and an interactive protocol that allows the controlling process to pause & resume a simulation while also get/set-ing data during progression. Sample and tests are in Tests/Simulation/ZMQ-Sync

Todo:
- test harness in python
- structs in msgpack data packets? 
- error handling/recovery
- events (eg sow, irrigate etc)
- 