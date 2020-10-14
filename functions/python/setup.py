import setuptools
from setuptools import setup

with open("README.md") as f:
    long_description = f.read()

setup(
    name='azure-functions-perper-binding',
    version='0.5.1',
    packages=['azure_functions.perper'],
    long_description=long_description,
    long_description_content_type='text/markdown',
    license='MIT License',
    author='Obecto EOOD',
    url='https://github.com/obecto/perper',
    install_requires=['azure-functions-worker','pyignite'],
    classifiers=[
        'Development Status :: 3 - Alpha',
        'Programming Language :: Python :: 3.6',
        'Operating System :: OS Independent',
        'License :: OSI Approved :: MIT License',
    ]
)