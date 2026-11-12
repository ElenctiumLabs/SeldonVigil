# Known Gaps

This is a work-in-progress platform. The following are known gaps as of **November 2026**.

| Severity | Gap | Notes |
|---|---|---|
| Medium | Semantic memory search | `SearchAgentMemoryAsync` returns recent episodic memories with a fixed score rather than ranking by semantic similarity |
| Medium | Behavioural patterns | `GetBehavioralPatternsAsync` returns an empty list; pattern detection is not implemented |
| Medium | Saga recovery | The recovery service compensates failed sagas; automatic re-execution is not implemented |
| Low | Prompt/evaluation surface | Prompts and evaluation results are persisted, but there is no API or UI surface for them yet |
| Low | CLI completeness | The command surface is registered; some handlers are thin |
| Low | API versioning | Only `/api/v1/` exists; no versioning strategy beyond v1 |
