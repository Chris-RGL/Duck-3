---
name: separate-config-files
description: Each ML-Agents behavior gets its own YAML config file rather than being added to an existing one
metadata:
  type: feedback
---

Don't append new ML-Agents behaviors to an existing trainer config YAML. Create a new file per behavior (e.g., `rocket_config.yaml` for the Rocket behavior).

**Why:** User explicitly rejected editing `trainer_config.yaml` and asked for a separate file.

**How to apply:** When adding a new ML-Agents training behavior, always create `config/<behavior_name>_config.yaml` instead of modifying an existing config.
