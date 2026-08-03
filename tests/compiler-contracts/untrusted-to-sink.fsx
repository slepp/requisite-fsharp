open Requisite

let trustedSink (_: Tainted<string, Trusted>) = ()

let raw = Tainted.fromInput "external input"
trustedSink raw
