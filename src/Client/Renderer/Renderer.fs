module Feezer.Client.Renderer



open Fable.Import.Browser
open Fable.Core
open Fable.Core.JsInterop
open Feezer.Domain.Protocol
open Feezer.Domain.User
open Fable.Import
open Fulma.Layouts
open System

module E = Fable.Import.Electron

let mutable private authPopup: E.BrowserWindow option = None

let body = document.getElementById("app")

body.textContent <- "Hello Feezer!"

let user = {name="blasd";id=1;avatar="dsad"}

let onSocketConnected (ws:WebSocket) =
    body.textContent <- "Socket connected"
    ws.send <| toJson Authozrize



let onMessageReceived (evt:MessageEvent) =
    let msg = ofJson<Server> !!evt.data
    match msg with
    | Authorization authUrl ->
        let currentWindow = electron.remote.getCurrentWindow()
        let options = createEmpty<E.BrowserWindowOptions>
        options.parent <- Some currentWindow
        options.modal <- Some true
        let authWindow = electron.remote.BrowserWindow.Create(options)
        authWindow.loadURL authUrl
        authPopup <- Some authWindow
    | Authorized expiration ->
        match expiration with
        | Never -> body.textContent <- "Token expiration: never"
        | Date date -> body.textContent <- sprintf "Token expiration: %A" date

        match authPopup with
        | Some popUp -> popUp.close()
        | None -> ()

let ws = WebSocket.Create "ws://localhost:8080/fcon"

ws.addEventListener_open (fun _ -> !!onSocketConnected(ws))

ws.addEventListener_message !!onMessageReceived



