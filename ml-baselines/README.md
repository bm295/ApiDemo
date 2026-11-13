# 03.3 — Train a baseline for each ML task

This is an **isolated learning MVP** alongside the .NET 10 API demo, not a production inference endpoint. No existing API contracts or application dependencies are changed.

## Setup and run

From the repository root:

```bash
python -m venv .venv
# Activate .venv for your shell
python -m pip install -r ml-baselines/requirements.txt
python ml-baselines/train.py --task all
cd ml-baselines && python -m unittest -v
```

The training command writes `ml-baselines/results/baselines.json` (ignored by Git).

## Experiment design

- **Classification:** binary synthetic labels, majority-class dummy comparator vs. standardized logistic regression; accuracy, precision, recall, F1.
- **Regression:** synthetic continuous targets, mean dummy comparator vs. standardized linear regression; MAE, RMSE, R².
- Fixed random seed 42; 70% training / 15% validation / 15% held-out test.
- Standardization is fitted **only on training data** through a pipeline, preventing validation/test leakage.
- Validation is intended for model selection. Held-out test results should only be consulted for final reporting; do not repeatedly tune against the test set.
- These generated datasets establish reproducible mechanics, **not** real-world predictive performance. Replace with domain datasets and add feature validation, missing-value handling and appropriate temporal/group splits before using operational data.

## Acceptance criteria

1. Both baseline tasks train from scratch without a running API server.
2. Dummy comparators and model metrics appear in JSON.
3. Tests verify split sizes, required metrics and reproducibility.
4. Existing .NET solution builds/tests independently.
