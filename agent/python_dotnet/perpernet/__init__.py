# Pythonnet setup
from clr_loader import get_coreclr
from pythonnet import set_runtime
import sys
import os.path

ROOT_DIR = os.path.dirname(os.path.abspath(__file__))
perper_utils_folder = os.path.abspath(os.path.join(ROOT_DIR,
                                                    '../DotnetUtils/PerperUtils/bin/Debug/net5.0/'))
sys.path.append(perper_utils_folder)
dotnetAgentDllFolder = os.path.abspath(os.path.join(ROOT_DIR,
                                                    '../DotnetUtils/DllDump/bin/Debug/net5.0/'))
sys.path.append(dotnetAgentDllFolder)
runtime_path = os.path.abspath(os.path.join(dotnetAgentDllFolder,
                                            'DllDump.runtimeconfig.json'))
rt = get_coreclr(runtime_path)
set_runtime(rt)

import clr
clr.AddReference("PerperUtils")
clr.AddReference("Perper")

from .extensions import *
from .application import *

