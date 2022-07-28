import os
import sys
import pkg_resources
import setuptools
import re
from setuptools import setup


class BuildProtos(setuptools.Command):
    description = "build grpc protobuf modules"
    user_options = []

    def initialize_options(self):
        pass

    def finalize_options(self):
        pass

    def run(self):
        proto_files = {("../../proto/fabric.proto", "perper/protocol/proto/")}

        from grpc_tools import protoc

        package_root = os.path.dirname(os.path.abspath(__file__))
        well_known_protos_include = pkg_resources.resource_filename("grpc_tools", "_proto")
        for proto_file, python_path in proto_files:
            proto_file = os.path.join(package_root, proto_file)
            python_path = os.path.join(package_root, python_path)
            proto_path = os.path.dirname(proto_file)

            command = [
                "grpc_tools.protoc",
                "--proto_path={}".format(proto_path),
                "--proto_path={}".format(well_known_protos_include),
                "--python_out={}".format(python_path),
                "--grpc_python_out={}".format(python_path),
            ] + [proto_file]

            if protoc.main(command) != 0:
                if self.strict_mode:
                    raise Exception("error: {} failed".format(command))
                else:
                    sys.stderr.write("warning: {} failed".format(command))

            grpc_generated_path = os.path.join(python_path, os.path.splitext(os.path.basename(proto_file))[0] + "_pb2_grpc.py")
            with open(grpc_generated_path, "r") as grpc_generated_file:
                grpc_content = grpc_generated_file.read()

            grpc_content = re.sub("(?m)^(import.*_pb2)", "from . \\1", grpc_content)

            with open(grpc_generated_path, "w") as grpc_generated_file:
                grpc_generated_file.write(grpc_content)


with open("README.md") as f:
    long_description = f.read()

setup(
    name="Perper",
    version="0.8.0b2",
    packages=setuptools.find_packages(),
    long_description=long_description,
    long_description_content_type="text/markdown",
    license="MIT License",
    author="Obecto EOOD",
    url="https://github.com/obecto/perper",
    setup_requires=["grpcio-tools>=1.33.2"],
    install_requires=["pyignite>=0.3.4", "grpcio>=1.33.2", "protobuf>=3.17.3", "backoff>=1.11.1"],
    classifiers=[
        "Development Status :: 3 - Alpha",
        "Programming Language :: Python",
        "Operating System :: OS Independent",
        "License :: OSI Approved :: MIT License",
    ],
    cmdclass={"build_protos": BuildProtos},
)
