#!/bin/bash

dotnet build && cp -R bin/bin . && func start --verbose
