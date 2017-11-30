#!/bin/bash

dotnet publish  Loaner --runtime ubuntu.16.04-x64 --output '.\publish\Loaner\ubuntu.16.04-x86'
dotnet publish  Lighthouse.NetCoreApp --runtime ubuntu.16.04-x64 --output '.\publish\Lighthouse.NetCoreApp\ubuntu.16.04-x86'
