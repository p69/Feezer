module Feezer.Client.Renderer

open Fable.Import.Browser
open Fable.Core
open Fable.Core.JsInterop
open Feezer.Domain.Protocol
open Fable.Import
open Fable.Import.Electron

let body = document.getElementById("app")
body.textContent <- "Hello Feezer!"

let onSocketConnected (ws:WebSocket) =
    body.textContent <- "Socket connected"
    ws.send <| toJson Authozrize

let onMessageReceived (evt:MessageEvent) =
    let msg = ofJson<Server> !!evt.data
    match msg with
    | Authorization authUrl ->
        let currentWindow = electron.remote.getCurrentWindow()
        let options = createEmpty<BrowserWindowOptions>
        options.parent <- Some currentWindow
        options.modal <- Some true
        let authWindow = electron.remote.BrowserWindow.Create(options)
        authWindow.loadURL authUrl
    | Authorized token ->() //TODO

let ws = WebSocket.Create "ws://localhost:8080/fcon"
ws.addEventListener_open (fun _ -> !!onSocketConnected(ws))
ws.addEventListener_message !!onMessageReceived

