module Feezer.Client.Renderer.Main.Authorized

open Elmish
open Elmish.React
open Fulma.Elements
open Fulma
open Fable.Helpers.React
open Fable.Import.Browser
open Fable.Core.JsInterop
open Fable.Helpers
open Feezer.Domain.Protocol
open Feezer.Domain.User
open Fable.Import

type Msg =
    | Logout

type Model = {
    user:UserInfo
}

let init user = {user=user}

let update msg model=
    match msg with
    | Logout -> ()//do logout
    model

let view model =
    let name = "Hello, " + model.user.name
    div [] [str name]