Quackers - a cute, noisy console logger for dotnet test
---

![A duck quacking loudly](quack.png "A duck quacking loudly")

_[ image credits: https://www.instagram.com/adrawingattempt ]_

Why?
---

The standard console logger doesn't summarise errors after a test run. If you have several
thousand tests, you'll have to scroll back up through them to get to the source of problems ):

Quackers outputs errors again at the end of the test. It also outputs fully-qualified
test names, not just the name of the test method, so you can better tell what's going
on from looking at the logs.

Usage
---

1. install `Quackers.TestLogger` into your test project(s)
2. invoke `dotnet test {project} -l quackers`
3. configure either 
    - via logger properties (eg `dotnet test -l "quackers:passlabel=ok;faillabel=fail"`)
        - passlabel :: string
        - faillabel :: string
        - nonelabel :: string
        - skiplabel :: string
        - notfoundlabel :: string
        - nocolor :: boolean
        - theme :: string ("default" or "darker" - depending on where you want to run tests, a darker theme may be more comfortable)
        - highlight_slow_tests :: boolean
        - slow_test_threshold_ms :: integer
        - debug_log_file :: string (path)
        - show_timestamps :: boolean
        - verbose_summary :: boolean
        - output_failures_inline :: boolean
        - show_help :: boolean
        - summary_start_marker :: string
        - summary_complete_marker :: string
        - failure_start_marker :: string
        - slow_summary_start_marker :: string
        - slow_summary_complete_marker :: string
        - log_prefix :: string
        - test_name_prefix :: string
        - failure_index_placeholder :: string
        - slow_index_placeholder :: string
    - or by environment variables, all the above values, but prefixed with `QUACKERS_`, eg
        - QUACKERS_PASSLABEL
        - QUACKERS_FAILLABEL
        - QUACKERS_NONELABEL
        - QUACKERS_SKIPLABEL
        - QUACKERS_NOTFOUNDLABEL
        - QUACKERS_NOCOLOR
        - ... etc

Configuration options
---
- Configuration keys are case-insensitive and tolerant of the characters: `-`, `_`, `.`
- boolean values may be set via truthy / falsey strings (case-insensitive):
    - truthy:
        - "yes"
        - "1"
        - "true"
        - "on"
        - "enable"
    - falsey:
        - "no"
        - "0"
        - "false"
        - "off"
        - "disable"

This means that the following are equivalent:

```bash
QUACKERS_SHOW_VERBOSE_SUMMARY=1 dotnet test Tests.csproj --logger quackers
QUACKERS_showverbosesummary=true dotnet test Tests.csproj --logger quackers
dotnet test Tests.csproj --logger 'quackers;verboseSummary=yes'
```

During testing, I've found some ocassions where the test host doesn't pass through
all the configuration parameters I've specified on the line (perhaps due to some
strange shell quoting issue). If you find that quackers is not obeying your configuration,
try setting the appropriate environment variables instead.

What do the config parameters do?
---

### Styling the output:
- Pass Label: the label to apply when a test passes (default ✅)
- Fail Label: the label to apply when a test fails (default 🛑)
- None Label: the label to apply when the test host provides None for a test result (default ❓)
- Skip Label: the label to apply when a test is skipped (default 🚫)
- Not Found Label: the label to apply when a test is not found (default 🤷)
- NoColor: disable coloring of the output altogether (default false)
- Theme: "default" or "darker", depending on where your tests are displayed
  - in the console, "darker" may be more pleasant to work with
  - on a web page with a white background (eg Jenkins CI), the default theme may be easier to read

### Controlling what is output:
- Highlight Slow Tests: when a test takes "too long", highlight it and include it in a summary at the end (default false)
- Slow Test Threshold Ms: when a test exceeds this time in milliseconds, mark it as slow (default 1 second / 1000ms)
- Show Timestamps: include a timestamp in the output for each test - useful for really long-running test processes, when you stumble across them and are trying to figure out how long long things are taking, but adds more noise to each test line (default off)
- Verbose Summary: show a nicely-formatted summary of passes, failures, skips, etc, and time taken, at the end of the test run. Note that since the default dotnet console logger cannot be removed or completely stilled, you will see that summary too ): See the section about zarro if you want pretty test output...
- Show Help: dump out some help text at the start of the test run (eg what can be configured) (default false)
- Test Name Prefix: prefix all logs for this run with this string (useful if you have, eg, two test projects which observe the same source files, for different targeted runtims)

### Markings to make the output easier to consume and collate
Since the inbuilt console logger that comes with the dotnet test host apparently
can't be completely disabled, and since running tests in parallel is no longer
supported via easy commandline, my own orchestration in [zarro](https://github.com/fluffynuts/zarro)
injects some markers to make it easier to ingest the outputs:

- Summary Start Marker - printed on a line by itself before the overall test summary is displayed
- Summary Complete Marker - printed on a line by itself after the summary is complete
- Failure Start Marker - printed within the summary to denote when failure recaps start
- Slow Summary Start Marker - printed just before starting on the slow test summary
- Slow Summary Complete Marker - printed after the slow test summary is complete
- Log Prefix - printed before each and every log line so that the consumer can distinguish between output from quackers and other loggers (or other IO from the test host)
- Failure Index Placeholder - when consolidating results from multiple test runs in parallel, this is output instead of the numeric index of the failed test so that the collector can re-index the tests. Eg if we had 2 assemblies and both had one failure, we'd like to see the failures prefixed with [1] and [2], instead of [1] and [1] - so the placeholder is printed out and the consumer can keep track of the total count
- Slow Index Placeholder - as above, but for slow tests

Credits
---
Quackers.TestLogger uses the [Pastel](https://github.com/skillfire/Pastel) console coloring
library to look pretty. Pastel is inlined via a submodule at the time of building Quackers.