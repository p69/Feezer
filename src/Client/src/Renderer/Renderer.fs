module Feezer.Client.Renderer

open Fable.Import

let body = Browser.document.getElementById("app")
body.textContent <- "Hello Feezer!"