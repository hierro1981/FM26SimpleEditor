"""Optional progress callbacks; JSON events precede the final response."""
import json
import sys
import time


def report(callback, percent, phase, done=None, total=None, unit=""):
    if callback is not None:
        callback(percent, phase, done, total, unit)


def scaled(callback, start, end):
    if callback is None:
        return None
    return lambda percent, phase, done=None, total=None, unit="": callback(
        start + (end-start)*max(0, min(100, percent))/100, phase, done, total, unit)


class ProgressReporter:
    def __init__(self, stream=None):
        self.stream = sys.stdout if stream is None else stream
        self.percent = 0
        self.phase = ""
        self.last_time = 0

    def __call__(self, percent, phase, done=None, total=None, unit=""):
        value = max(self.percent, min(100, int(percent)))
        now = time.monotonic()
        finished_counter = total is not None and done == total
        if phase == self.phase and value < 100 and not finished_counter and now-self.last_time < .08:
            return
        self.percent, self.phase, self.last_time = value, phase, now
        event = dict(event="progress", percent=value, phase=phase, done=done, total=total, unit=unit)
        print(json.dumps(event, ensure_ascii=True, separators=(",", ":")), file=self.stream, flush=True)
