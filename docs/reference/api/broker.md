# Broker Endpoints

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/v1/broker/status` | Broker status — reachable, counts |
| GET | `/api/v1/broker/topics` | List all topics |
| GET | `/api/v1/broker/consumer-groups` | Consumer groups + lag |
| GET | `/api/v1/broker/topics/{name}/messages` | Browse the last 20 messages on a topic |
| GET | `/api/v1/broker/config` | Static broker configuration |
