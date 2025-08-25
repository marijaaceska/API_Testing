import os
import requests
import time
import schedule
import json
import pika
import atexit
from datetime import datetime
from dotenv import load_dotenv
from elasticsearch import Elasticsearch
import urllib3


urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

load_dotenv()
ES_HOST = os.getenv("ES_HOST")
ES_USER = os.getenv("ES_USER")
ES_PASSWORD = os.getenv("ES_PASSWORD")

try:
    es = Elasticsearch(
        [ES_HOST],
        basic_auth=(ES_USER, ES_PASSWORD),
        verify_certs=False
    )
    if es.ping():
        print("Connected to Elasticsearch!")
    else:
        print("Could not connect to Elasticsearch!")
except Exception as e:
    print(f"Elasticsearch connection failed: {e}")

RABBITMQ_QUEUE = "api_logs"

try:
    connection = pika.BlockingConnection(pika.ConnectionParameters("localhost"))
    channel = connection.channel()
    channel.queue_declare(queue=RABBITMQ_QUEUE, durable=True)
except Exception as e:
    print(f"Failed to connect to RabbitMQ: {str(e)}")
    connection = None
    channel = None

@atexit.register
def close_rabbitmq():
    if connection and connection.is_open:
        channel.close()
        connection.close()
        print("RabbitMQ connection closed.")

def send_to_rabbitmq(message):
    if channel:
        try:
            channel.basic_publish(
                exchange="",
                routing_key=RABBITMQ_QUEUE,
                body=json.dumps(message),
                properties=pika.BasicProperties(delivery_mode=2)
            )
        except Exception as e:
            print(f"Failed to send message to RabbitMQ: {str(e)}")

def send_to_elasticsearch(index, doc):
    try:
        es.index(index=index, document=doc)
    except Exception as e:
        print(f"Failed to send to Elasticsearch: {e}")

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
        else:
            return None, None, f"Connection error: {str(e)}"
    except requests.exceptions.RequestException as e:
        return None, None, f"Request error: {str(e)}"

def log_result(name, status, duration, error):
    msg = {
        "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "api_name": name,
        "status": status,
        "response_time": duration,
        "error": error
    }
    send_to_rabbitmq(msg)
    send_to_elasticsearch(index="api_logs", doc=msg)

    if status == 200:
        print(f"{name:20} | Status: 200 | Response Time: {duration:.2f}s")
    elif status and 400 <= status < 500:
        print(f"{name:20} | Status: {status} Client Error | Response Time: {duration:.2f}s")
    elif status and 500 <= status < 600:
        print(f"{name:20} | Status: {status} Server Error | Response Time: {duration:.2f}s")
    elif error:
        print(f"{name:20} | Error: {error}")

def update_summary(summary, status, error):
    if status == 200:
        summary["success"] += 1
    elif status and 400 <= status < 500:
        summary["client_error"] += 1
    elif status and 500 <= status < 600:
        summary["server_error"] += 1
    elif error:
        summary["failures"] += 1
    return summary

run_count = 0

def run_tests():
    global run_count
    run_count += 1
    api_list = load_apis()
    print(f"\n=== Running tests at {datetime.now()} (Run #{run_count}) ===")

    summary = {"success": 0, "client_error": 0, "server_error": 0, "failures": 0}

    for name, url in api_list.items():
        status, duration, error = make_request(url)
        log_result(name, status, duration, error)
        summary = update_summary(summary, status, error)

    summary_doc = {
        "timestamp": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
        "run_number": run_count,
        "success": summary["success"],
        "client_error": summary["client_error"],
        "server_error": summary["server_error"],
        "failures": summary["failures"]
    }

    send_to_rabbitmq(summary_doc)
    send_to_elasticsearch(index="api_summary", doc=summary_doc)

    print(f"\nRun #{run_count} Summary: Successes: {summary['success']}, "
          f"Client Errors: {summary['client_error']}, "
          f"Server Errors: {summary['server_error']}, "
          f"Failures: {summary['failures']}")

    return schedule.CancelJob


schedule.every(30).seconds.do(run_tests)

run_tests()

try:
    while True:
        schedule.run_pending()
        time.sleep(1)
except KeyboardInterrupt:
    print("\nScript stopped by user.")
finally:
    if connection and connection.is_open:
        try:
            channel.close()
            connection.close()
            print("RabbitMQ connection closed cleanly.")
        except Exception as e:
            print(f"Error closing RabbitMQ: {e}")
