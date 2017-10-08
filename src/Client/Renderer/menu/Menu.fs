module Feezer.Client.Renderer.Menu

open Elmish
open Elmish.React
open Fulma.Elements.Form
open Fulma
open Fable.Helpers.React.Props
open Fable.Import.Browser
open Feezer.Domain.User
open Fulma.BulmaClasses
open Fulma.Components
open Fulma.Elements
open Fable.Helpers.React
open Fulma.Extra.FontAwesome

type Page =
    | Flow
    | MyMusic
    | Favorites

type MenuItem = {
    label:string;
    icon:Fa.FontAwesomeIcons;
    isActive:bool;
    page:Page
}

type Msg =
    | ItemSelected of MenuItem

type Model = {
    user:UserInfo;
    items:MenuItem list
}

let init userInfo = {
    user=userInfo;
    items=[
        {label="Flow"; icon=Fa.PlayCircleO; isActive=false; page=Flow}
        {label="My music"; icon=Fa.Music; isActive=false; page=MyMusic}
        {label="Favorites"; icon=Fa.Heart; isActive=false; page=Favorites} ]
}

let private activate item itemForTest =
    if item=itemForTest
    then {item with isActive=true}
    else {item with isActive=false}

let update msg model =
    match msg with
    | ItemSelected item ->
        {model with
          items = model.items |> List.map (fun x-> activate x item) }

let private viewUser user = [
        yield Media.left [] [
                Fulma.Elements.Image.image [ Fulma.Elements.Image.is64x64 ]
                    [ img [Src user.avatar]]
                ]
        yield Media.content [] [
                a [ ClassName "item" ]
                    [ str user.name ]
                ]
    ]

let private viewItem dispatch item =
    let clickHandler _ = ItemSelected(item) |> dispatch
    li [] [
        a [
            classList [ Bulma.Menu.State.IsActive, item.isActive; "item", true ];
            Props.OnClick clickHandler ] [
                Icon.faIcon [] item.icon; span [Props.ClassName "name"] [ str item.label]
                ]
       ]

let view model dispatch = [
    yield! viewUser model.user
    yield Menu.menu [ ] [
             Menu.label [ Common.CustomClass "title" ] [ str "Feezer" ]
             Menu.list [Common.CustomClass "main"] (model.items|>List.map (fun x-> x|>viewItem dispatch))
        ]
    ]
