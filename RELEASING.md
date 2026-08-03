# Releasing

1. Update `VersionPrefix` in `Directory.Build.props` and add the matching
   changelog entry. `AssemblyVersion` and `FileVersion` derive from it.
   Starting with the second release, also set `PackageValidationBaselineVersion`
   in the library project to the previous stable package version.
2. Run the locked restore, formatting, Release build, both test targets,
   package, package inspection, and example commands from the README.
3. Commit and push those changes, then create and push an exact `v<version>`
   tag, for example `v0.1.0`.
4. The release workflow verifies that the tag and package version match,
   rebuilds from lock files, tests, packs, and attests the artifacts. It
   publishes through the protected `nuget` environment when a key is
   configured; otherwise the validated artifacts remain available from the
   workflow run.

Configure the `nuget` environment with required reviewers and a
`NUGET_API_KEY` secret scoped to `Requisite.FSharp`. Never publish from an
untagged branch or by editing the workflow's version check.
