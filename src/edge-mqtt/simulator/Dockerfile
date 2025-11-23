FROM python:3.12-alpine

RUN pip install --no-cache-dir paho-mqtt==1.6.1

WORKDIR /app
COPY machine.py /app/machine.py

ENTRYPOINT ["sh", "-c", "sleep 10 && python /app/machine.py"]
