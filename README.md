# FictionChecker (working title)

An offline spelling and grammar checker for fiction, written in pure managed C#.
Early development; nothing is usable yet.

## Layout

- `src/FictionChecker` - the library (no runtime dependencies, Native AOT safe)
- `src/FictionChecker.DataBuilder` - builds the embedded data files from recorded sources
- `tests/FictionChecker.Tests` - unit tests
- `tests/FictionChecker.AotTest` - small app published with Native AOT as a check
- `eval/` - evaluation harness (corpora stay outside the repo)
- `bench/` - benchmarks

## Commands

    dotnet build
    dotnet test
    dotnet publish tests/FictionChecker.AotTest -c Release -o aot-out && ./aot-out/FictionChecker.AotTest

## Licensing

MIT. Data sources are recorded in `SOURCES.md` and credited in `THIRD-PARTY-NOTICES.md`.
