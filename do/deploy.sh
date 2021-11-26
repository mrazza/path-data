#!/bin/bash
dotnet build ../proto
protoc --include_imports --include_source_info --proto_path="$HOME/.nuget/packages/grpc.tools/1.19.0/build/native/include" --proto_path=../proto/ --descriptor_set_out=api_descriptor.pb ../proto/*.proto
dotnet publish -c Release ../server
docker build -t registry.digitalocean.com/razza/server:v1.17 ../server/bin/Release/netcoreapp2.2/publish
docker push registry.digitalocean.com/razza/server:v1.17
kubectl apply -f path-api-server.yaml
