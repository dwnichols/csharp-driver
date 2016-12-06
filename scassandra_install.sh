#!/bin/bash
Write-Host "Install scassandra..."

dep_dir="$HOME/deps"
if [ ! -d $dep_dir ]; then
  # Control will enter here if $DIRECTORY doesn't exist.
  echo "creating deps dir $deps_dir"
  mkdir $dep_dir
fi

# Install SCassandra
scassandra_path="$dep_dir/scassandra-server-1.0.10"
if [ ! -d $scassandra_path ]; then
  echo "creating scassandra dir $scassandra_path"
  mkdir $scassandra_path
  wget -O scassandra-server-1.0.10.tar.gz https://github.com/scassandra/scassandra-server/archive/1.0.10.zip
  tar -C $dep_dir -zxf scassandra-server-1.0.10.tar.gz
fi

scassandra_build_path="$scassandra_path/server/build/libs"
if [ ! -d $scassandra_build_path ]; then
  echo "Building scassandra..."
  pushd $scassandra_path
  ./gradlew server:fatJar
  popd
fi

export SCASSANDRA_JAR="$scassandra_build_path/scassandra-server_2.11-1.0.11-SNAPSHOT-standalone.jar"
echo "scassandra jar path: $SCASSANDRA_JAR"
