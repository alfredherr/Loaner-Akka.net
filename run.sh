#!/bin/bash
cd Lighthouse.NetCoreApp
dotnet run  &
cd ../Loaner
dotnet run Loaner.csproj &


