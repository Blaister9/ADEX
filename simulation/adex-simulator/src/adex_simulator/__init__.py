"""ADEX offline simulator.

Reserved for simulation, offline experimentation and evaluation. It is never in
the online request path: production decisions are made by the C# service
(ADR-0003). It reads exported data and writes reports; it never writes
production state.
"""

from adex_simulator.environment import BernoulliEnvironment
from adex_simulator.policies import Selection, UniformRandomPolicy
from adex_simulator.runner import SYNTHETIC_BANNER, RunResult, SimulationRunner, format_report
from adex_simulator.scenarios import SCENARIOS, Scenario
from adex_simulator.seeds import derive_seed

__all__ = [
    "SCENARIOS",
    "SYNTHETIC_BANNER",
    "BernoulliEnvironment",
    "RunResult",
    "Scenario",
    "Selection",
    "SimulationRunner",
    "UniformRandomPolicy",
    "derive_seed",
    "format_report",
]
