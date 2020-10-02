import alpacasearch
import pyuv



loop = pyuv.Loop.default_loop()

alpacasearch.search(loop)                     

loop.run()