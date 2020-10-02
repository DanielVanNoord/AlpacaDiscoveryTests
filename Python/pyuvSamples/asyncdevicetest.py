import alpacadevice
import pyuv



loop = pyuv.Loop.default_loop()

alpacadevice.server(loop, 4321)                     

loop.run()