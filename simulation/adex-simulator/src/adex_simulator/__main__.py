"""Command-line entry point: ``python -m adex_simulator``."""

from __future__ import annotations

import argparse
import sys

from adex_simulator import scenarios
from adex_simulator.runner import SimulationRunner, format_report


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="adex_simulator",
        description="Run an ADEX policy against a synthetic environment. Offline only.",
    )
    parser.add_argument(
        "--scenario",
        default="services-clear-winner",
        choices=sorted(scenarios.SCENARIOS),
        help="Named scenario to run.",
    )
    parser.add_argument(
        "--rounds", type=int, default=10_000, help="Number of decisions to simulate."
    )
    parser.add_argument(
        "--seed",
        type=int,
        default=20260722,
        help="Seed for the environment. The same seed always reproduces the same run.",
    )
    parser.add_argument(
        "--list", action="store_true", help="List the available scenarios and exit."
    )
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)

    if args.list:
        for name, scenario in sorted(scenarios.SCENARIOS.items()):
            print(f"{name:<24}{scenario.description}")
        return 0

    scenario = scenarios.get(args.scenario)
    result = SimulationRunner().run(scenario, rounds=args.rounds, seed=args.seed)
    print(format_report(scenario, result))
    return 0


if __name__ == "__main__":
    sys.exit(main())
