Quackers - a cute, noisy console logger for dotnet test
---

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
        - nocolor :: boolean (yes, no, true, false, 1, 0)
    - or by environment variables, all starting with `QUACKERS_`
        - QUACKERS_PASSLABEL
        - QUACKERS_FAILLABEL
        - QUACKERS_NONELABEL
        - QUACKERS_SKIPLABEL
        - QUACKERS_NOTFOUNDLABEL
        - QUACKERS_NOCOLOR