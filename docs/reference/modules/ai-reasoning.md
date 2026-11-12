# AIReasoning Module

Provides AI-powered root-cause analysis using the OpenAI API.

## RCA request schema

```json
{
  "agentId": "uuid",
  "anomalyType": "string",
  "timeWindowStart": "ISO8601",
  "timeWindowEnd": "ISO8601"
}
```

## RCA response

```json
{
  "rootCause": "string",
  "confidence": 0.87,
  "evidence": ["string"],
  "contributingFactors": ["string"],
  "recommendedActions": ["string"]
}
```
