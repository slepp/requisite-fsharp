# Contributing

Keep the API small and F#-native. Preserve `.fsi` abstractions, add focused
runtime or compiler-contract coverage, and avoid helper dependencies when
`Result`, records, functions, or DUs suffice.

Before opening a pull request, run the locked restore, Fantomas check, Release
build, both test targets, and pack commands from the README. Contributions are
accepted under the repository's MIT OR Apache-2.0 terms.

After changing the FSharp.Core floor, delete `bin`/`obj` and restore with a
clean package cache (or a fresh `NUGET_PACKAGES` directory) before judging
compatibility; a warm cache can hide resolution mistakes.
