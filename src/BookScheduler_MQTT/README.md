# BookScheduler

BookScheduler is a simple book production simulator using Printers, Binders, and Packagers.  
Machines communicate via MQTT (mosquitto) and can be run in Docker.

---

## Prerequisites

- Docker installed
- (Optional) MQTT broker container (mosquitto)

Start the MQTT broker:

```bash
docker run -d --name mosquitto -p 1883:1883 eclipse-mosquitto

Build the image
docker build -t bookscheduler .

Run the container in interactive mode so you can choose how many books to print.
docker run -it --rm -e MQTT_HOST=mosquitto --name bookscheduler bookscheduler
