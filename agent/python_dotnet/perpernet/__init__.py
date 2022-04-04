# Pythonnet setup
from clr_loader import get_coreclr
from pythonnet import set_runtime
import sys
import os.path

ROOT_DIR = os.path.dirname(os.path.abspath(__file__))
dotnetAgentDllFolder = os.path.abspath(os.path.join(ROOT_DIR, '../../..', "samples/dotnet/BasicSample/bin/Debug/net5.0/"))
sys.path.append(dotnetAgentDllFolder)

runtime_path = os.path.abspath(os.path.join(ROOT_DIR, "../runtimeconfig.json"))
rt = get_coreclr(runtime_path)
set_runtime(rt)

import clr
clr.AddReference("Perper")

# from .model import *
from .extensions import *
from .application import *

