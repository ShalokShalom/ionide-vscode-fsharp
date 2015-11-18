namespace Ionide.VSCode.FSharp

open System
open FunScript
open FunScript.TypeScript
open FunScript.TypeScript.vscode
open FunScript.TypeScript.child_process

open DTO
open Ionide.VSCode.Helpers

[<ReflectedDefinition>]
module Fsi =
    let mutable fsiProcess : ChildProcess option = None
    let mutable fsiOutput : OutputChannel option = None

    let private handle (data : obj) =
        if data <> null then
            let response = data.ToString().Replace("\\","\\\\")
            fsiOutput |> Option.iter (fun fo -> fo.append response |> ignore)

    let private start () =
        fsiProcess |> Option.iter(fun fp -> fp.kill ())
        fsiProcess <-
            (if Process.isWin () then Process.spawn "Fsi.exe" "" "" else Process.spawn "fsharpi" "" "")
            |> Process.onExit (fun _ -> fsiOutput |> Option.iter (fun fo -> fo.clear () ))
            |> Process.onOutput handle
            |> Some
        fsiOutput <-
            window.Globals.createOutputChannel("F# Interactive")
            |> Some
        fsiOutput |> Option.iter (fun fo -> fo.show (2 |> unbox) )

    let private send (msg : string) =
        if fsiProcess.IsNone then start ()
        let msg = msg.Replace("\uFEFF", "") + ";;\n"
        fsiOutput |> Option.iter (fun fo -> fo.append msg)
        fsiProcess |> Option.iter (fun fp -> fp.stdin.write(msg, "utf-8" |> unbox) |> ignore)

    let private sendLine () =
        let editor = window.Globals.activeTextEditor
        let pos = editor.selection.start
        let line = editor.document.lineAt pos
        send line.text

    let private sendSelection () =
        let editor = window.Globals.activeTextEditor
        let range = Range.Create (editor.selection.anchor, editor.selection.active)
        editor.document.getText range |> send

    let private sendFile () =
        let editor = window.Globals.activeTextEditor
        editor.document.getText () |> send

    let activate (disposables: Disposable[]) =
        commands.Globals.registerCommand("fsi.Start", start |> unbox) |> ignore
        commands.Globals.registerCommand("fsi.SendLine", sendLine |> unbox) |> ignore
        commands.Globals.registerCommand("fsi.SendSelection", sendSelection |> unbox) |> ignore
        commands.Globals.registerCommand("fsi.SendFile", sendFile |> unbox) |> ignore
