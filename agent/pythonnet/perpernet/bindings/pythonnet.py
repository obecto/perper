from clr_loader import get_coreclr
from pythonnet import set_runtime
import sys
import os.path

_root_dir = os.path.dirname(os.path.abspath(__file__))
_perper_utils_folder = os.path.abspath(os.path.join(_root_dir, "../../DotnetUtils/PerperUtils/bin/Debug/net5.0/"))
sys.path.append(_perper_utils_folder)
_dotnet_agent_dll_folder = os.path.abspath(os.path.join(_root_dir, "../../DotnetUtils/DllDump/bin/Debug/net5.0/"))
sys.path.append(_dotnet_agent_dll_folder)
_runtime_path = os.path.abspath(os.path.join(_dotnet_agent_dll_folder, "DllDump.runtimeconfig.json"))
_runtime = get_coreclr(_runtime_path)
set_runtime(_runtime)

import clr

clr.AddReference("Perper")
clr.AddReference("PerperUtils")
