namespace Requisite.Tests

open System
open System.Diagnostics
open System.IO
open Requisite
open Xunit

module CompileTests =
    type private CompileContract =
        { Script: string
          ExpectedCode: string
          WarnAsError: int option }

    let private runScript scriptName warnAsError =
        let scriptPath =
            Path.Combine(AppContext.BaseDirectory, "compiler-contracts", scriptName)

        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "dotnet"
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.ArgumentList.Add("fsi")
        startInfo.ArgumentList.Add("--nologo")
        startInfo.ArgumentList.Add("--noninteractive")
        startInfo.ArgumentList.Add("--exec")
        startInfo.ArgumentList.Add($"--reference:{typeof<Trusted>.Assembly.Location}")

        warnAsError
        |> Option.iter (fun warning -> startInfo.ArgumentList.Add($"--warnaserror:{warning}"))

        startInfo.ArgumentList.Add(scriptPath)

        use childProcess = new Process()
        childProcess.StartInfo <- startInfo
        Assert.True(childProcess.Start(), $"failed to start F# Interactive for {scriptName}")

        let stdout = childProcess.StandardOutput.ReadToEndAsync()
        let stderr = childProcess.StandardError.ReadToEndAsync()

        if not (childProcess.WaitForExit(TimeSpan.FromSeconds(30.0))) then
            childProcess.Kill(true)
            failwithf "F# Interactive timed out for %s" scriptName

        childProcess.ExitCode, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult()

    [<Fact>]
    let ``public compiler contracts reject invalid programs with expected diagnostics`` () =
        let contracts =
            [ { Script = "untrusted-to-sink.fsx"
                ExpectedCode = "FS0001"
                WarnAsError = None }
              { Script = "forge-certain.fsx"
                ExpectedCode = "FS0800"
                WarnAsError = None }
              { Script = "forge-thresholds.fsx"
                ExpectedCode = "FS0800"
                WarnAsError = None }
              { Script = "confidence-as-bool.fsx"
                ExpectedCode = "FS0001"
                WarnAsError = None }
              { Script = "gate-equality.fsx"
                ExpectedCode = "FS0001"
                WarnAsError = None }
              { Script = "tainted-equality.fsx"
                ExpectedCode = "FS0001"
                WarnAsError = None }
              { Script = "non-exhaustive-gate.fsx"
                ExpectedCode = "FS0025"
                WarnAsError = Some 25 } ]

        for contract in contracts do
            let exitCode, output = runScript contract.Script contract.WarnAsError
            Assert.NotEqual(0, exitCode)
            Assert.Contains($"error {contract.ExpectedCode}:", output, StringComparison.Ordinal)

    [<Fact>]
    let ``positive compiler control succeeds`` () =
        let exitCode, output = runScript "positive-control.fsx" (Some 25)
        Assert.True((exitCode = 0), output)
        Assert.DoesNotContain("error FS", output, StringComparison.Ordinal)
