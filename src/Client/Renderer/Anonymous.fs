module Feezer.Client.Renderer.Anonymous

open Elmish
open Elmish.React
open Fulma.Elements
open Fulma
open Fable.Helpers.React
open Fable.Import.Browser
open Fable.Core.JsInterop
open Feezer.Domain.Protocol
open Fable.Import
module E = Fable.Import.Electron

type Msg =
    | ShowPopup of url:string
    | HidePopup

type State =
    | InitialState
    | Authorizing
    | EndState

type Model = {
    ws:WebSocket;
    state:State;
    authPopup: E.BrowserWindow option
}


let init ws = {ws=ws; state=InitialState; authPopup=None}

let showPopup url =
    let currentWindow = electron.remote.getCurrentWindow()
    let options = createEmpty<E.BrowserWindowOptions>
    options.parent <- Some currentWindow
    options.modal <- Some true
    let popup = electron.remote.BrowserWindow.Create(options)
    popup.loadURL url
    popup

let update msg (model:Model) =
    match msg with
    | ShowPopup url -> {model with authPopup=Some(showPopup url); state=Authorizing}
    | HidePopup ->
        match model.authPopup with
        | Some popup ->popup.close()
        | None -> ()
        {model with authPopup=None; state=EndState}

let private onWsMessageReveived (evt:MessageEvent) dispatch =
    console.log("message received")
    let msg = ofJson<Server> !!evt.data
    match msg with
    | Server.Authorization authUrl -> dispatch Cmd.ofMsg <| ShowPopup(authUrl)
    | Server.Authorized expiration ->
        match expiration with
        | Never -> console.log("Token expiration: never")
        | Date date -> console.log(sprintf "Token expiration: %A" date)
        dispatch Cmd.ofMsg <| HidePopup
    | _ -> ()

let subscription model =
    console.log("subscribe")
    let sub dispatch =
        console.log("do subscribe")
        model.ws.addEventListener_message (
              fun evt->
                console.log("msg received")
                !!obj
              )
    Cmd.ofSub sub

let private loginClickHandler model dispatch =
    model.ws.send <| toJson Authozrize

let view model dispatch =
    match model.state with
    | InitialState -> Button.button [Button.isInfo
                                     Button.onClick (fun _ -> loginClickHandler model dispatch)] [str "Login using Deezer"]
    | Authorizing -> Button.button [Button.isInfo; Button.isLoading][]
    | EndState -> Button.button [Button.isInfo; Button.isLoading][]
