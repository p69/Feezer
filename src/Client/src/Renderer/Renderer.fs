module Feezer.Client.Renderer

open Fable.Import
open Fable.Websockets.Client

let body = Browser.document.getElementById("app")
body.textContent <- "Hello Feezer!"