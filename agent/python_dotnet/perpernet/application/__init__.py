from clr_loader import get_coreclr
from pythonnet import set_runtime
import sys

sys.path.append("/home/nikola/Projects/Hatchery/perper/samples/dotnet/BasicSample/bin/Debug/net5.0/")
runtime_path = "/home/nikola/Projects/Hatchery/perper/samples/dotnet/BasicSample/bin/Debug/net5.0/BasicSample.runtimeconfig.json"
rt = get_coreclr(runtime_path)
set_runtime(rt)
from .startup import run
