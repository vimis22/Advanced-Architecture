Build simulator: docker compose build --no-cache simulator
Run: docker compose up -d
Scale simulator: docker compose --scale simulator=100 

Kafka topics: iot.mqtt.raw, iot.mqtt.cmd 
Mqtt topics: pico/+/telemetry, pico/+/status, iot.mqtt.cmd

pico/+/telemetry, pico/+/status sources to iot.mqtt.raw
iot.mqtt.raw (kafka topic) sinks to iot.mqtt.cmd (mqtt topic)

All devices recieves all the messages. They have an internal filter. Messages to devices must be {"device_id":"<DEVICE_ID>","op":"power","value":"<on/off>"}

Sources from device:
Path: devices -> mqtt.broker -> kafka-connect -> kafka -> kafka-connect -> timescaledb

Sinks to device:
Path: kafka -> kafka-connect -> mqtt.broker -> device

Redpanda is purely for debugging kafka
