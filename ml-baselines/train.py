"""Reproducible classification and regression baselines; no production data required."""
import argparse
import json
from pathlib import Path

from sklearn.datasets import make_classification, make_regression
from sklearn.dummy import DummyClassifier, DummyRegressor
from sklearn.linear_model import LinearRegression, LogisticRegression
from sklearn.metrics import accuracy_score, precision_recall_fscore_support, mean_absolute_error, mean_squared_error, r2_score
from sklearn.model_selection import train_test_split
from sklearn.pipeline import make_pipeline
from sklearn.preprocessing import StandardScaler

SEED = 42


def split_data(X, y):
    X_train, X_temp, y_train, y_temp = train_test_split(
        X, y, test_size=0.30, random_state=SEED, stratify=y if len(set(y)) <= 2 else None
    )
    X_val, X_test, y_val, y_test = train_test_split(
        X_temp, y_temp, test_size=0.50, random_state=SEED,
        stratify=y_temp if len(set(y_temp)) <= 2 else None
    )
    return X_train, X_val, X_test, y_train, y_val, y_test


def metrics(task, actual, predicted):
    if task == "classification":
        precision, recall, f1, _ = precision_recall_fscore_support(
            actual, predicted, average="binary", zero_division=0
        )
        return {"accuracy": float(accuracy_score(actual, predicted)),
                "precision": float(precision), "recall": float(recall), "f1": float(f1)}
    return {"mae": float(mean_absolute_error(actual, predicted)),
            "rmse": float(mean_squared_error(actual, predicted) ** 0.5),
            "r2": float(r2_score(actual, predicted))}


def run(task):
    if task == "classification":
        X, y = make_classification(n_samples=600, n_features=8, n_informative=5,
                                   n_redundant=1, random_state=SEED)
        models = {"dummy": DummyClassifier(strategy="most_frequent"),
                  "baseline": make_pipeline(StandardScaler(), LogisticRegression(max_iter=1000, random_state=SEED))}
    else:
        X, y = make_regression(n_samples=600, n_features=8, n_informative=5,
                               noise=15.0, random_state=SEED)
        models = {"dummy": DummyRegressor(strategy="mean"),
                  "baseline": make_pipeline(StandardScaler(), LinearRegression())}
    X_train, X_val, X_test, y_train, y_val, y_test = split_data(X, y)
    results = {"task": task, "seed": SEED, "data": "synthetic",
               "splits": {"train": len(y_train), "validation": len(y_val), "test": len(y_test)},
               "models": {}}
    for name, model in models.items():
        model.fit(X_train, y_train)
        results["models"][name] = {
            "validation": metrics(task, y_val, model.predict(X_val)),
            "test": metrics(task, y_test, model.predict(X_test))}
    return results


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--task", choices=["classification", "regression", "all"], default="all")
    parser.add_argument("--output", type=Path, default=Path("ml-baselines/results/baselines.json"))
    args = parser.parse_args()
    tasks = ["classification", "regression"] if args.task == "all" else [args.task]
    results = {task: run(task) for task in tasks}
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(results, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
