# Messaging Module

Exposes broker introspection to the frontend — does not produce or consume events. All heavy broker work is in SeldonVigil.Workers.

## Endpoints

- `GET /api/v1/broker/status` — broker reachability, topic/partition/consumer counts
- `GET /api/v1/broker/topics` — list all topics
- `GET /api/v1/broker/consumer-groups` — consumer groups and lag
- `GET /api/v1/broker/topics/{name}/messages` — browse the last 20 messages on a topic
- `GET /api/v1/broker/config` — static broker configuration
