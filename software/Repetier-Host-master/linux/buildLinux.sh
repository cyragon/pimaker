#!/bin/sh
cp ../src/empty.txt RepetierHost
cp ../version.txt RepetierHost
cp ../changelog.txt RepetierHost
cp ../APACHE-LICENSE-2.0.txt RepetierHost
cp ../Repetier-Host-licence.txt RepetierHost
cp -r ../src/data RepetierHost
cp ../src/RepetierHost/bin/Release/Rep* RepetierHost
tar -czf ../repetierHostLinux.tgz RepetierHost


