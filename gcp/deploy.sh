#!/bin/bash
dotnet build ../proto
protoc --include_imports --include_source_info --proto_path="$HOME/.nuget/packages/grpc.tools/1.19.0/build/native/include" --proto_path=../proto/ --descriptor_set_out=api_descriptor.pb ../proto/*.proto
gcloud endpoints services deploy api_descriptor.pb api_config.yaml
dotnet publish -c Release ../server
cp Dockerfile ../server/bin/Release/netcoreapp2.2/publish
docker build -t gcr.io/path-data/server:v1.3 ../server/bin/Release/netcoreapp2.2/publish
gcloud docker -- push gcr.io/path-data/server:v1.3
kubectl apply -f path-api-server.yaml
