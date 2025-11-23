FROM python:3.12-alpine
WORKDIR /app
RUN pip install --no-cache-dir fastapi==0.114.2 uvicorn[standard]==0.30.6 psycopg2-binary==2.9.9
COPY app.py /app/app.py
ENV PORT=8090
EXPOSE 8090
CMD ["uvicorn", "app:app", "--host", "0.0.0.0", "--port", "8090"]