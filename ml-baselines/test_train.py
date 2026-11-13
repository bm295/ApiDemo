import unittest
from train import run


class BaselineTests(unittest.TestCase):
    def test_classification(self):
        result = run("classification")
        self.assertEqual(result["splits"], {"train": 420, "validation": 90, "test": 90})
        self.assertIn("f1", result["models"]["baseline"]["test"])
        self.assertEqual(result, run("classification"))

    def test_regression(self):
        result = run("regression")
        self.assertEqual(result["splits"], {"train": 420, "validation": 90, "test": 90})
        self.assertIn("rmse", result["models"]["baseline"]["test"])
        self.assertEqual(result, run("regression"))


if __name__ == "__main__":
    unittest.main()
