module Feezer.Client.Renderer.App

open Fable.Import.Browser
open Fable.Core
open Fable.Core.JsInterop
open Feezer.Domain.Protocol
open Feezer.Domain.User
open Fable.Import
open Fulma.Layouts
open Fulma.Elements
open System
open Feezer.Client.Renderer
open Fable.Helpers.React
module E = Fable.Import.Electron
open Elmish
open Elmish.React

type AuthorizedModel = {
    user:User
}

type Msg =
    | AnonymousMsg of Anonymous.Msg

type Model =
    | Anonymous of Anonymous.Model
    | Authorized of AuthorizedModel

let init ws = Anonymous(Anonymous.init ws)

let update msg model =
    match msg,model with
    | AnonymousMsg anonimMsg, Anonymous anonim -> Anonymous(Anonymous.update anonimMsg anonim)
    | _,_ -> model

let view model dispatch =
    Container.container [Container.isFluid]
        [div [] [
                let page =
                    match model with
                    | Anonymous m -> Anonymous.view m dispatch
                    | Authorized _-> div[][]
                yield page
            ]]

let private onSocketConnected (ws:WebSocket) =
    console.log("Socket connected")
    let subscription model =
        match model with
        | Anonymous m -> Cmd.map AnonymousMsg (Anonymous.subscription m)
        | _ -> Cmd.none
    let inited() = init ws
    Program.mkSimple inited update view
    |> Program.withSubscription subscription
    |> Program.withConsoleTrace
    |> Program.withReact "app"
    |> Program.run

let private ws = WebSocket.Create "ws://localhost:8080/fcon"
ws.addEventListener_open (fun _ -> !!onSocketConnected(ws))