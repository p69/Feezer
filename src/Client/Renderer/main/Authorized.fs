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
open Feezer.Client.Renderer
open Fulma.Layouts

type Msg =
    | Logout
    | MenuMsg of Menu.Msg

type Model = {
    user:UserInfo;
    menu:Menu.Model
}

let init user = {user=user; menu=Menu.init user}

let update msg model=
    match msg with
    | Logout -> model//do logout
    | MenuMsg subMsg -> {model with menu=Menu.update subMsg model.menu}

let view model dispatch =
        [ aside [Props.ClassName "aside is-fullheight is-hidden-mobile"; Props.Role "navigation" ] [yield! Menu.view model.menu (MenuMsg >> dispatch)]
          section [Props.ClassName "main-content"] [str "Content"]
        ]