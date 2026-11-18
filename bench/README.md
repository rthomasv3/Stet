# Benchmarks

How fast Stet loads and checks, measured with [BenchmarkDotNet](https://benchmarkdotnet.org) on the
normal .NET runtime and compiled with Native AOT.

## Results for Stet 0.0.1

Measured on:

- AMD Ryzen 9 9950X (16 cores, 32 threads), 64 GB of memory
- Fedora Linux 44
- .NET 10

The text is the first 100,000 words of a public-domain novel from Project Gutenberg, an older book
with long paragraphs. Stet checks on a single thread, so the number of cores does not help. Expect
slower times on a laptop.

### Starting up

The first use of Stet in a process. Each figure is from 20 separate launches with no warm-up, so it
includes loading the built-in data and, on the normal runtime, compiling the code as it first runs.

| | .NET 10 | Native AOT |
|---|---:|---:|
| Create the first checker | 44 ms | 13 ms |
| Create it and run the first check | 87 ms | 15 ms |

### Checking text for the first time

What opening a scene or a book in an app costs. Every measurement starts with a new checker. The
checker has a word list for the book's names, as a writer's would.

| | .NET 10 | Native AOT |
|---|---:|---:|
| 1,500-word scene | 4.3 ms | 1.8 ms |
| 5,000-word chapter | 11 ms | 4.6 ms |
| 100,000-word novel | 80 ms | 87 ms |
| 100,000-word novel, no word list | 102 ms | 112 ms |

With no word list every name in the book is unknown, and Stet works out suggestions for each.

The .NET 10 scene and chapter figures vary more from run to run than the others.

### Checking text again

A checker remembers what it worked out for the unknown words it has met, so text it has seen before
checks faster. This is the case while a writer works in a document that is already open.

| | .NET 10 | Native AOT |
|---|---:|---:|
| One paragraph | 8 µs | 11 µs |
| 1,500-word scene | 0.7 ms | 0.9 ms |
| 5,000-word chapter | 2.5 ms | 3.0 ms |
| 100,000-word novel | 49 ms | 59 ms |
| 100,000-word novel, no word list | 48 ms | 57 ms |
| Recheck a novel after one paragraph changed, through `CheckCache` | 2.0 ms | 2.0 ms |

### Other operations

| | .NET 10 | Native AOT |
|---|---:|---:|
| Create a checker, after the first | 14 ms | 14 ms |
| Suggestions for one misspelled word | 0.43 ms | 0.44 ms |
| Check a sentence with an unknown name in it | 0.44 ms | 0.45 ms |
| Replace a 20,000-word word list | 1.1 ms | 1.3 ms |

Native AOT starts three to six times faster. Once running it is about 20% slower at checking.

## Running them

From the repository root, on a text of your own:

    dotnet run -c Release --project bench/Stet.Bench -- --text /path/to/novel.txt --filter '*'

`--text` names a plain text file with one paragraph per line. The first 100,000 words are used. A word
list is made for it the way the [evaluation harness](../eval/README.md) makes one: every word Stet does
not know that the text uses twice or more.

Without `--text` the benchmarks use made-up sentences. Those repeat themselves far more than a book
does, so the document timings come out much faster. That is fine for comparing one change or one
machine with another, and it is not what the tables above show.

Everything else on the command line goes to BenchmarkDotNet. A full run takes about twelve minutes.
To run one group, name it in the filter:

| Filter | Measures |
|---|---|
| `'*ColdStart*'` | Starting up |
| `'*FirstCheckBenchmarks*'` | Checking text for the first time |
| `'*CheckerBenchmarks*'` | Checking text again, and the other operations |

The Native AOT runs need the tools Native AOT itself needs: see Microsoft's
[prerequisites](https://learn.microsoft.com/dotnet/core/deploying/native-aot/#prerequisites).

BenchmarkDotNet also reports the memory each operation allocates, and prints the machine it ran on.
