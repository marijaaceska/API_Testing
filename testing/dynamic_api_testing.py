import os
import time
import requests
import json
import pika
import threading
from datetime import datetime
from dotenv import load_dotenv
from elasticsearch import Elasticsearch
import urllib3

urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)
load_dotenv()

ES_HOST = os.getenv("ES_HOST")
ES_USER = os.getenv("ES_USER")
ES_PASSWORD = os.getenv("ES_PASSWORD")

RABBITMQ_HOST = os.getenv("RABBITMQ_HOST")
PUBLISHER_USER = os.getenv("PUBLISHER_USER")
PUBLISHER_PASS = os.getenv("PUBLISHER_PASS")
CONSUMER_USER = os.getenv("CONSUMER_USER")
CONSUMER_PASS = os.getenv("CONSUMER_PASS")
RABBITMQ_QUEUE = "api_logs"

es = Elasticsearch([ES_HOST], basic_auth=(ES_USER, ES_PASSWORD))

if es.ping():
    print("Connected to Elasticsearch!")
else:
    print("Could not connect to Elasticsearch!")

pub_credentials = pika.PlainCredentials(PUBLISHER_USER, PUBLISHER_PASS)
pub_connection = pika.BlockingConnection(
    pika.ConnectionParameters(host=RABBITMQ_HOST, credentials=pub_credentials)
)
pub_channel = pub_connection.channel()
pub_channel.queue_declare(queue=RABBITMQ_QUEUE, durable=True)
print("Publisher connected to RabbitMQ!")

cons_credentials = pika.PlainCredentials(CONSUMER_USER, CONSUMER_PASS)
cons_connection = pika.BlockingConnection(
    pika.ConnectionParameters(host=RABBITMQ_HOST, credentials=cons_credentials)
)
print("Consumer connection established!")


def load_apis(file_path="apis.json"):
    with open(file_path, "r") as f:
        return json.load(f)


def make_request(url, timeout=10):
    try:
        start_time = time.time()
        response = requests.get(url, timeout=timeout)
        duration = round(time.time() - start_time, 2)
        return response.status_code, duration, None
    except requests.exceptions.Timeout:
        return None, None, f"Timeout after {timeout}s"
    except requests.exceptions.ConnectionError as e:
        if "Name or service not known" in str(e) or "getaddrinfo failed" in str(e):
            return None, None, "DNS resolution failed"
        return None, None, f"Connection error: {str(e)}"
    except requests.exceptions.RequestException as e:
        return None, None, f"Request error: {str(e)}"


def send_to_rabbitmq(message):
    if pub_channel and pub_connection.is_open:
        try:
            pub_channel.basic_publish(
                exchange="",
                routing_key=RABBITMQ_QUEUE,
                body=json.dumps(message),
                properties=pika.BasicProperties(delivery_mode=2)
            )
        except Exception as e:
            print(f"Failed to send message to RabbitMQ: {e}")


def send_to_elasticsearch(index, doc):
    try:
        if es.ping():
            es.index(index=index, document=doc)
        else:
            print("Elasticsearch connection lost!")
    except Exception as e:
        print(f"Failed to send to Elasticsearch: {e}")


def log_api(name, status, duration, error):
    msg = {
        "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "api_name": f"{name}",
        "status": status,
        "response_time": duration,
        "error": error
    }
    send_to_rabbitmq(msg)
    send_to_elasticsearch(index="api_logs", doc=msg)
    print(f"Logged: {msg}")
    print("-----")


def start_consumer():
    def callback(ch, method, properties, body):
        message = json.loads(body)
        print(f"Consumed: {message}")

        time.sleep(1)  # delay 1 second
        ch.basic_ack(delivery_tag=method.delivery_tag)

    consumer_channel = cons_connection.channel()
    consumer_channel.queue_declare(queue=RABBITMQ_QUEUE, durable=True)
    consumer_channel.basic_qos(prefetch_count=1)
    consumer_channel.basic_consume(queue=RABBITMQ_QUEUE, on_message_callback=callback)
    print("Consumer started...")
    consumer_channel.start_consuming()


consumer_thread = threading.Thread(target=start_consumer, daemon=True)
consumer_thread.start()


run_count = 0
MAX_RUNS = 2

while run_count < MAX_RUNS:
    run_count += 1
    print(f"\n=== Running API test cycle #{run_count} at {datetime.now()} ===")
    api_list = load_apis()
    for name, url in api_list.items():
        status, duration, error = make_request(url)
        log_api(name, status, duration, error)

    summary = {
        "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "run_number": run_count,
        "total_apis": len(api_list)
    }
    send_to_rabbitmq(summary)
    send_to_elasticsearch(index="api_summary", doc=summary)
    print(f"--- Completed run #{run_count} ---\n")
    time.sleep(5)

print("Max runs reached. Connections remain open. Press Ctrl+C to exit.")

try:
    while True:
        time.sleep(1)
except KeyboardInterrupt:
    print("\nStopping script manually.")
finally:
    if pub_connection.is_open:
        pub_channel.close()
        pub_connection.close()
    if cons_connection.is_open:
        cons_connection.close()
    print("RabbitMQ connections closed.")
