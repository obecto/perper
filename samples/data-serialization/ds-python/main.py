from pyignite import Client
from SimpleData import SimpleData
import time

# Progress
# Writing and Reading from python works
# Writing and Reading from dotnet works
# Finds its own Binary type
# Works with objects and dynamics
# TODO: Move this to samples
# Not sure about subscribing to a stream

def preview(cache):
    print("Size of %s %s" % (cache.name, cache.get_size()))

client = Client()
client.connect('localhost', 10800)
client.register_binary_type(SimpleData)

print(client.get_cache_names())
initial_caches_num = len(client.get_cache_names())

stream_name = None
for cache_name in list(client.get_cache_names()):
    if "DynamicDataStream" in cache_name:
        stream_name = cache_name

if stream_name == None:
    print('Perper stream not started yet')
else:
    simpleDataStream = client.get_or_create_cache(stream_name)


simpleDataStream.put(
    simpleDataStream.get_size() + 1,
    SimpleData(name='Goshko', priority=1231, json='test')
)

simpleDataStream.put(simpleDataStream.get_size() + 1, "TESTING DYNAMICS")

for el in simpleDataStream.scan():
    print(el[1])
    print(el[1].__class__)

last_size = simpleDataStream.get_size();

while True:
    caches_num = len(client.get_cache_names())
    if caches_num > initial_caches_num:
        print(f"New cache: {list(client.get_cache_names())[-1]}")
        new_cache_name = list(client.get_cache_names())[-1]
        new_cache = client.get_cache(new_cache_name)
        print(new_cache.scan())
    current_size = simpleDataStream.get_size()
    
    if (current_size > last_size):
        last_size = current_size
        
        print("New Item...")
        ## This is not possible because it is not sorted by timestamp / key
        
        for item in simpleDataStream.scan():
            print(item)
        
        *_, new = simpleDataStream.scan()
        print(new)
        
    time.sleep(1)
