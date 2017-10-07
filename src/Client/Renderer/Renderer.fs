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
open Feezer.Client.Renderer.Main
open Fable.Helpers.React
module E = Fable.Import.Electron
open Elmish
open Elmish.React
open Fulma.Extensions

type Msg =
    | TryGetUser
    | UserReceived of User
    | AnonymousMsg of Anonymous.Msg
    | AuthorizedMsg of Authorized.Msg

type MainModel =
    | Loading
    | Anonymous of Anonymous.Model
    | Authorized of Authorized.Model

type Model = {
    user:User;
    ws:WebSocket;
    main:MainModel
}


let init ws = {user=User.Anonymous; ws=ws; main=Loading}, Cmd.ofMsg TryGetUser

let update msg model : Model*Cmd<Msg> =
    match msg,model.main with
    | TryGetUser, Loading ->
        Client.GetUser |> toJson |> model.ws.send
        model, Cmd.none
    | AnonymousMsg subMsg, Anonymous subModel ->
        let updatedAnonymous = Anonymous.update subMsg subModel
        { model with main=MainModel.Anonymous(updatedAnonymous)}, Cmd.none
    | AuthorizedMsg subMsg,Authorized subModel ->
        let updated = Authorized.update subMsg subModel
        {model with main=MainModel.Authorized(updated)}, Cmd.none
    | UserReceived user, _ ->
        match user with
        | User.Anonymous ->
            let (m, cmd) = Anonymous.init model.ws
            {model with user=user; main=Anonymous(m)}, Cmd.map AnonymousMsg cmd
        | User.Authorized info->
            let m = Authorized.init info
            {model with user=user; main=Authorized(m)}, Cmd.none
    | _,_ -> model, Cmd.none

let view model dispatch =
    console.log("render")
    Container.container [Container.isFluid] [
        match model.main with
        | Loading -> yield span [] [str "Loading..."]
        | Anonymous m -> yield div [] [Anonymous.view m dispatch]
        | Authorized m -> yield div [] [Authorized.view m]
    ]

let private onWsMessageReveived (evt:MessageEvent) dispatch =
    let msg = ofJson<Server> !!evt.data
    console.log("message received" ,msg)
    match msg with
    | Server.CurrentUser user ->
        UserReceived user |> dispatch
    | _ -> ()

let subscription model =
    let sub dispatch =
        model.ws.addEventListener_message (
              fun evt->
                !! (onWsMessageReveived evt dispatch)
              )
    Cmd.ofSub sub

let private onSocketConnected (ws:WebSocket) =
    console.log("Socket connected")
    let inited() = init ws
    Program.mkProgram inited update view
    |> Program.withSubscription subscription
    |> Program.withConsoleTrace
    |> Program.withReact "app"
    |> Program.run

let private ws = WebSocket.Create "ws://localhost:8080/fcon"
ws.addEventListener_open (fun _ -> !!onSocketConnected(ws))