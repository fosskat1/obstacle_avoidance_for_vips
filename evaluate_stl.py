import csv
from collections import defaultdict, deque
from pathlib import Path
from typing import Iterable, List, Mapping, NamedTuple, Tuple
import math
import glob
import os

import rtamt
from rtamt import STLDenseTimeSpecification

Signal = Iterable[Tuple[float, float]]
Trace = Mapping[str, Signal]

def extract_trace(tracefile: Path) -> Trace:
    signals = [
    			"dummyX", "dummyY", "dummyZ",
    			"fireHydrantX", "fireHydrantY", "fireHydrantZ",
    			"stopSignX", "stopSignY", "stopSignZ",
    			"bikeX", "bikeY", "bikeZ",
    			"leftSidewalkBoundZ", "rightSidewalkBoundZ", "endSidewalkBoundX"
    	]
    trace = defaultdict(deque)  # type: Mapping[str, deque[Tuple[float, float]]]
    with open(tracefile, "r") as f:
        csv_file = csv.DictReader(f)
        for row in csv_file:
            for signal in signals:
                trace[signal].append((float(row["time"]), float(row[signal])))
    return trace

def _prepare_spec() -> STLDenseTimeSpecification:
	spec = STLDenseTimeSpecification()
	# spec.set_sampling_period(500, "ms", 0.1)
	spec.declare_const("sidewalk_safe_dist", "float", "0.2")
	spec.declare_const("obstacle_safe_dist", "float", "0.5")
	# spec.declare_const("sidewalk_length", "float", "0.0")
	# spec.declare_const("T", "float", "20.0")

	spec.declare_var("dist_covered", "float")
	spec.declare_var("left_sidewalk_dist", "float")
	spec.declare_var("right_sidewalk_dist", "float")
	spec.declare_var("fire_hydrant_dist", "float")
	spec.declare_var("stop_sign_dist", "float")
	spec.declare_var("bike_dist", "float")
	spec.declare_var("end_dist", "float")

	return spec

def _parse_and_eval_spec(spec: STLDenseTimeSpecification, trace: Trace) -> float:
	try:
	    spec.parse()
	except rtamt.STLParseException as e:
	    logger.critical("STL Parse Exception: {}".format(e))
	    sys.exit(1)

	num_timesteps = len(trace["dummyZ"])
	left_sidewalk_dist, right_sidewalk_dist = [], []

	fire_hydrant_dist, stop_sign_dist, bike_dist = [], [], []

	for idx in range(num_timesteps):
		ts = trace["dummyZ"][idx][0]

		dummy_x_ts = trace["dummyX"][idx][1]
		dummy_y_ts = trace["dummyY"][idx][1]
		dummy_z_ts = trace["dummyZ"][idx][1]

		fire_hydrant_x_ts = trace["fireHydrantX"][idx][1]
		fire_hydrant_y_ts = trace["fireHydrantY"][idx][1]
		fire_hydrant_z_ts = trace["fireHydrantZ"][idx][1]

		stop_sign_x_ts = trace["stopSignX"][idx][1]
		stop_sign_y_ts = trace["stopSignY"][idx][1]
		stop_sign_z_ts = trace["stopSignZ"][idx][1]

		bike_x_ts = trace["bikeX"][idx][1]
		bike_y_ts = trace["bikeY"][idx][1]
		bike_z_ts = trace["bikeZ"][idx][1]

		left_sidewalk_z_ts = trace["leftSidewalkBoundZ"][idx][1]
		right_sidewalk_z_ts = trace["rightSidewalkBoundZ"][idx][1]

		left_sidewalk_dist.append((ts, dummy_z_ts-left_sidewalk_z_ts))
		right_sidewalk_dist.append((ts, right_sidewalk_z_ts-dummy_z_ts))
		fire_hydrant_dist.append(
			(
				ts, _calculate_distance(
					dummy_x_ts,dummy_y_ts,dummy_z_ts,
					fire_hydrant_x_ts,fire_hydrant_y_ts,fire_hydrant_z_ts
				)
			)
		)
		stop_sign_dist.append(
			(
				ts, _calculate_distance(
					dummy_x_ts,dummy_y_ts, dummy_z_ts,
					stop_sign_x_ts,stop_sign_y_ts,stop_sign_z_ts
				)
			)
		)
		bike_dist.append(
			(
				ts, _calculate_distance(
					dummy_x_ts,dummy_y_ts, dummy_z_ts,
					bike_x_ts,bike_y_ts,bike_z_ts
				)
			)
		)

	return spec.evaluate(
		["dist_covered", list(trace["dummyX"])],
	    ["left_sidewalk_dist", left_sidewalk_dist],
	    ["right_sidewalk_dist", right_sidewalk_dist],
	    ["fire_hydrant_dist", fire_hydrant_dist],
	    ["stop_sign_dist", stop_sign_dist],
	    ["bike_dist", bike_dist],
	    ["end_dist", list(trace["endSidewalkBoundX"])],
	)

def _calculate_distance(x1, y1, z1, x2, y2, z2):
	return math.sqrt((x1-x2)**2 + (y1-y2)**2 + (z1-z2)**2)

def check_on_path(trace: Trace) -> float:
	spec = _prepare_spec()

	spec.name = "Check if person stays within sidewalk bounds"
	spec.spec = "always ((left_sidewalk_dist >= sidewalk_safe_dist) and (right_sidewalk_dist >= sidewalk_safe_dist))"

	return _parse_and_eval_spec(spec, trace)

def check_obstacle_avoidance(trace: Trace) -> float:
	spec = _prepare_spec()

	spec.name = "Check if person stays away from obstacles"
	spec.spec = "always ((fire_hydrant_dist >= obstacle_safe_dist) and (stop_sign_dist >= obstacle_safe_dist) and (bike_dist >= obstacle_safe_dist))"

	return _parse_and_eval_spec(spec, trace)

def check_reach_end(trace: Trace) -> float:
	spec = _prepare_spec()

	spec.name = "Check if person reaches the end of the sidewalk"
	spec.spec = "eventually (dist_covered <= end_dist)"

	return _parse_and_eval_spec(spec, trace)

def evaluate_tracefile(tracefile: Path):
    trace = extract_trace(tracefile)

    on_path = check_on_path(trace)
    print(
        "Robustness for `on_path` = {}".format(
            (on_path[0], on_path[-1])
        )
    )

    obstacle_avoidance = check_obstacle_avoidance(trace)
    print(
        "Robustness for `obstacle_avoidance` = {}".format(
            (obstacle_avoidance[0], obstacle_avoidance[-1])
        )
    )

    reach_end = check_reach_end(trace)
    print(
        "Robustness for `reach_end` = {}".format(
            (reach_end[0], reach_end[-1])
        )
    )


    # print("Robustness for `on_path` = ", on_path)
    # print("Robustness for `obstacle_avoidance` = ", obstacle_avoidance)
    # print("Robustness for `reach_end` = ", reach_end)

def main():
    # args = parse_args()

    # tracefiles = args.tracefiles  # type: List[Path]
    # for tracefile in tracefiles:
    #     print("===================================================")
    #     print("Evaluating trace file: %s", str(tracefile.relative_to(Path.cwd())))
    #     evaluate_tracefile(tracefile)
    #     print("===================================================")
    #     print()

    tracefile_dir = "Traces"

    tracefiles = glob.glob(os.path.join(tracefile_dir, "*.csv"))

    for tracefile in tracefiles:
	    print("===================================================")
	    print("Evaluating trace file: %s", tracefile)
	    evaluate_tracefile(tracefile)
	    print("===================================================")
	    print()


if __name__ == "__main__":
    main()
