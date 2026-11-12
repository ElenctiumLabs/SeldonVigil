# Redpanda Operations Runbook

Redpanda is SeldonVigil's event broker. It implements the Kafka wire protocol, so the
`Confluent.Kafka` client stack is unchanged. Admin operations use `rpk` (the
Redpanda CLI).

**Local dev**: single-node container (`docker-compose.yml` service `redpanda`).
**Production**: self-managed cluster (TLS + SASL). No manifests are shipped; set the
`Broker:*` keys shown below and point `ConnectionStrings:Broker` at the cluster.

SeldonVigil's clients need no code change for a hardened broker — they read the
`Broker` config section:

| App config key | Value (production) | Notes |
|---|---|---|
| `ConnectionStrings:Broker` | `redpanda.redpanda.svc.cluster.local:9093` | unchanged key name |
| `Broker:SecurityProtocol` | `SASL_SSL` | or `SaslSsl` |
| `Broker:SaslMechanism` | `SCRAM-SHA-256` | or `ScramSha256` |
| `Broker:SaslUsername` | `seldonvigil-api` / `seldonvigil-workers` | per deployment, from Secret |
| `Broker:SaslPassword` | (Secret) | per deployment |
| `Broker:SslCaLocation` | `/etc/ssl/redpanda/ca.crt` | CA bundle mount |

Applies to the API producer + consumer factory, the Workers, the
`/api/v1/broker/*` admin/viewer clients, and the `/healthz` `broker` check.
`ConfigurationValidator` fails fast in Production if any TLS/SASL key is missing.

---

## Cluster operations

### Cluster status

```bash
rpk cluster info
```

Returns broker count, controller ID, Redpanda version, and uptime.

### Cluster health

```bash
rpk cluster health
```

Reports partition health, under-replicated partitions, leaderless partitions, and disk usage.

### View/set configuration

```bash
rpk cluster config list                          # list all properties
rpk cluster config get enable_consumer_group_metrics  # get a specific property
rpk cluster config set enable_consumer_group_metrics '["consumer_lag"]'  # set
```

Configuration changes take effect immediately for most properties (no restart needed).

---

## Topic operations

### List topics

```bash
rpk topic list
rpk topic list --detailed          # include partition/replica info
```

### Describe a topic

```bash
rpk topic describe TraceIngestedEvent
rpk topic describe TraceIngestedEvent --print-offsets    # show begin/end offsets
rpk topic describe TraceIngestedEvent --print-config     # show topic config
```

### Create a topic

```bash
rpk topic create TraceIngestedEvent -p 1 -r 1
```

Topics are auto-created by SeldonVigil's clients (`AllowAutoCreateTopics=true`). Manual
creation is only needed for pre-provisioning or non-default partition counts.

### Delete a topic

```bash
rpk topic delete TraceIngestedEvent
```

### Consume from a topic (debug)

```bash
rpk topic consume TraceIngestedEvent --num 10    # read last 10 messages
rpk topic consume TraceIngestedEvent -o start     # read from beginning
```

### Produce to a topic (debug)

```bash
echo '{"test":"payload"}' | rpk topic produce TraceIngestedEvent
```

---

## Consumer group operations

### List consumer groups

```bash
rpk group list
```

SeldonVigil uses groups `seldonvigil` (API) and `seldonvigil-workers` (Workers).

### Describe a consumer group

```bash
rpk group describe seldonvigil-workers
```

Shows members, assignments, and per-partition lag.

### Consumer lag inspection

```bash
rpk group describe seldonvigil-workers --print-offsets
```

Key columns:
- **CURRENT-OFFSET**: last committed offset for the partition
- **LOG-END-OFFSET**: broker's high watermark (latest offset)
- **LAG**: difference between log-end and current offset

For Prometheus-based lag monitoring, the alert uses:
```
redpanda_kafka_consumer_group_lag_sum{redpanda_group="seldonvigil-workers"} > 10000
```

---

## TLS/SASL connection flags

When connecting to a TLS/SASL-secured Redpanda cluster, prefix `rpk` commands with:

```bash
rpk <command> \
  -X brokers=<broker>:9093 \
  -X user=<username> \
  -X pass=<password> \
  -X sasl.mechanism=SCRAM-SHA-256 \
  -X tls.enabled=true \
  -X tls.ca=<path-to-ca-cert>
```

Or set environment variables:

```bash
export REDPANDA_BROKERS=<broker>:9093
export REDPANDA_SASL_USERNAME=<username>
export REDPANDA_SASL_PASSWORD=<password>
export REDPANDA_SASL_MECHANISM=SCRAM-SHA-256
export REDPANDA_TLS_ENABLED=true
export REDPANDA_TLS_CA=<path-to-ca-cert>
```

---

## Troubleshooting

### Consumer lag is high

1. Check group status: `rpk group describe seldonvigil-workers`
2. If lag is growing, verify workers are running and not crash-looping
3. Check broker health: `rpk cluster health`
4. Check for under-replicated partitions: `rpk cluster health` (look for `under-replicated: 0`)
5. Review worker logs for deserialization errors (messages land on DLQ topics like `TraceIngestedEvent.dlq`)

### DLQ messages

SeldonVigil forwards malformed messages to `{Topic}.dlq` (e.g., `TraceIngestedEvent.dlq`).

```bash
rpk topic consume TraceIngestedEvent.dlq --num 5
```

Common causes: schema mismatch, binary payload on a JSON topic.

### Broker unreachable

1. `docker compose ps`
2. Check logs: `docker compose logs redpanda`
3. Verify listeners: `rpk cluster info` should show the broker
4. For network issues, confirm `ConnectionStrings:Broker` points to the correct address

### Topic auto-creation not working

Redpanda enables auto-creation by default in dev mode. Verify:

```bash
rpk cluster config get auto_create_topics_enabled
```

Should return `true`. If not:

```bash
rpk cluster config set auto_create_topics_enabled true
```

---

## Useful endpoints

| Endpoint | URL | Purpose |
|---|---|---|
| Broker API | `localhost:9092` | Produce/consume (Confluent.Kafka clients) |
| Admin API | `localhost:9644` | `rpk` commands, metrics |
| Public metrics | `localhost:9644/public_metrics` | Prometheus scrape target |
| Internal metrics | `localhost:9644/metrics` | Detailed internal metrics (high cardinality) |
