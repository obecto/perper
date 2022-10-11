import setuptools
from setuptools import setup

with open("README.md") as f:
    long_description = f.read()

setup(
    name="perpernet",
    version="0.8.0rc1",
    packages=setuptools.find_packages(),
    long_description=long_description,
    long_description_content_type="text/markdown",
    license="MIT License",
    author="Obecto EOOD",
    url="https://github.com/obecto/perper",
    install_requires=["backoff>=1.11.1", "pythonnet>=3.0.0a2", "asyncio"],
    classifiers=[
        "Development Status :: 3 - Alpha",
        "Programming Language :: Python",
        "Operating System :: OS Independent",
        "License :: OSI Approved :: MIT License",
    ],
)
