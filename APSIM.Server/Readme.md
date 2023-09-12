Remote control apsim

Several methods of controlling apsim 

V1 - initially developed to talk with [APSIM.Client](https://github.com/APSIMInitiative/APSIM.Client), a homebrew C application  

ZMQ+Msgpack - uses [0MQ](https://zeromq.org/) and [msgpack](https://github.com/msgpack/msgpack-cli) to talk with multilanguage clients in two communication paradigms: a "one shot" protocol that starts a simulation, collecting output at its end; and an interactive protocol that allows the controlling process to pause & resume a simulation while also get/set-ting data during progression. 