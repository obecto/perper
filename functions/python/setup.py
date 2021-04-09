import setuptools
from setuptools import setup

with open("README.md") as f:
    long_description = f.read()

setup(
    name='Perper',
    version='0.5.1',
    packages=['perper', 'perper.jupyter', 'perper.model', 'perper.services', 'perper.cache', 'perper.cache.notifications'],
    long_description=long_description,
    long_description_content_type='text/markdown',
    license='MIT License',
    author='Obecto EOOD',
    url='https://github.com/obecto/perper',
    install_requires=['azure-functions-worker','pyignite>=0.3.4', 'grpcio', 'ipython', 'jupyterlab'],
    classifiers=[
        'Development Status :: 3 - Alpha',
        'Programming Language :: Python',
        'Operating System :: OS Independent',
        'License :: OSI Approved :: MIT License',
    ]
)