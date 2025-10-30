FROM python:3.12-alpine

RUN pip install --no-cache-dir paho-mqtt==1.6.1

WORKDIR /app
COPY sim.py /app/sim.py

ENTRYPOINT ["python", "/app/sim.py"]