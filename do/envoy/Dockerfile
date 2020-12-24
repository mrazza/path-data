FROM envoyproxy/envoy:v1.16-latest
COPY envoy_config.yaml /etc/envoy.yaml
COPY api_descriptor.pb /data/api_descriptor.pb
CMD /usr/local/bin/envoy -c /etc/envoy.yaml

