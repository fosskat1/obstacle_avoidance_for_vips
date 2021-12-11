from collections import defaultdict, deque
from pathlib import Path
from typing import Iterable, List, Mapping, NamedTuple, Tuple
import math

import rtamt
from rtamt import STLDenseTimeSpecification

def _prepare_spec() -> STLDenseTimeSpecification:
    spec = STLDenseTimeSpecification()
    # spec.set_sampling_period(500, "ms", 0.1)
    spec.declare_const("t1", "float", "3.0")

    spec.declare_var("t", "float")

    return spec

def _parse_and_eval_spec(spec: STLDenseTimeSpecification) -> float:
    try:
        spec.parse()
    except rtamt.STLParseException as e:
        logger.critical("STL Parse Exception: {}".format(e))
        sys.exit(1)

    l_t = [(i, (float)(i)) for i in range(10)]

    return spec.evaluate(
        ["t", l_t]
    )

def check_t():
    spec = _prepare_spec()

    spec.name = "t should be greater than t1"
    spec.spec = "eventually (t > t1)"

    return _parse_and_eval_spec(spec)

def main():
    check = check_t()

    print("Robustness for t {}".format(check))

if __name__ == "__main__":
    main()