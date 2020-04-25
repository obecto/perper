#!/bin/bash

dotnet build && cp -R bin/Debug/netcoreapp3.1/bin . && func start
